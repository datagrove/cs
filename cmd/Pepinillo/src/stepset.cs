using System.Text.RegularExpressions;
using System.Reflection;
using System.Text;
using Gherkin.Ast;
using TechTalk.SpecFlow;

// using reflection we don't get the file name, change to code generation?
public class StepSet
{
    public List<GherkinStep> allSteps = new List<GherkinStep>();
    List<TransformArg> transforms = new List<TransformArg>();

    // the json will create something the publisher can use to describe the classes.
    public string text()
    {
        StringBuilder sb = new StringBuilder();
        var was = "";
        foreach (var gs in allSteps) {
            if (was!=gs.classType.FullName) {
                sb.Append($"\n## {gs.classType.FullName}\n");
              sb.Append("\n| pattern | method | class |\n|-|-|-|\n");
              was = gs.classType.FullName;
            }
            sb.Append($"| {gs.text} | {gs.m.Name} |\n");
        }
        return sb.ToString();
    }
    // outputs a representation that can go in the stepCall. (mostly string to bool)
    public string convert(String s, Type t)
    {
        string r = "";
        foreach (var o in transforms)
        {
            if (o.tryTransform(s, t, out r))
                return r;
        }
        try
        {
            var d = Convert.ChangeType(s, t);
            return d.ToString() ?? "";
        }
        catch (Exception)
        {
            return s;
        }
    }

    // maps fully qualified name to local name in the set.
    public Dictionary<string, string> usingNamespace = new Dictionary<string, string>();
    public Dictionary<string, int> nameCount = new Dictionary<string, int>();

    // base namespaces before resolution
    public HashSet<string> usingNs = new HashSet<string>();

    public string shortName(string fullName, string name)
    {
        string r = "";
        if (usingNamespace.TryGetValue(fullName, out r!)) return r;

        int r2 = 0;
        if (nameCount.TryGetValue(name, out r2))
        {
            r2++;
        }
        nameCount[name] = r2;
        if (r2 > 0)
        {
            name = $"{name}{r2}";
        }
        usingNamespace[fullName] = name;
        return name;
    }

