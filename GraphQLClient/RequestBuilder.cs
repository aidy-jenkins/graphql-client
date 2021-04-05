using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQLClient 
{
    public static class Query 
    {
        public static Query<T> Build<T>(T shape = default) => new Query<T>(shape);

        public static void RegisterScalarType(Type type) 
        {
            if(!ScalarTypes.Contains(type))
                ScalarTypes.Add(type);
        }

        internal static HashSet<Type> ScalarTypes = new HashSet<Type>
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

    public class Query<T>
    {
        private Dictionary<MemberInfo, string> Aliases = new Dictionary<MemberInfo, string>();
        private Dictionary<MemberInfo, Dictionary<string, object>> Parameters = new Dictionary<MemberInfo, Dictionary<string, object>>();

        public T Shape { get; }
        public Query(T shape) {}

        private static IEnumerable<(string Name, Type Type, MemberInfo Member)> GetFields(Type type)
            => type.GetProperties()
                    .Select(property => (property.Name, Type: property.PropertyType, Member: property as MemberInfo))
                .Union(type.GetFields()
                    .Select(field => (field.Name, Type: field.FieldType, Member: field as MemberInfo)));

        private static bool IsScalar(Type type) 
            => Query.ScalarTypes.Contains(type);

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

        public MetaMemberInfo Field<TOut>(Expression<Func<T, TOut>> fieldSelector)
        {
            var body = fieldSelector.Body as MemberExpression;
            if(body == null)
                throw new ArgumentException("Unsupported field selector expression");

            return new MetaMemberInfo { Member = body.Member, Query = this };
        }

        public string GetQuery() 
        {
            return TypeQuery(typeof(T));
        }

        public class MetaMemberInfo 
        {
            public Query<T> Query {get; set;}

            public MemberInfo Member {get; internal set;}

            public MetaMemberInfo AddParameter(string name, object value) 
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

            public MetaMemberInfo IsAliasFor(string fieldName) 
            {
                Query.Aliases[this.Member] = fieldName;
                return this;
            }
        }
    }
}