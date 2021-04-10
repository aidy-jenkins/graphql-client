using System;
using Xunit;
using GraphQlClient;

namespace GraphQlClient.UnitTests
{
    public class RequestBuilderTests
    {
        private IGraphQlQueryBuilder _queryBuilder;

        public RequestBuilderTests() 
        {
            _queryBuilder = new GraphQlQueryBuilder();
        }

        [Fact]
        public void RequestBuilder_ShouldAcceptAnonymousType()
        {
            _queryBuilder.Build(new {
                Aidys = new [] {
                    new {
                        Foo = 1
                    }
                }
            });
        }

        [Fact]
        public void RequestBuilder_ShouldReturnSimpleQuery()
        {
            var expectedQuery = "{id name}";
            var request = _queryBuilder.Build
            (
                new {
                    Id = default(int),
                    Name = default(string)
                }
            );

            Assert.Equal(expectedQuery, request.GetQuery());
        }

        [Fact]
        public void RequestBuilder_ShouldHandleObjectFieldSelection()
        {
            var expectedQuery = "{id foo{bar baz}}";
            var request = _queryBuilder.Build
            (
                new {
                    Id = default(int),
                    Foo = new { 
                        Bar = default(string),
                        Baz = default(string)
                    }
                }
            );

            Assert.Equal(expectedQuery, request.GetQuery());
        }

        [Fact]
        public void RequestBuilder_ShouldHandleCollections()
        {
            var expectedQuery = "{id foo{bar baz}}";
            var request = _queryBuilder.Build
            (
                new {
                    Id = default(int),
                    Foo = new[] {
                        new { 
                            Bar = default(string),
                            Baz = default(string)
                        }
                    }
                }
            );

            Assert.Equal(expectedQuery, request.GetQuery());
        }

        [Fact]
        public void RequestBuilder_ShouldAllowParameters() 
        {
            var expectedQuery = "{foo(x:7)}";
            var request = _queryBuilder.Build(
                new {
                    Foo = default(int)
                }
            );
            request.Field(x => x.Foo).AddParameter("x", 7);

            Assert.Equal(expectedQuery, request.GetQuery());
        }

        [Fact]
        public void RequestBuilder_ShouldAllowMultipleParameters() 
        {
            var expectedQuery = "{foo(x:7,bar:\"baz\",date:\"2021-03-19\")}";
            var request = _queryBuilder.Build(
                new {
                    Foo = default(int)
                }
            );
            request.Field(x => x.Foo)
                .AddParameter("x", 7)
                .AddParameter("bar", "baz")
                .AddParameter("date", "2021-03-19");

            Assert.Equal(expectedQuery, request.GetQuery());
        }

        [Fact]
        public void RequestBuilder_ShouldSupportAliasingAField() 
        {
            var expectedQuery = "{foo:bar{id} bar{id}}";
            var request = _queryBuilder.Build(new {
                Foo = new {
                    Id = default(int)
                },
                Bar = new {
                    Id = default(int)
                }
            });

            request.Field(x => x.Foo).IsAliasFor("bar");
            
            Assert.Equal(expectedQuery, request.GetQuery());
        }

        [Fact]
        public void RequestBuilder_ShouldSupportAliasingAFieldWithParameters() 
        {
            var expectedQuery = "{foo:bar(id:7){id} bar:fubar(name:\"maxbar\"){id}}";
            var request = _queryBuilder.Build(new {
                Foo = new {
                    Id = default(int)
                },
                Bar = new {
                    Id = default(int)
                }
            });

            request.Field(x => x.Foo)
                .IsAliasFor("bar")
                .AddParameter("id", 7);

            request.Field(x => x.Bar)
                .AddParameter("name", "maxbar")
                .IsAliasFor("fubar");
            
            Assert.Equal(expectedQuery, request.GetQuery());
        }

        [Fact]
        public void RequestBuilder_ShouldAllowDefinedType() 
        {
            var expectedQuery = "{foo bar}";
            var request = _queryBuilder.Build<DefinedTypeTest>();
            
            Assert.Equal(expectedQuery, request.GetQuery());
        }

        [Fact]
        public void RequestBuilder_ShouldHandleEnumParameters() 
        {
            var expectedQuery = "{foo(type:SOMEVALUE,otherType:SOMEOTHERVALUE)}";
            var request = _queryBuilder.Build(new {
                Foo = null as string[]
            });

            request.Field(q => q.Foo)
                    .AddParameter("type", EnumTest.SomeValue)
                    .AddParameter("otherType", EnumTest.SomeOtherValue);
            
            Assert.Equal(expectedQuery, request.GetQuery());
        }

        class DefinedTypeTest 
        {
            public int Foo {get; set;} //property

            public string Bar; //field
        }

        enum EnumTest 
        {
            SomeValue,
            SomeOtherValue
        }
    }
}
