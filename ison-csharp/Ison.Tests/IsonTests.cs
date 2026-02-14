using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Ison.Tests
{
    public class IsonTests
    {
        [Fact]
        public void TestVersion()
        {
            Assert.Equal("1.0.1", VersionInfo.Version);
        }

        [Fact]
        public void TestParseSimpleTable()
        {
            var input = @"
table.users
id name email
1 Alice alice@example.com
2 Bob bob@example.com
";
            var doc = Ison.Parse(input);

            var (block, ok) = doc.Get("users");
            Assert.True(ok);
            Debug.Assert(block != null, nameof(block) + " != null");
            Assert.Equal("table", block.Kind);
            Assert.Equal("users", block.Name);
            Assert.Equal(3, block.Fields.Count);
            Assert.Equal(2, block.Rows.Count);

            var row1 = block.Rows[0];
            var (id, idOk) = row1["id"].AsInt();
            Assert.True(idOk);
            Assert.Equal(1L, id);

            var (name, nameOk) = row1["name"].AsString();
            Assert.True(nameOk);
            Assert.Equal("Alice", name);
        }

        [Fact]
        public void TestParseTypedFields()
        {
            var input = @"
table.users
id:int name:string active:bool score:float
1 Alice true 95.5
2 Bob false 82.0
";
            var doc = Ison.Parse(input);

            var (block, ok) = doc.Get("users");
            Assert.True(ok);
            Debug.Assert(block != null, nameof(block) + " != null");
            Assert.Equal("int", block.Fields[0].TypeHint);
            Assert.Equal("string", block.Fields[1].TypeHint);
            Assert.Equal("bool", block.Fields[2].TypeHint);
            Assert.Equal("float", block.Fields[3].TypeHint);

            var row = block.Rows[0];
            var (id, _) = row["id"].AsInt();
            Assert.Equal(1L, id);

            var (active, activeOk) = row["active"].AsBool();
            Assert.True(activeOk);
            Assert.True(active);

            var (score, scoreOk) = row["score"].AsFloat();
            Assert.True(scoreOk);
            Assert.Equal(95.5, score);
        }

        [Fact]
        public void TestParseQuotedStrings()
        {
            var input = "\n" +
                "table.users\n" +
                "id name email\n" +
                "1 \"Alice Smith\" alice@example.com\n" +
                "2 \"Bob \\\"The Builder\\\" Jones\" bob@example.com\n";
            var doc = Ison.Parse(input);

            var (block, _) = doc.Get("users");
            Debug.Assert(block != null, nameof(block) + " != null");
            var (name1, _) = block.Rows[0]["name"].AsString();
            Assert.Equal("Alice Smith", name1);

            var (name2, _) = block.Rows[1]["name"].AsString();
            Assert.Equal("Bob \"The Builder\" Jones", name2);
        }

        [Fact]
        public void TestParseNullValues()
        {
            var input = @"
table.users
id name email
1 Alice ~
2 ~ null
3 Charlie NULL
";
            var doc = Ison.Parse(input);

            var (block, _) = doc.Get("users");
            Debug.Assert(block != null, nameof(block) + " != null");
            Assert.True(block.Rows[0]["email"].IsNull());
            Assert.True(block.Rows[1]["name"].IsNull());
            Assert.True(block.Rows[1]["email"].IsNull());
            Assert.True(block.Rows[2]["email"].IsNull());
        }

        [Fact]
        public void TestParseReferences()
        {
            var input = @"
table.orders
id user_id product
1 :1 Widget
2 :user:42 Gadget
3 :OWNS:5 Gizmo
";
            var doc = Ison.Parse(input);

            var (block, _) = doc.Get("orders");

            Debug.Assert(block != null, nameof(block) + " != null");
            var (ref1, ok1) = block.Rows[0]["user_id"].AsRef();
            Assert.True(ok1);
            Assert.Equal("1", ref1.ID);
            Assert.Empty(ref1.Namespace);
            Assert.Empty(ref1.Relationship);
            Assert.Equal(":1", ref1.ToIson());

            var (ref2, ok2) = block.Rows[1]["user_id"].AsRef();
            Assert.True(ok2);
            Assert.Equal("42", ref2.ID);
            Assert.Equal("user", ref2.Namespace);
            Assert.Empty(ref2.Relationship);
            Assert.Equal(":user:42", ref2.ToIson());

            var (ref3, ok3) = block.Rows[2]["user_id"].AsRef();
            Assert.True(ok3);
            Assert.Equal("5", ref3.ID);
            Assert.Equal("OWNS", ref3.Relationship);
            Assert.True(ref3.IsRelationship());
            Assert.Equal(":OWNS:5", ref3.ToIson());
        }

        [Fact]
        public void TestParseObjectBlock()
        {
            var input = @"
object.config
key value
debug true
timeout 30
";
            var doc = Ison.Parse(input);

            var (block, ok) = doc.Get("config");
            Assert.True(ok);
            Debug.Assert(block != null, nameof(block) + " != null");
            Assert.Equal("object", block.Kind);
            Assert.Equal(2, block.Rows.Count);
        }

        [Fact]
        public void TestParseMultipleBlocks()
        {
            var input = @"
table.users
id name
1 Alice

table.orders
id user_id
O1 :1

object.meta
version 1.0
";
            var doc = Ison.Parse(input);

            Assert.Equal(3, doc.Blocks.Count);
            Assert.Equal(new[] { "users", "orders", "meta" }, doc.Order);

            Assert.True(doc.Get("users").ok);
            Assert.True(doc.Get("orders").ok);
            Assert.True(doc.Get("meta").ok);
        }

        [Fact]
        public void TestParseSummaryRow()
        {
            var input = @"
table.sales
product amount
Widget 100
Gadget 200
---
total 300
";
            var doc = Ison.Parse(input);

            var (block, _) = doc.Get("sales");
            Debug.Assert(block != null, nameof(block) + " != null");
            Assert.Equal(2, block.Rows.Count);
            Assert.NotNull(block.SummaryRow);

            var (total, ok) = block.SummaryRow["amount"].AsInt();
            Assert.True(ok);
            Assert.Equal(300L, total);
        }

        [Fact]
        public void TestParseComments()
        {
            var input = @"
# This is a comment
table.users
# Field definitions
id name
# Row 1
1 Alice
# Row 2
2 Bob
";
            var doc = Ison.Parse(input);

            var (block, _) = doc.Get("users");
            Debug.Assert(block != null, nameof(block) + " != null");
            Assert.Equal(2, block.Rows.Count);
        }

        [Fact]
        public void TestDumps()
        {
            var doc = new Document();
            var block = new Block("table", "users");
            block.AddField("id", "int");
            block.AddField("name", "string");
            block.AddRow(new Row { ["id"] = Value.Int(1), ["name"] = Value.String("Alice") });
            block.AddRow(new Row { ["id"] = Value.Int(2), ["name"] = Value.String("Bob") });
            doc.AddBlock(block);

            var output = Ison.Dumps(doc);
            Assert.Contains("table.users", output);
            Assert.Contains("id:int", output);
            Assert.Contains("name:string", output);
            Assert.Contains("1 Alice", output);
            Assert.Contains("2 Bob", output);
        }

        [Fact]
        public void TestRoundtrip()
        {
            var input = @"table.users
id:int name:string active:bool
1 Alice true
2 Bob false
";
            var doc = Ison.Parse(input);
            var output = Ison.Dumps(doc);
            var doc2 = Ison.Parse(output);

            var (block1, _) = doc.Get("users");
            var (block2, _) = doc2.Get("users");

            Debug.Assert(block1 != null, nameof(block1) + " != null");
            Debug.Assert(block2 != null, nameof(block2) + " != null");
            Assert.Equal(block1.Rows.Count, block2.Rows.Count);
            for (int i = 0; i < block1.Rows.Count; i++)
            {
                foreach (var kvp in block1.Rows[i])
                {
                    Assert.Equal(kvp.Value.ToObject(), block2.Rows[i][kvp.Key].ToObject());
                }
            }
        }

        [Fact]
        public void TestDumpsIsonl()
        {
            var doc = new Document();
            var block = new Block("table", "users");
            block.AddField("id", "int");
            block.AddField("name", "string");
            block.AddRow(new Row { ["id"] = Value.Int(1), ["name"] = Value.String("Alice") });
            block.AddRow(new Row { ["id"] = Value.Int(2), ["name"] = Value.String("Bob") });
            doc.AddBlock(block);

            var output = Ison.DumpsIsonl(doc);
            var lines = output.Trim().Split('\n');
            Assert.Equal(2, lines.Length);
            Assert.Contains("table.users|id:int name:string|1 Alice", lines[0]);
            Assert.Contains("table.users|id:int name:string|2 Bob", lines[1]);
        }

        [Fact]
        public void TestParseIsonl()
        {
            var input = "table.users|id:int name:string|1 Alice\ntable.users|id:int name:string|2 Bob\ntable.orders|id product|O1 Widget";

            var doc = Ison.ParseIsonl(input);

            var (users, ok1) = doc.Get("users");
            Assert.True(ok1);
            Debug.Assert(users != null, nameof(users) + " != null");
            Assert.Equal(2, users.Rows.Count);

            var (orders, ok2) = doc.Get("orders");
            Assert.True(ok2);
            Debug.Assert(orders != null, nameof(orders) + " != null");
            Assert.Single(orders.Rows);
        }

        [Fact]
        public void TestToJson()
        {
            var input = @"
table.users
id:int name:string active:bool
1 Alice true
2 Bob false
";
            var jsonStr = Ison.ToJson(input);

            var data = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, JsonElement>>>>(jsonStr);
            Assert.NotNull(data);

            var users = data["users"];
            Assert.Equal(2, users.Count);

            Assert.Equal(1, users[0]["id"].GetInt64());
            Assert.Equal("Alice", users[0]["name"].GetString());
            Assert.True(users[0]["active"].GetBoolean());
        }

        [Fact]
        public void TestFromJson()
        {
            var jsonStr = @"{
                ""users"": [
                    {""id"": 1, ""name"": ""Alice"", ""active"": true},
                    {""id"": 2, ""name"": ""Bob"", ""active"": false}
                ]
            }";

            var doc = Ison.FromJson(jsonStr);

            var (block, ok) = doc.Get("users");
            Assert.True(ok);
            Debug.Assert(block != null, nameof(block) + " != null");
            Assert.Equal(2, block.Rows.Count);

            var (id, _) = block.Rows[0]["id"].AsInt();
            Assert.Equal(1L, id);
        }

        [Fact]
        public void TestValueTypes()
        {
            var nullVal = Value.Null();
            Assert.Equal(ValueType.Null, nullVal.Type);
            Assert.True(nullVal.IsNull());
            Assert.Null(nullVal.ToObject());

            var boolVal = Value.Bool(true);
            Assert.Equal(ValueType.Bool, boolVal.Type);
            var (b, bOk) = boolVal.AsBool();
            Assert.True(bOk);
            Assert.True(b);

            var intVal = Value.Int(42);
            Assert.Equal(ValueType.Int, intVal.Type);
            var (i, iOk) = intVal.AsInt();
            Assert.True(iOk);
            Assert.Equal(42L, i);

            var floatVal = Value.Float(3.14);
            Assert.Equal(ValueType.Float, floatVal.Type);
            var (f, fOk) = floatVal.AsFloat();
            Assert.True(fOk);
            Assert.Equal(3.14, f);

            var (f2, f2Ok) = intVal.AsFloat();
            Assert.True(f2Ok);
            Assert.Equal(42.0, f2);

            var strVal = Value.String("hello");
            Assert.Equal(ValueType.String, strVal.Type);
            var (s, sOk) = strVal.AsString();
            Assert.True(sOk);
            Assert.Equal("hello", s);

            var refVal = Value.Ref(new Reference("1", "user"));
            Assert.Equal(ValueType.Reference, refVal.Type);
            var (r, rOk) = refVal.AsRef();
            Assert.True(rOk);
            Assert.Equal("1", r.ID);
            Assert.Equal("user", r.Namespace);
        }

        [Fact]
        public void TestValueToIson()
        {
            Assert.Equal("~", Value.Null().ToIson());
            Assert.Equal("true", Value.Bool(true).ToIson());
            Assert.Equal("false", Value.Bool(false).ToIson());
            Assert.Equal("42", Value.Int(42).ToIson());
            Assert.Equal("3.14", Value.Float(3.14).ToIson());
            Assert.Equal("hello", Value.String("hello").ToIson());
            Assert.Equal("\"hello world\"", Value.String("hello world").ToIson());
            Assert.Equal("\"with \\\"quotes\\\"\"", Value.String("with \"quotes\"").ToIson());
            Assert.Equal(":1", Value.Ref(new Reference("1")).ToIson());
            Assert.Equal(":user:1", Value.Ref(new Reference("1", "user")).ToIson());
            Assert.Equal(":OWNS:1", Value.Ref(new Reference("1", relationship: "OWNS")).ToIson());
        }

        [Fact]
        public void TestReferenceGetNamespace()
        {
            var ref1 = new Reference("1");
            Assert.Empty(ref1.GetNamespace());

            var ref2 = new Reference("1", "user");
            Assert.Equal("user", ref2.GetNamespace());

            var ref3 = new Reference("1", relationship: "OWNS");
            Assert.Equal("OWNS", ref3.GetNamespace());
        }

        [Fact]
        public void TestBlockToDict()
        {
            var block = new Block("table", "users");
            block.AddField("id", "int");
            block.AddField("name", "string");
            block.AddRow(new Row { ["id"] = Value.Int(1), ["name"] = Value.String("Alice") });

            var dict = block.ToDict();
            Assert.Equal("table", dict["kind"]);
            Assert.Equal("users", dict["name"]);

            var fields = (List<Dictionary<string, object>>)dict["fields"];
            Assert.Equal(2, fields.Count);
            Assert.Equal("id", fields[0]["name"]);
            Assert.Equal("int", fields[0]["typeHint"]);

            var rows = (List<Dictionary<string, object>>)dict["rows"];
            Assert.Single(rows);
            Assert.Equal(1L, rows[0]["id"]);
        }

        [Fact]
        public void TestDocumentToDict()
        {
            var doc = new Document();
            var block = new Block("table", "users");
            block.AddField("id", "int");
            block.AddRow(new Row { ["id"] = Value.Int(1) });
            doc.AddBlock(block);

            var dict = doc.ToDict();
            var users = (Dictionary<string, object>)dict["users"];
            Assert.Equal("table", users["kind"]);
        }

        [Fact]
        public void TestEscapeSequences()
        {
            var input = "\n" +
                "table.data\n" +
                "id text\n" +
                "1 \"line1\\nline2\"\n" +
                "2 \"tab\\there\"\n";
            var doc = Ison.Parse(input);

            var (block, _) = doc.Get("data");
            Debug.Assert(block != null, nameof(block) + " != null");
            var (text1, _) = block.Rows[0]["text"].AsString();
            Assert.Equal("line1\nline2", text1);

            var (text2, _) = block.Rows[1]["text"].AsString();
            Assert.Equal("tab\there", text2);
        }

        [Fact]
        public void TestEmptyDocument()
        {
            var doc = Ison.Parse("");
            Assert.Empty(doc.Blocks);
        }

        [Fact]
        public void TestOnlyComments()
        {
            var input = @"
# Comment 1
# Comment 2
";
            var doc = Ison.Parse(input);
            Assert.Empty(doc.Blocks);
        }

        [Fact]
        public void TestTypeInference()
        {
            var input = @"
table.data
a b c d e
42 3.14 true false hello
";
            var doc = Ison.Parse(input);

            var (block, _) = doc.Get("data");
            Debug.Assert(block != null, nameof(block) + " != null");
            var row = block.Rows[0];

            Assert.Equal(ValueType.Int, row["a"].Type);
            Assert.Equal(ValueType.Float, row["b"].Type);
            Assert.Equal(ValueType.Bool, row["c"].Type);
            Assert.Equal(ValueType.Bool, row["d"].Type);
            Assert.Equal(ValueType.String, row["e"].Type);
        }

        [Fact]
        public void TestGetFieldNames()
        {
            var block = new Block("table", "test");
            block.AddField("id", "int");
            block.AddField("name", "string");
            block.AddField("active", "bool");

            var names = block.GetFieldNames();
            Assert.Equal(new[] { "id", "name", "active" }, names);
        }

        [Fact]
        public void TestLoadDump()
        {
            var tmpfile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.ison");

            try
            {
                var doc = new Document();
                var block = new Block("table", "users");
                block.AddField("id", "int");
                block.AddField("name", "string");
                block.AddRow(new Row { ["id"] = Value.Int(1), ["name"] = Value.String("Alice") });
                doc.AddBlock(block);

                Ison.Dump(doc, tmpfile);

                var loaded = Ison.Load(tmpfile);

                var (users, ok) = loaded.Get("users");
                Assert.True(ok);
                Debug.Assert(users != null, nameof(users) + " != null");
                Assert.Single(users.Rows);
                var (name, _) = users.Rows[0]["name"].AsString();
                Assert.Equal("Alice", name);
            }
            finally
            {
                if (File.Exists(tmpfile))
                    File.Delete(tmpfile);
            }
        }

        [Fact]
        public void TestLoadDumpIsonl()
        {
            var tmpfile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.isonl");

            try
            {
                var doc = new Document();
                var block = new Block("table", "users");
                block.AddField("id", "int");
                block.AddField("name", "string");
                block.AddRow(new Row { ["id"] = Value.Int(1), ["name"] = Value.String("Alice") });
                block.AddRow(new Row { ["id"] = Value.Int(2), ["name"] = Value.String("Bob") });
                doc.AddBlock(block);

                Ison.DumpIsonl(doc, tmpfile);

                var loaded = Ison.LoadIsonl(tmpfile);

                var (users, ok) = loaded.Get("users");
                Assert.True(ok);
                Debug.Assert(users != null, nameof(users) + " != null");
                Assert.Equal(2, users.Rows.Count);
            }
            finally
            {
                if (File.Exists(tmpfile))
                    File.Delete(tmpfile);
            }
        }

        [Fact]
        public void TestIsonToIsonl()
        {
            var isonText = "table.users\nid:int name:string\n1 Alice\n2 Bob";

            var isonlText = Ison.IsonToIsonl(isonText);

            var lines = isonlText.Trim().Split('\n');
            Assert.Equal(2, lines.Length);
            Assert.Contains("table.users|", lines[0]);
            Assert.Contains("1 Alice", lines[0]);
            Assert.Contains("2 Bob", lines[1]);
        }

        [Fact]
        public void TestIsonlToIson()
        {
            var isonlText = "table.users|id:int name:string|1 Alice\ntable.users|id:int name:string|2 Bob";

            var isonText = Ison.IsonlToIson(isonlText);

            Assert.Contains("table.users", isonText);
            Assert.Contains("id:int name:string", isonText);
            Assert.Contains("1 Alice", isonText);
            Assert.Contains("2 Bob", isonText);
        }

        [Fact]
        public void TestDumpsWithOptions()
        {
            var doc = new Document();
            var block = new Block("table", "users");
            block.AddField("id", "int");
            block.AddField("name", "string");
            block.AddRow(new Row { ["id"] = Value.Int(1), ["name"] = Value.String("Alice") });
            block.AddRow(new Row { ["id"] = Value.Int(2), ["name"] = Value.String("Bob") });
            doc.AddBlock(block);

            var opts = new DumpsOptions
            {
                AlignColumns = false,
                Delimiter = "\t"
            };
            var output = Ison.DumpsWithOptions(doc, opts);
            Assert.Contains("id:int\tname:string", output);
            Assert.Contains("1\tAlice", output);
        }

        [Fact]
        public async Task TestIsonlStream()
        {
            var input = "table.users|id:int name:string|1 Alice\ntable.users|id:int name:string|2 Bob\ntable.orders|id product|O1 Widget";

            using var reader = new StringReader(input);
            var ch = Ison.IsonlStream(reader);

            var records = new List<Ison.IsonlRecord>();
            await foreach (var record in ch.ReadAllAsync())
            {
                records.Add(record);
            }

            Assert.Equal(3, records.Count);
            Assert.Equal("users", records[0].Name);
            Assert.Equal("orders", records[2].Name);

            var (name, ok) = records[0].Values["name"].AsString();
            Assert.True(ok);
            Assert.Equal("Alice", name);
        }

        [Fact]
        public void TestFromDict()
        {
            var data = new Dictionary<string, object>
            {
                ["users"] = new List<object>
                {
                    new Dictionary<string, object> { ["id"] = 1, ["name"] = "Alice", ["active"] = true },
                    new Dictionary<string, object> { ["id"] = 2, ["name"] = "Bob", ["active"] = false }
                }
            };

            var doc = Ison.FromDict(data);

            var (users, ok) = doc.Get("users");
            Assert.True(ok);
            Debug.Assert(users != null, nameof(users) + " != null");
            Assert.Equal("table", users.Kind);
            Assert.Equal(2, users.Rows.Count);
        }

        [Fact]
        public void TestFromDictWithAutoRefs()
        {
            var data = new Dictionary<string, object>
            {
                ["orders"] = new List<object>
                {
                    new Dictionary<string, object> { ["id"] = 1, ["customer_id"] = 42, ["product"] = "Widget" }
                },
                ["customers"] = new List<object>
                {
                    new Dictionary<string, object> { ["id"] = 42, ["name"] = "Alice" }
                }
            };

            var opts = new FromDictOptions
            {
                AutoRefs = true,
                SmartOrder = true
            };
            var doc = Ison.FromDictWithOptions(data, opts);

            var (orders, ok) = doc.Get("orders");
            Assert.True(ok);
            Debug.Assert(orders != null, nameof(orders) + " != null");
            var custId = orders.Rows[0]["customer_id"];
            var (refVal, refOk) = custId.AsRef();
            Assert.True(refOk);
            Assert.Equal("42", refVal.ID);
        }

        [Fact]
        public void TestDefaultDumpsOptions()
        {
            var opts = DumpsOptions.Default;
            Assert.False(opts.AlignColumns);
            Assert.Equal(" ", opts.Delimiter);
        }

        [Fact]
        public void TestDefaultFromDictOptions()
        {
            var opts = FromDictOptions.Default;
            Assert.False(opts.AutoRefs);
            Assert.False(opts.SmartOrder);
        }
    }
}
