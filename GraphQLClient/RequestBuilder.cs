using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQlClient 
{
    public interface IGraphQlQueryBuilder 
    {
        IGraphQlQuery<T> Build<T>(T shape = default);
    }

    public interface IGraphQlQuery<T> 
    {
        string GetQuery();

        IMetaMember Field<TOut>(Expression<Func<T, TOut>> fieldSelector);
    }

    public interface IMetaMember 
    {
        IMetaMember AddParameter(string parameterName, object parameterValue);
        IMetaMember IsAliasFor(string fieldName);
    }

    public class GraphQlQueryBuilder : IGraphQlQueryBuilder
    {
        public IGraphQlQuery<T> Build<T>(T shape = default) => new GraphQlQuery<T>(shape, ScalarTypes);

        public void RegisterScalarType(Type type) 
        {
            if(!ScalarTypes.Contains(type))
                ScalarTypes.Add(type);
        }

        private HashSet<Type> ScalarTypes = new HashSet<Type>
        {
            typeof(int),
            typeof(short),
            typeof(long),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(string),
            typeof(bool),
            typeof(DateTime),
        };
    }

    public class GraphQlQuery<T> : IGraphQlQuery<T>
    {
        private Dictionary<MemberInfo, string> Aliases = new Dictionary<MemberInfo, string>();
        private Dictionary<MemberInfo, Dictionary<string, object>> Parameters = new Dictionary<MemberInfo, Dictionary<string, object>>();

        private HashSet<Type> _scalarTypes;

        public T Shape { get; }
        public GraphQlQuery(T shape, HashSet<Type> scalarTypes) 
        {
            _scalarTypes = scalarTypes;
        }

        private static IEnumerable<(string Name, Type Type, MemberInfo Member)> GetFields(Type type)
            => type.GetProperties()
                    .Select(property => (property.Name, Type: property.PropertyType, Member: property as MemberInfo))
                .Union(type.GetFields()
                    .Select(field => (field.Name, Type: field.FieldType, Member: field as MemberInfo)));

        private bool IsScalar(Type type) 
            => _scalarTypes.Contains(type);

        private static string PascalToCamel(string s)
            => string.IsNullOrEmpty(s) ? s : string.Concat(s.First().ToString().ToLower(), string.Join(string.Empty, s.Skip(1)));

        public string TypeQuery(Type type) 
        {
            if(IsScalar(type))
                return string.Empty;
                
            var enumerableType = type.GetInterfaces().FirstOrDefault(@interface 
                    => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if(enumerableType != null)
                type = enumerableType.GetGenericArguments().First();

            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = Nullable.GetUnderlyingType(type);

            if(IsScalar(type))
                return string.Empty;

            var fields = GetFields(type);
            var query = string.Join(' ', fields.Select(field => {
                var name = PascalToCamel(field.Name);
                var queryField = Aliases.GetValueOrDefault(field.Member) ?? string.Empty;
                if(!string.IsNullOrWhiteSpace(queryField))
                    queryField = $":{queryField}";

                var fieldQuery = TypeQuery(field.Type);
                
                var parameters = string.Join(',', Parameters.GetValueOrDefault(field.Member)?.Select(x => $"{x.Key}:{Newtonsoft.Json.JsonConvert.SerializeObject(x.Value)}") ?? new string[0]);
                if(!string.IsNullOrWhiteSpace(parameters))
                    parameters = $"({parameters})";

                return $"{name}{queryField}{parameters}{fieldQuery}";
            }));

            return $"{{{query}}}";
        }

        public string GetQuery() 
        {
            return TypeQuery(typeof(T));
        }

        public IMetaMember Field<TOut>(Expression<Func<T, TOut>> fieldSelector)
        {
            var body = fieldSelector.Body as MemberExpression;
            if(body == null)
                throw new ArgumentException("Unsupported field selector expression");

            return new MetaMemberInfo { Member = body.Member, Query = this };
        }

        public class MetaMemberInfo : IMetaMember
        {
            public GraphQlQuery<T> Query {get; set;}

            public MemberInfo Member {get; internal set;}

            public IMetaMember AddParameter(string name, object value) 
            {
                var parameters = Query.Parameters.GetValueOrDefault(Member);
                if(parameters == null) 
                {
                    parameters = new Dictionary<string, object>();
                    Query.Parameters.Add(Member, parameters);
                }
                
                parameters.Add(name, value);

                return this;
            }

            public IMetaMember IsAliasFor(string fieldName) 
            {
                Query.Aliases[this.Member] = fieldName;
                return this;
            }
        }
    }
}