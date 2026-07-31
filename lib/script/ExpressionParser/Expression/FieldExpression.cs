#region License
//=============================================================================
// Iridium Script - Portable .NET Productivity Library 
//
// Copyright (c) 2008-2018 Philippe Leybaert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//=============================================================================
#endregion

using System;
using System.Linq;
using System.Reflection;

namespace Iridium.Script
{
    public class FieldExpression : Expression
    {
        public Expression Target { get; }
        public string Member { get; }

        public FieldExpression(Expression target, string member)
        {
            Target = target;
            Member = member;
        }

        public override ValueExpression Evaluate(IParserContext context)
        {
        	return Evaluate(context, false, null);
        }

		private ValueExpression Evaluate(IParserContext context, bool assign, object? newValue)
    	{
    		ValueExpression targetValue = Target.Evaluate(context);
    		object? targetObject;
    		Type? targetType;

    		if (targetValue.Value is TypeName typeName)
    		{
    			targetType = typeName.Type;
    			targetObject = null;
    		}
    		else
    		{
    			targetType = targetValue.Type;
    			targetObject = targetValue.Value;

                if (targetObject == null)
                    return Exp.Value(null, targetType);
    		}

            /*
            if (targetObject is IDynamicObject dynamicObject)
            {
                if (dynamicObject.TryGetValue(Member, out var value, out var type) && value is IDynamicObject dynField && dynField.IsValue && dynField.TryGetValue(out var fieldValue, out var fieldType))
                    return Exp.Value(fieldValue, fieldType);
                else
                    return Exp.Value(value, type);
            }
            */

            targetType = targetType.RealType();

            MemberInfo[] members = FindMemberInHierarchy(targetType, Member, (context.Behavior & ParserContextBehavior.CaseInsensitiveMembers) == ParserContextBehavior.CaseInsensitiveMembers);

    		if (members.Length == 0)
    		{
                PropertyInfo indexerPropInfo = targetType.FindIndexer([typeof(string)]);

                if (indexerPropInfo != null)
                {
                    return Exp.Value(indexerPropInfo.GetValue(targetObject, [Member]), indexerPropInfo.PropertyType);
                }

    			throw new UnknownPropertyException("Unknown property " + Member + " for object " + Target + " (type " + targetType.Name + ")", this);
    		}

    		if (members.Length >= 1 && members[0] is MethodInfo)
    		{
    			if (targetObject == null)
                    return Exp.Value(new StaticMethod(targetType, Member));
    			else
                    return Exp.Value(new InstanceMethod(targetType, Member, targetObject));
    		}

    		MemberInfo member = members[0];

    		if (members.Length > 1 && targetObject != null) // CoolStorage, ActiveRecord and Dynamic Proxy frameworks sometimes return > 1 member
    		{
    			foreach (MemberInfo mi in members)
    				if (mi.DeclaringType == targetObject.GetType())
    					member = mi;
    		}

	        if (member is FieldInfo fieldInfo)
	        {
                if (assign)
                    fieldInfo.SetValue(targetObject, newValue);

	            return Exp.Value(fieldInfo.GetValue(targetObject), fieldInfo.FieldType);
	        }

	        if (member is PropertyInfo propertyInfo)
	        {
	            if (assign)
	                propertyInfo.SetValue(targetObject, newValue, null);

	            return Exp.Value(propertyInfo.GetValue(targetObject, null), propertyInfo.PropertyType);
	        }

    		throw new ExpressionEvaluationException(Member + " is not a field or property", this);
    	}

        private static MemberInfo[] FindMemberInHierarchy(Type type, string name, bool caseInsensitive = false)
        {
            Type? t = type;

            var stringComparison = caseInsensitive ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;

            while (t != null)
            {
                MemberInfo[] members = t.GetMembers().Where(m => string.Equals(m.Name, name, stringComparison)).ToArray();

                if (members.Length > 0)
                    return members;

                t = t.BaseType;
            }

            return [];
        }

#if DEBUG
    	public override string ToString()
        {
            return $"({Target}.{Member})";
        }
#endif

    	public ValueExpression Assign(IParserContext context, object? value)
    	{
    		return Evaluate(context, true, value);
    	}
    }
}