using System.Text.RegularExpressions;
using Xunit;

namespace Isonantic.Tests;

public class IsonanticTests
{
    [Fact]
    public void TestVersion()
    {
        Assert.Equal("1.0.1", IsonanticInfo.Version);
    }

    // String Schema Tests

    [Fact]
    public void TestStringRequired()
    {
        var schema = I.String();

        schema.Validate("hello");
        
        var ex = Assert.Throws<Exception>(() => schema.Validate(null));
        Assert.Contains("required", ex.Message);
    }

    [Fact]
    public void TestStringOptional()
    {
        var schema = I.String().Optional();

        schema.Validate(null);
        schema.Validate("hello");
    }

    [Fact]
    public void TestStringMinLength()
    {
        var schema = I.String().Min(5);

        schema.Validate("hello");
        
        var ex = Assert.Throws<Exception>(() => schema.Validate("hi"));
        Assert.Contains("at least 5", ex.Message);
    }

    [Fact]
    public void TestStringMaxLength()
    {
        var schema = I.String().Max(5);

        schema.Validate("hello");
        
        var ex = Assert.Throws<Exception>(() => schema.Validate("hello world"));
        Assert.Contains("at most 5", ex.Message);
    }

    [Fact]
    public void TestStringExactLength()
    {
        var schema = I.String().Length(5);

        schema.Validate("hello");
        
        var ex = Assert.Throws<Exception>(() => schema.Validate("hi"));
        Assert.Contains("exactly 5", ex.Message);
    }

    [Fact]
    public void TestStringEmail()
    {
        var schema = I.String().Email();

        schema.Validate("test@example.com");
        
        var ex = Assert.Throws<Exception>(() => schema.Validate("invalid-email"));
        Assert.Contains("invalid email", ex.Message);
    }

    [Fact]
    public void TestStringUrl()
    {
        var schema = I.String().Url();

        schema.Validate("https://example.com");
        schema.Validate("http://example.com/path");
        
        var ex = Assert.Throws<Exception>(() => schema.Validate("not-a-url"));
        Assert.Contains("invalid URL", ex.Message);
    }

    [Fact]
    public void TestStringRegex()
    {
        var pattern = new Regex(@"^[A-Z]{2,3}$");
        var schema = I.String().Regex(pattern);

        schema.Validate("AB");
        schema.Validate("ABC");
        
        Assert.Throws<Exception>(() => schema.Validate("A"));
        Assert.Throws<Exception>(() => schema.Validate("ABCD"));
    }

    [Fact]
    public void TestStringDefault()
    {
        var schema = I.String().Default("default");

        var (def, hasDefault) = schema.GetDefault();
        Assert.True(hasDefault);
        Assert.Equal("default", def);
    }

    [Fact]
    public void TestStringDescribe()
    {
        var schema = I.String().Describe("User's name");

        Assert.Equal("User's name", schema.Description);
    }

    [Fact]
    public void TestStringRefine()
    {
        var schema = I.String().Refine(s => s[0] >= 'A' && s[0] <= 'Z', "must start with uppercase");

        schema.Validate("Hello");
        
        var ex = Assert.Throws<Exception>(() => schema.Validate("hello"));
        Assert.Contains("must start with uppercase", ex.Message);
    }

    // Number Schema Tests

    [Fact]
    public void TestNumberRequired()
    {
        var schema = I.Number();

        schema.Validate(42.5);
        
        Assert.Throws<Exception>(() => schema.Validate(null));
    }

    [Fact]
    public void TestNumberOptional()
    {
        var schema = I.Number().Optional();

        schema.Validate(null);
    }

    [Fact]
    public void TestIntSchema()
    {
        var schema = I.Int();

        schema.Validate((long)42);
        
        var ex = Assert.Throws<Exception>(() => schema.Validate(42.5));
        Assert.Contains("expected integer", ex.Message);
    }

    [Fact]
    public void TestNumberMin()
    {
        var schema = I.Number().Min(10);

        schema.Validate(10.0);
        
        var ex = Assert.Throws<Exception>(() => schema.Validate(5.0));
        Assert.Contains("at least", ex.Message);
    }

    [Fact]
    public void TestNumberMax()
    {
        var schema = I.Number().Max(10);

        schema.Validate(10.0);
        
        var ex = Assert.Throws<Exception>(() => schema.Validate(15.0));
        Assert.Contains("at most", ex.Message);
    }

    [Fact]
    public void TestNumberPositive()
    {
        var schema = I.Number().Positive();

        schema.Validate(5.0);
        
        var ex1 = Assert.Throws<Exception>(() => schema.Validate(0.0));
        Assert.Contains("positive", ex1.Message);
        
        var ex2 = Assert.Throws<Exception>(() => schema.Validate(-5.0));
        Assert.Contains("positive", ex2.Message);
    }

