using System;
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
				if (node.Method.Name.StartsWith("Contains") && node.Method.DeclaringType == typeof(Enumerable))
				{
					if (node.Method.GetGenericArguments().First() == typeof(Int32))
						Expressions.Add(DB.Column(GetColumnName(node.Arguments[1])).IsIn((IEnumerable<int>) GetValue(node.Arguments[0])));
					else if (node.Method.GetGenericArguments().First() == typeof(String))
						Expressions.Add(DB.Column(GetColumnName(node.Arguments[1])).IsIn((IEnumerable<string>) GetValue(node.Arguments[0])));
				}
				return base.VisitMethodCall(node);
			}

			protected override Expression VisitInvocation(InvocationExpression node)
			{
				Console.WriteLine("Invocation");
				return base.VisitInvocation(node);
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
				var prop = node as MemberExpression;
				if (prop == null && node is UnaryExpression unary)
					prop = unary.Operand as MemberExpression;

				if (prop != null)
					return Expression.Lambda(prop).Compile().DynamicInvoke();

				if (node is ConstantExpression constant)
					return constant.Value;

				if (node is NewArrayExpression arr)
					return Expression.Lambda(arr).Compile().DynamicInvoke();

				throw new NotImplementedException($"GetValue() for node type {node.NodeType} is not implemented");
			}
		}
	}
}