    public StepSet(Assembly prog, string[] stepSpace)
    {
        // we ignore any type if it isn't in one of the  namespaces
        foreach (var t in prog.GetTypes())
        {
            var ca = (BindingAttribute?)t.GetCustomAttribute(typeof(BindingAttribute), false);
            if (ca == null) continue;

            var match = false;
            var sp = t.Namespace ?? "";
            int priority = 0;
            foreach (var o in stepSpace)
            {
                if (sp.StartsWith(o + "."))
                {
                    match = true;
                    break;
                }
                priority++;
            }
            if (!match) continue;
            var sn = shortName(t.FullName ?? t.Name, t.Name);

            if (t.Namespace != null)
                usingNs.Add(t.Namespace);

            foreach (var m in t.GetMethods())
            {
                foreach (var tc in m.GetCustomAttributes(typeof(StepDefinitionBaseAttribute), false))
                {
                    var tcx = (StepDefinitionBaseAttribute)tc;
                    var mch = tcx.Regex ?? m.Name.Replace('_', ' ');
                    allSteps.Add(new GherkinStep(t, this, m, sn, mch, priority));
                     break; // often we have when/then/given variants
                }
                foreach (var tc in m.GetCustomAttributes(typeof(StepArgumentTransformationAttribute), false))
                {
                    var o = t.GetConstructor(new Type[] { });
                    var o2 = o?.Invoke(null);
                    var tcx = (StepArgumentTransformationAttribute)tc;
                    transforms.Add(new TransformArg(tcx.Regex, o2!, m));
                }
            }

        }
        allSteps.Sort((a,b) => a.priority-b.priority);
    }
    public string usingText()
    {
        var sb = new StringBuilder();
        foreach (var s in usingNs)
        {
            sb.Append($"using {s};\n");
        }
        return sb.ToString();
    }
        static public string stringList(string[] cell)
    {
        var sb = new StringBuilder();
        int i = 0;
        foreach (var c in cell)
        {
            if (i != 0) sb.Append(",");
            sb.Append($"\"{c}\"");
            i++;
        }
        return $" new string[]{{ {sb.ToString()} }}";
    }
    static public string declare(string name, int nrow, List<String> cell)
    {
        int ncol = cell.Count() / nrow;
        var header = stringList(cell.Take(ncol).ToArray());
        var data = stringList(cell.Skip(ncol).ToArray());
        return $"GherkinTable.make({header}, {data})";
    }
    static public string declare(DataTable dt)
    {
        var cl = new List<String>();
        foreach (var row in dt.Rows)
        {
            foreach (var cell in row.Cells)
            {
                cl.Add(cell.Value);
            }
        }
        return declare("", dt.Rows.Count(), cl);
    }
    public string csharp(GherkinStep step, Step stepCall, string text)
    {
        // SFINAE - substitution failure is not an error
        var stmt = "//" + text + "\n";
        var suffix = "";

        var pr = step.m.GetParameters();
        var match = step.rg.Match(text).Groups.Cast<Group>().Skip(1).ToArray();
        if (match.Count() + (stepCall.Argument == null ? 0 : 1) != pr.Count())
        {
            // the number of parameters doesn't match
            return "";
        }

        // table or docstring must be the last parameter
        if (stepCall.Argument is DataTable dt)
        {
            suffix = declare(dt);

            if (pr.Last().ParameterType != typeof(TechTalk.SpecFlow.Table))
            {
                // this stepCall has a table, it only matches steps that take one.
                return "";
            }
            pr = pr.Take(pr.Count() - 1).ToArray();
        }
        else if (stepCall.Argument is DocString ds)
        {
            suffix = "@\"" + ds.Content + "\"";
            if (pr.Last().GetType() != typeof(string))
            {
                // this stepCall has a docstring, it only matches steps that take one.
                return "";
            }
            pr = pr.Take(pr.Count() - 1).ToArray();
        }

        var args = new List<string>();
        for (var i = 0; i < pr.Length; i++)
        {
            var rw = match[i].ToString();
            var t1 = pr[i].ParameterType;
            if (t1 == typeof(string))
                args.Add("\"" + rw + "\"");
            else
            {
                // this can fail too, not matching
                rw = step.ss.convert(rw ?? "", t1);
                args.Add(rw);
            }
        }
        if (suffix != "")
        {
            args.Add(suffix);
        }

        var n = step.className.Substring(0, 1).ToLower() + step.className.Substring(1);
        stmt += n + "." + step.m.Name + "(" + String.Join(",", args) + ");\n";

        return stmt;
    }
    // we have "overloaded" steps that match on type as well as on the regex.
    public CompiledStep? compile(Step s, string text,Action<string> warning)
    {
        // should use a trie here, this is o(n^2)
        var step = new List<CompiledStep>();
        foreach (var st in allSteps)
        {
            if (st.rg.IsMatch(text))
            {
                // see if we can match the arguments, if we can't, this isn't a match.
                var cs = csharp(st, s, text);
                if (cs != "")
                {
                    step.Add( new CompiledStep(st, s, text, cs));
                }
            }
        }

        if (step.Count()>1) 
        {
            var b = new StringBuilder();
            b.Append($"\n*{text}*\n");
            foreach (var stx in step) {
               b.Append($"```\n{stx.step.errorDescription()}\n```\n");
            }
            warning(b.ToString());
        }
        return step.Count()>0?step.First():null;
    }

}


public class GherkinStep
{
    public Type classType;
    public StepSet ss;
    public MethodInfo m;
    public string className;
    public Regex rg = new Regex(@"");

    public string text;
    public int priority;
    public GherkinStep(Type classType, StepSet stepSet, MethodInfo m, string className, string s, int priority)
    {
        this.classType = classType;
        this.m = m;
        this.ss = stepSet;
        this.className = className;
        this.text = s;
        this.priority = priority;
        this.rg = new Regex("^" + s + "$");
    }

    public string qualified_name() {
        return $"{classType.FullName}.{m.Name}";
    }

    public string errorDescription() {
        return $"{classType.FullName}.{m.Name} {text}";
    }

}
public class CompiledStep
{

    public GherkinStep step;
    public Step stepCall;
    public string text;
    public string csharp;

    public CompiledStep(GherkinStep step, Step stepCall, string text, string csharp)
    {
        this.step = step;
        this.stepCall = stepCall;
        this.text = text;
        this.csharp = csharp;
    }

  
}