    [Fact]
    public void TestNumberNegative()
    {
        var schema = I.Number().Negative();

        schema.Validate(-5.0);
        
        var ex1 = Assert.Throws<Exception>(() => schema.Validate(0.0));
        Assert.Contains("negative", ex1.Message);
        
        var ex2 = Assert.Throws<Exception>(() => schema.Validate(5.0));
        Assert.Contains("negative", ex2.Message);
    }

    [Fact]
    public void TestNumberRefine()
    {
        var schema = I.Number().Refine(n => (int)n % 2 == 0, "must be even");

        schema.Validate(4.0);
        
        var ex = Assert.Throws<Exception>(() => schema.Validate(3.0));
        Assert.Contains("must be even", ex.Message);
    }

    // Boolean Schema Tests

    [Fact]
    public void TestBooleanRequired()
    {
        var schema = I.Boolean();

        schema.Validate(true);
        schema.Validate(false);
        
        Assert.Throws<Exception>(() => schema.Validate(null));
    }

    [Fact]
    public void TestBooleanOptional()
    {
        var schema = I.Bool().Optional();

        schema.Validate(null);
    }

    [Fact]
    public void TestBooleanDefault()
    {
        var schema = I.Boolean().Default(true);

        var (def, hasDefault) = schema.GetDefault();
        Assert.True(hasDefault);
        Assert.Equal(true, def);
    }

    // Null Schema Tests

    [Fact]
    public void TestNullSchema()
    {
        var schema = I.Null();

        schema.Validate(null);
        
        var ex = Assert.Throws<Exception>(() => schema.Validate("not null"));
        Assert.Contains("expected null", ex.Message);
    }

    // Reference Schema Tests

    [Fact]
    public void TestRefRequired()
    {
        var schema = I.Ref();

        schema.Validate(":1");
        schema.Validate(new Dictionary<string, object?> { ["_ref"] = "1" });
        
        Assert.Throws<Exception>(() => schema.Validate(null));
    }

    [Fact]
    public void TestRefOptional()
    {
        var schema = I.Reference().Optional();

        schema.Validate(null);
    }

    [Fact]
    public void TestRefNamespace()
    {
        var schema = I.Ref().Namespace("user");

        schema.Validate(new Dictionary<string, object?>
        {
            ["_ref"] = "1",
            ["_namespace"] = "user"
        });
        
        Assert.Throws<Exception>(() => schema.Validate(new Dictionary<string, object?>
        {
            ["_ref"] = "1",
            ["_namespace"] = "other"
        }));
    }

    [Fact]
    public void TestRefRelationship()
    {
        var schema = I.Ref().Relationship("OWNS");

        schema.Validate(new Dictionary<string, object?>
        {
            ["_ref"] = "1",
            ["_relationship"] = "OWNS"
        });
        
        Assert.Throws<Exception>(() => schema.Validate(new Dictionary<string, object?>
        {
            ["_ref"] = "1",
            ["_relationship"] = "OTHER"
        }));
    }

    [Fact]
    public void TestRefStringFormat()
    {
        var schema = I.Ref();

        schema.Validate(":1");
        schema.Validate(":user:42");
        
        var ex = Assert.Throws<Exception>(() => schema.Validate("not-a-ref"));
        Assert.Contains("expected reference string starting with ':'", ex.Message);
    }

    // Object Schema Tests

    [Fact]
    public void TestObjectRequired()
    {
        var schema = I.Object(new Dictionary<string, ISchema>
        {
            ["name"] = I.String(),
            ["age"] = I.Int()
        });

        schema.Validate(new Dictionary<string, object?>
        {
            ["name"] = "Alice",
            ["age"] = (long)30
        });
        
        Assert.Throws<Exception>(() => schema.Validate(null));
    }

    [Fact]
    public void TestObjectFieldValidation()
    {
        var schema = I.Object(new Dictionary<string, ISchema>
        {
            ["name"] = I.String().Min(1),
            ["email"] = I.String().Email()
        });

        schema.Validate(new Dictionary<string, object?>
        {
            ["name"] = "Alice",
            ["email"] = "alice@example.com"
        });
        
        var ex = Assert.Throws<ValidationErrors>(() => schema.Validate(new Dictionary<string, object?>
        {
            ["name"] = "",
            ["email"] = "invalid"
        }));
        Assert.Equal(2, ex.Errors.Count);
    }

    [Fact]
    public void TestObjectOptionalField()
    {
        var schema = I.Object(new Dictionary<string, ISchema>
        {
            ["name"] = I.String(),
            ["email"] = I.String().Optional()
        });

        schema.Validate(new Dictionary<string, object?>
        {
            ["name"] = "Alice"
        });
    }

