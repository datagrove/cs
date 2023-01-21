

using System.Text.Json;


public class GherkinFeature
{
    public string path { get; set; } = "";
    public string source { get; set; } = "";
    public string qualified_name { get; set; } = "";

    public List<GherkinStepJson> background { get; set; } = new List<GherkinStepJson>();

    public List<GherkinScenario> scenario { get; set; } = new List<GherkinScenario>();
}
public class GherkinScenario
{
    public string name { get; set; } = "";
    public List<GherkinStepJson> step { get; set; } = new List<GherkinStepJson>();
}

public class GherkinStepJson
{
    public string text { get; set; } = "";
    public string matches { get; set; } = "";
    public string stepDef { get; set; } = "";
}

public class GherkinDocJson
{
    public List<GherkinFeature> feature { get; set; } = new List<GherkinFeature>();
}
public class GherkinDocumentation
{
    public GherkinDocJson doc = new GherkinDocJson();
    public BuildGherkin bg;

    public GherkinDocumentation(BuildGherkin bg, string[] file)
    {
        this.bg = bg;
    }

    public void addFeature(string featureFile, string className)
    {
        var o = new GherkinFeature();
        o.path = featureFile;
        o.source = File.ReadAllText(featureFile);
        o.qualified_name = $"As1.{bg.tag}.{className}";
        this.doc.feature.Add(o);
    }
    public void addScenario(string name)
    {
        GherkinScenario x = new GherkinScenario();
        x.name = name;
        if (doc.feature.Count() == 0)
            return;
        doc.feature.Last().scenario.Add(x);
    }

    // why would we call step when there is no scenario?
    public void addStep(CompiledStep step)
    {
        GherkinStepJson x = new GherkinStepJson();
        x.text = step.text;
        x.matches = step.step.text;
        x.stepDef = step.step.qualified_name();
        if (doc.feature.Count() == 0)
            return;
        var sc = doc.feature.Last();
        if (sc.scenario.Count() == 0)
        {
            sc.background.Add(x);
        }
        else
        {
            var s = sc.scenario.Last();
            s.step.Add(x);
        }
    }

    public string json()
    {
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }
}
