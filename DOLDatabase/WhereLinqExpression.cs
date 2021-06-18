using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DOL.Database
{
	public static class WhereLinqExpression
	{
		public static WhereClause CreateWhereClause<TObject>(Expression<Func<TObject, bool>> expr) where TObject : DataObject
		{
			var visitor = new LinqVisitor();
			visitor.Visit(expr);
			return visitor.GeneratedExpression;
		}

		private class LinqVisitor : ExpressionVisitor
		{
			private static Dictionary<ExpressionType, string> ExpressionTypeToSqlOperator = new Dictionary<ExpressionType, string>
			{
				{ExpressionType.Equal, "="},
				{ExpressionType.NotEqual, "!="},
				{ExpressionType.GreaterThan, ">"},
				{ExpressionType.GreaterThanOrEqual, ">="},
				{ExpressionType.LessThan, "<"},
				{ExpressionType.LessThanOrEqual, "<="},
			};

			public List<WhereClause> Expressions = new List<WhereClause>();

			public WhereClause GeneratedExpression
			{
				get
				{
					return Expressions.Aggregate((expr, cur) => expr.And(cur));
				}
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				if (node.Method.Name == "Contains")
				{
					if (node.Method.DeclaringType == typeof(Enumerable))
					{
						var column = DB.Column(GetColumnName(node.Arguments[1]));
						if (node.Method.GetGenericArguments().First() == typeof(Int32))
							Expressions.Add(column.IsIn((IEnumerable<int>) GetValue(node.Arguments[0])));
						else if (node.Method.GetGenericArguments().First() == typeof(String))
							Expressions.Add(column.IsIn((IEnumerable<string>) GetValue(node.Arguments[0])));
						else
							Expressions.Add(column.IsIn((IEnumerable<object>) GetValue(node.Arguments[0])));
					}
					else if (node.Method.DeclaringType?.GetInterfaces().Contains(typeof(ICollection<int>)) ?? false)
						Expressions.Add(DB.Column(GetColumnName(node.Arguments[0])).IsIn((IEnumerable<int>) GetValue(node.Object)));
					else if (node.Method.DeclaringType?.GetInterfaces().Contains(typeof(ICollection<string>)) ?? false)
						Expressions.Add(DB.Column(GetColumnName(node.Arguments[0])).IsIn((IEnumerable<string>) GetValue(node.Object)));
					else if (node.Method.DeclaringType?.GetInterfaces().Contains(typeof(IList)) ?? false)
						Expressions.Add(DB.Column(GetColumnName(node.Arguments[0])).IsIn((IEnumerable<object>) GetValue(node.Object)));
				}
				return base.VisitMethodCall(node);
			}

			protected override Expression VisitBinary(BinaryExpression node)
			{
				switch (node.NodeType)
				{
					case ExpressionType.AndAlso:
						Visit(node.Left);
						Visit(node.Right);
						return node;
					case ExpressionType.OrElse:
						var visitor = new LinqVisitor();
						visitor.Visit(node.Left);
						visitor.Visit(node.Right);
						Expressions.Add(visitor.Expressions.First().Or(visitor.Expressions.Last()));
						return node;
				}

				string sqlOp;
				if (!ExpressionTypeToSqlOperator.TryGetValue(node.NodeType, out sqlOp))
					return node;

				var value = GetValue(node.Right);
				if (value == null && node.NodeType == ExpressionType.Equal)
					Expressions.Add(DB.Column(GetColumnName(node.Left)).IsNull());
				else if (value == null && node.NodeType == ExpressionType.NotEqual)
					Expressions.Add(DB.Column(GetColumnName(node.Left)).IsNotNull());
				else
					Expressions.Add(new FilterExpression(GetColumnName(node.Left), sqlOp, value));

				return node;
			}

			protected override Expression VisitMember(MemberExpression node)
			{
				if (node.Type == typeof(Boolean))
					Expressions.Add(DB.Column(GetColumnName(node)).IsEqualTo(1));
				return base.VisitMember(node);
			}

			protected static string GetColumnName(Expression expr)
			{
				var prop = expr as MemberExpression;
				if (prop == null && expr is UnaryExpression unary)
					prop = unary.Operand as MemberExpression;
				if (prop != null)
					return prop.Member.Name;
				throw new NotImplementedException($"GetColumnName() for node type {expr.NodeType} is not implemented");
			}
			protected static object GetValue(Expression node)
			{
				if (node is ConstantExpression constant)
					return constant.Value;
				try
				{
					// we try to evaluate the expression to get a value
					return Expression.Lambda(node).Compile().DynamicInvoke();
				}
				catch {}

				throw new NotImplementedException($"GetValue() for node type {node.NodeType} is not implemented");
			}
		}
	}
}