    [Fact]
    public void TestObjectExtend()
    {
        var baseSchema = I.Object(new Dictionary<string, ISchema>
        {
            ["id"] = I.Int(),
            ["name"] = I.String()
        });

        var extendedSchema = baseSchema.Extend(new Dictionary<string, ISchema>
        {
            ["email"] = I.String().Email()
        });

        extendedSchema.Validate(new Dictionary<string, object?>
        {
            ["id"] = (long)1,
            ["name"] = "Alice",
            ["email"] = "alice@example.com"
        });
    }

    [Fact]
    public void TestObjectPick()
    {
        var schema = I.Object(new Dictionary<string, ISchema>
        {
            ["id"] = I.Int(),
            ["name"] = I.String(),
            ["email"] = I.String()
        });

        var pickedSchema = schema.Pick("id", "name");

        pickedSchema.Validate(new Dictionary<string, object?>
        {
            ["id"] = (long)1,
            ["name"] = "Alice"
        });
    }

    [Fact]
    public void TestObjectOmit()
    {
        var schema = I.Object(new Dictionary<string, ISchema>
        {
            ["id"] = I.Int(),
            ["name"] = I.String(),
            ["email"] = I.String()
        });

        var omittedSchema = schema.Omit("email");

        omittedSchema.Validate(new Dictionary<string, object?>
        {
            ["id"] = (long)1,
            ["name"] = "Alice"
        });
    }

    // Array Schema Tests

    [Fact]
    public void TestArrayRequired()
    {
        var schema = I.Array(I.String());

        schema.Validate(new List<object?> { "a", "b", "c" });
        
        Assert.Throws<Exception>(() => schema.Validate(null));
    }

    [Fact]
    public void TestArrayOptional()
    {
        var schema = I.Array(I.String()).Optional();

        schema.Validate(null);
    }

    [Fact]
    public void TestArrayMinLength()
    {
        var schema = I.Array(I.String()).Min(2);

        schema.Validate(new List<object?> { "a", "b" });
        
        var ex = Assert.Throws<Exception>(() => schema.Validate(new List<object?> { "a" }));
        Assert.Contains("at least 2", ex.Message);
    }

    [Fact]
    public void TestArrayMaxLength()
    {
        var schema = I.Array(I.String()).Max(2);

        schema.Validate(new List<object?> { "a", "b" });
        
        var ex = Assert.Throws<Exception>(() => schema.Validate(new List<object?> { "a", "b", "c" }));
        Assert.Contains("at most 2", ex.Message);
    }

    [Fact]
    public void TestArrayItemValidation()
    {
        var schema = I.Array(I.Int());

        schema.Validate(new List<object?> { (long)1, (long)2, (long)3 });
        
        var ex = Assert.Throws<ValidationErrors>(() => schema.Validate(new List<object?> { (long)1, "not an int", (long)3 }));
        Assert.Single(ex.Errors);
        Assert.Equal("[1]", ex.Errors[0].Field);
    }

    // Table Schema Tests

    [Fact]
    public void TestTableRequired()
    {
        var schema = I.Table("users", new Dictionary<string, ISchema>
        {
            ["id"] = I.Int(),
            ["name"] = I.String()
        });

        schema.Validate(new List<object?>
        {
            new Dictionary<string, object?> { ["id"] = (long)1, ["name"] = "Alice" },
            new Dictionary<string, object?> { ["id"] = (long)2, ["name"] = "Bob" }
        });
        
        Assert.Throws<Exception>(() => schema.Validate(null));
    }

    [Fact]
    public void TestTableOptional()
    {
        var schema = I.Table("users", new Dictionary<string, ISchema>
        {
            ["id"] = I.Int(),
            ["name"] = I.String()
        }).Optional();

        schema.Validate(null);
    }

    [Fact]
    public void TestTableRowValidation()
    {
        var schema = I.Table("users", new Dictionary<string, ISchema>
        {
            ["id"] = I.Int(),
            ["email"] = I.String().Email()
        });

        var ex = Assert.Throws<ValidationErrors>(() => schema.Validate(new List<object?>
        {
            new Dictionary<string, object?> { ["id"] = (long)1, ["email"] = "alice@example.com" },
            new Dictionary<string, object?> { ["id"] = (long)2, ["email"] = "invalid" }
        }));
        Assert.Single(ex.Errors);
        Assert.Contains("row[1]", ex.Errors[0].Field);
    }

    [Fact]
    public void TestTableBlockFormat()
    {
        var schema = I.Table("users", new Dictionary<string, ISchema>
        {
            ["id"] = I.Int(),
            ["name"] = I.String()
        });

        schema.Validate(new Dictionary<string, object?>
        {
            ["kind"] = "table",
            ["name"] = "users",
            ["rows"] = new List<object?>
            {
                new Dictionary<string, object?> { ["id"] = (long)1, ["name"] = "Alice" }
            }
        });
    }

