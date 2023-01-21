

we have some ? namespace distinctions here.

```
namespace As1.v9 {
    class Foobar{}
    class BadClass {}
}
namespace As1.V9 {
    class BadClass{

        BadClass() {
            var x = new As1.v9.BadClass();

            Foobar a;
            As1.v9.Foobar b;
        }

    }
}

```