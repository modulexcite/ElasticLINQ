﻿// Licensed under the Apache 2.0 License. See LICENSE.txt in the project root for more information.

using ElasticLinq.Request.Criteria;
using ElasticLinq.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace ElasticLinq.Mapping
{
    public enum EnumFormat { Integer, String };

    /// <summary>
    /// A base class for mapping Elasticsearch values that can lower-case all field values
    /// (and respects <see cref="NotAnalyzedAttribute"/> to opt-out of the lower-casing), 
    /// camel-case field names, and camel-case and pluralize type names.
    /// </summary>
    public class ElasticMapping : IElasticMapping
    {
        private readonly bool camelCaseFieldNames;
        private readonly bool camelCaseTypeNames;
        private readonly CultureInfo conversionCulture;
        private readonly bool lowerCaseAnalyzedFieldValues;
        private readonly bool pluralizeTypeNames;
        private readonly EnumFormat enumFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElasticMapping"/> class.
        /// </summary>
        /// <param name="camelCaseFieldNames">Pass <c>true</c> to automatically camel-case field names (for <see cref="GetFieldName(System.Reflection.MemberInfo)"/>).</param>
        /// <param name="camelCaseTypeNames">Pass <c>true</c> to automatically camel-case type names (for <see cref="GetDocumentType"/>).</param>
        /// <param name="pluralizeTypeNames">Pass <c>true</c> to automatically pluralize type names (for <see cref="GetDocumentType"/>).</param>
        /// <param name="lowerCaseAnalyzedFieldValues">Pass <c>true</c> to automatically convert field values to lower case (for <see cref="FormatValue"/>).</param>
        /// <param name="enumFormat">Pass <c>EnumFormat.String</c> to format enums as strings or <c>EnumFormat.Integer</c> to use integers (defaults to string).</param>
        /// <param name="conversionCulture">The culture to use for the lower-casing, camel-casing, and pluralization operations. If <c>null</c>,
        /// uses <see cref="CultureInfo.CurrentCulture"/>.</param>
        public ElasticMapping(bool camelCaseFieldNames = true,
                              bool camelCaseTypeNames = true,
                              bool pluralizeTypeNames = true,
                              bool lowerCaseAnalyzedFieldValues = true,
                              EnumFormat enumFormat = EnumFormat.String,
                              CultureInfo conversionCulture = null)
        {
            this.camelCaseFieldNames = camelCaseFieldNames;
            this.camelCaseTypeNames = camelCaseTypeNames;
            this.pluralizeTypeNames = pluralizeTypeNames;
            this.lowerCaseAnalyzedFieldValues = lowerCaseAnalyzedFieldValues;
            this.conversionCulture = conversionCulture ?? CultureInfo.CurrentCulture;
            this.enumFormat = enumFormat;
        }

        /// <inheritdoc/>
        public virtual JToken FormatValue(MemberInfo member, object value)
        {
            Argument.EnsureNotNull("member", member);

            if (value == null)
                return new JValue((string)null);

            if (enumFormat == EnumFormat.String)
                value = ReformatValueIfEnum(member, value);

            var result = JToken.FromObject(value);

            if (lowerCaseAnalyzedFieldValues && result.Type == JTokenType.String && !IsNotAnalyzed(member))
            {
                var loweredString = conversionCulture.TextInfo.ToLower(result.Value<string>());
                result = new JValue(loweredString);
            }

            return result;
        }

        private static object ReformatValueIfEnum(MemberInfo member, object value)
        {
            var returnType = TypeHelper.GetReturnType(member);
            if (!returnType.GetTypeInfo().IsEnum) return value;

            var nameValue = Enum.GetName(returnType, value);
            if (nameValue == null)
                throw new ArgumentOutOfRangeException("value",
                    String.Format("Value '{0}' is not defined for enum type '{1}'.", value, returnType.FullName));

            return nameValue;
        }

        /// <inheritdoc/>
        public virtual string GetFieldName(MemberExpression memberExpression)
        {
            Argument.EnsureNotNull("memberExpression", memberExpression);

            switch (memberExpression.Expression.NodeType)
            {
                case ExpressionType.MemberAccess:
                    return GetFieldName((MemberExpression)memberExpression.Expression) + "." + GetFieldName(memberExpression.Member);

                case ExpressionType.Parameter:
                    return GetFieldName(memberExpression.Member);

                default:
                    throw new NotSupportedException(String.Format("Unknown expression type {0} for left hand side of expression {1}", memberExpression.Expression.NodeType, memberExpression));
            }
        }

        /// <summary>
        /// Create the Elasticsearch field name for a given memberInfo.
        /// </summary>
        /// <param name="memberInfo">The member whose field name is required.</param>
        /// <returns>The Elasticsearch field name that matches the member.</returns>
        public virtual string GetFieldName(MemberInfo memberInfo)
        {
            Argument.EnsureNotNull("memberInfo", memberInfo);

            var memberName = GetMemberName(memberInfo);
            var prefix = GetDocumentMappingPrefix(memberInfo.DeclaringType);

            return String.Format("{0}.{1}", prefix, memberName).TrimStart('.');
        }

        protected string GetMemberName(MemberInfo memberInfo)
        {
            var jsonPropertyAttribute = memberInfo.GetCustomAttribute<JsonPropertyAttribute>(inherit: true);

            if (jsonPropertyAttribute != null)
                return jsonPropertyAttribute.PropertyName;

            return camelCaseFieldNames
                ? memberInfo.Name.ToCamelCase(conversionCulture)
                : memberInfo.Name;
        }

        /// <inheritdoc/>
        public virtual string GetDocumentMappingPrefix(Type type)
        {
            return null;
        }

        /// <inheritdoc/>
        public virtual string GetDocumentType(Type type)
        {
            Argument.EnsureNotNull("type", type);

            var result = type.Name;
            if (pluralizeTypeNames)
                result = result.ToPlural(conversionCulture);
            if (camelCaseTypeNames)
                result = result.ToCamelCase(conversionCulture);

            return result;
        }

        /// <inheritdoc/>
        public virtual ICriteria GetTypeSelectionCriteria(Type docType)
        {
            Argument.EnsureNotNull("docType", docType);
            return null;
        }

        /// <summary>
        /// Determine whether a field is "not analyzed". By default, looks for the member to be
        /// decorated with the <see cref="NotAnalyzedAttribute"/>.
        /// </summary>
        /// <param name="member">The member to evaluate.</param>
        /// <returns>Returns <c>true</c> if the field is not analyzed; <c>false</c>, otherwise.</returns>
        protected virtual bool IsNotAnalyzed(MemberInfo member)
        {
            return member.GetCustomAttribute<NotAnalyzedAttribute>(inherit: true) != null;
        }
    }
}
