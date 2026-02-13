# ISON-C#

A minimal, token-efficient data format optimized for LLMs and Agentic AI workflows.

The origin project and why ([HERE](https://github.com/ISON-format/ison))

## Usage Examples

### C#

```csharp
var doc = Ison.Parse(@"
table.users
id:int name:string active:bool
1 Alice true
2 Bob false
");
var (users, ok) = doc.Get("users");
Assert.True(ok);
Debug.Assert(users != null, nameof(users) + " != null");
foreach (var row in users.Rows)
{
    var (name, _) = row["name"].AsString();
    _testOutputHelper.WriteLine(name);
}
```