    [Fact]
    public void TestTableGetName()
    {
        var schema = I.Table("users", new Dictionary<string, ISchema>());
        Assert.Equal("users", schema.GetName());
    }

    // Document Schema Tests

    [Fact]
    public void TestDocumentParse()
    {
        var schema = I.Document(new Dictionary<string, ISchema>
        {
            ["users"] = I.Table("users", new Dictionary<string, ISchema>
            {
                ["id"] = I.Int(),
                ["name"] = I.String()
            }),
            ["config"] = I.Object(new Dictionary<string, ISchema>
            {
                ["debug"] = I.Boolean()
            })
        });

        var doc = new Dictionary<string, object?>
        {
            ["users"] = new List<object?>
            {
                new Dictionary<string, object?> { ["id"] = (long)1, ["name"] = "Alice" }
            },
            ["config"] = new Dictionary<string, object?>
            {
                ["debug"] = true
            }
        };

        var result = schema.Parse(doc);
        Assert.NotNull(result);
    }

    [Fact]
    public void TestDocumentParseErrors()
    {
        var schema = I.Document(new Dictionary<string, ISchema>
        {
            ["users"] = I.Table("users", new Dictionary<string, ISchema>
            {
                ["id"] = I.Int(),
                ["email"] = I.String().Email()
            })
        });

        var doc = new Dictionary<string, object?>
        {
            ["users"] = new List<object?>
            {
                new Dictionary<string, object?> { ["id"] = (long)1, ["email"] = "invalid" }
            }
        };

        var ex = Assert.Throws<ValidationErrors>(() => schema.Parse(doc));
        Assert.Single(ex.Errors);
    }

    [Fact]
    public void TestDocumentSafeParse()
    {
        var schema = I.Document(new Dictionary<string, ISchema>
        {
            ["users"] = I.Table("users", new Dictionary<string, ISchema>
            {
                ["id"] = I.Int(),
                ["name"] = I.String()
            })
        });

        // Valid document
        var doc = new Dictionary<string, object?>
        {
            ["users"] = new List<object?>
            {
                new Dictionary<string, object?> { ["id"] = (long)1, ["name"] = "Alice" }
            }
        };

        var result = schema.SafeParse(doc);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);

        // Invalid document
        var invalidDoc = new Dictionary<string, object?>
        {
            ["users"] = new List<object?>
            {
                new Dictionary<string, object?> { ["id"] = "not-an-int", ["name"] = "Alice" }
            }
        };

        result = schema.SafeParse(invalidDoc);
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.NotNull(result.Error);
    }

    // I Namespace Tests

    [Fact]
    public void TestINamespace()
    {
        Assert.NotNull(I.String());
        Assert.NotNull(I.Number());
        Assert.NotNull(I.Int());
        Assert.NotNull(I.Float());
        Assert.NotNull(I.Boolean());
        Assert.NotNull(I.Bool());
        Assert.NotNull(I.Null());
        Assert.NotNull(I.Ref());
        Assert.NotNull(I.Reference());
        Assert.NotNull(I.Object(new Dictionary<string, ISchema>()));
        Assert.NotNull(I.Array(I.String()));
        Assert.NotNull(I.Table("test", new Dictionary<string, ISchema>()));
    }

    [Fact]
    public void TestINamespaceUsage()
    {
        // Example of using I namespace like Zod's z
        var userSchema = I.Table("users", new Dictionary<string, ISchema>
        {
            ["id"] = I.Int(),
            ["name"] = I.String().Min(1),
            ["email"] = I.String().Email(),
            ["active"] = I.Bool().Default(true)
        });

        userSchema.Validate(new List<object?>
        {
            new Dictionary<string, object?>
            {
                ["id"] = (long)1,
                ["name"] = "Alice",
                ["email"] = "alice@example.com",
                ["active"] = true
            }
        });
    }

    // ValidationError Tests

    [Fact]
    public void TestValidationErrorString()
    {
        var err = new ValidationError("email", "invalid email format", "not-an-email");

        Assert.Equal("email: invalid email format", err.ToString());
    }

    [Fact]
    public void TestValidationErrorsString()
    {
        var errs = new ValidationErrors(new List<ValidationError>
        {
            new("email", "invalid email"),
            new("name", "required")
        });

        Assert.Contains("email: invalid email", errs.Message);
        Assert.Contains("name: required", errs.Message);
    }

    [Fact]
    public void TestValidationErrorsHasErrors()
    {
        var empty = new ValidationErrors();
        Assert.False(empty.HasErrors);

        var withErrors = new ValidationErrors(new List<ValidationError>
        {
            new("test", "error")
        });
        Assert.True(withErrors.HasErrors);
    }
}
