# graphql-client
### GraphQL query generator for .NET

Allows you to automatically generate GraphQL queries from C# variables and types

## Usage

Instantiate a ```GraphQlQueryBuilder``` from the ```GraphQlClient``` namespace (you can also use the ```IGraphQlQueryBuilder``` interface for service pattern and to support mocking)

```csharp
using GraphQlClient;

class User //Example query type
{
    public int Id;
    public string Name;
}

IGraphQlQueryBuilder queryBuilder = new GraphQlQueryBuilder();
var query = queryBuilder.Build<User>();

//query is the string "{id name}"
```

Supports anonymous types for inline definitions (this is particularly useful when writing tests)

### Example 1

```csharp
var request = queryBuilder.Build
(
    new 
    {
        User = new 
        {
            Id = default(int),
            Name = default(string)
        }
    }
);

```

### Example 2

```csharp
var expectedResult = new 
{
    Users = new [] 
    {
        new 
        {
            Name = "Tom"
        },
        new 
        {
            Name = "Harry"
        }
    }
};

var query = queryBuilder.Build(expectedResult);

var actual = await SendRequest(query);

// assert expected equal to actual
```

## Fluent interface for parameters and field aliases

```csharp

class UserQuery
{
    public User User;
}

public async Task<User> GetUser(int userId)
{
    var query = queryBuilder.Build<UserQuery>();

    query
        .Field(q => q.User)
        .AddParameter("id", userId);

    return (await SendRequest<UserQuery>(query)).User;
}

public async Task<User> GetTom()
{
    var tomShape = new { Tom = default(User)};
    var query = queryBuilder.Build();

    query.Field(q => q.Tom)
            .IsAliasFor("user")
            .AddParameter("name", "Tom");

    return (await SendRequest(shape: tomShape)).Tom;
}
```

### Dependencies

No third party dependencies used, written in .NET 5
&nbsp;
Uses xUnit and FluentAssertions for the test suite
