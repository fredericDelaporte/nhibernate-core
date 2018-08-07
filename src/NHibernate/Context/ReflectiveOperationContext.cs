using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NHibernate.Context
{
	/// <summary>
	/// This class allows access to the OperationContext without referring to System.ServiceModel at compile time.
	/// The accessors are cached as delegates for performance.
	/// </summary>
	public static class ReflectiveOperationContext
	{
		static ReflectiveOperationContext()
		{
			OperationContextType = System.Type.GetType(
				$"System.ServiceModel.OperationContext, System.ServiceModel, Version={Environment.Version}, " +
				"Culture=neutral, PublicKeyToken=b77a5c561934e089",
				true);
			var extensionsProperty =
				OperationContextType.GetProperty("Extensions", BindingFlags.Instance | BindingFlags.Public) ??
				throw new InvalidOperationException("Unable to find OperationContext.Extensions instance property");
			ExtensionsType = extensionsProperty.PropertyType;
			ExtensionElementType =
				ExtensionsType
					.FindInterfaces((i, c) => i.IsGenericType && i != ExtensionsType, null)
					.First().GenericTypeArguments[0];

			OperationContextCurrentGetter = CreateCurrentOperationContextGetter();
			OperationContextExtensionsGetter = CreateOperationContextExtensionsGetter();
			OperationContextExtensionsAdder = CreateOperationContextExtensionsAdder();
		}

		private static readonly System.Type OperationContextType;
		private static readonly System.Type ExtensionsType;
		private static readonly System.Type ExtensionElementType;

		public static Func<object> OperationContextCurrentGetter { get; }
		public static Func<object, object> OperationContextExtensionsGetter { get; }
		public static Action<object, object> OperationContextExtensionsAdder { get; }

		public static object OperationContextCurrentExtensions =>
			OperationContextExtensionsGetter(OperationContextCurrentGetter());

		public static Func<object, object> CreateOperationContextExtensionsFinder(System.Type extensionType)
		{
			var extensionsParam = Expression.Parameter(typeof(object), "extensions");
			var convertedParam = Expression.Convert(extensionsParam, ExtensionsType);
			var genericFindMethod = ExtensionsType.GetMethod("Find") ?? throw new InvalidOperationException("Unable to find Extensions.Find method");
			var findMethod = genericFindMethod.MakeGenericMethod(extensionType);
			var extensionResult = Expression.Call(convertedParam, findMethod);
			var convertedExpression = Expression.Convert(extensionResult, typeof(object));
			return (Func<object, object>) Expression.Lambda(convertedExpression, extensionsParam).Compile();
		}

		private static Func<object> CreateCurrentOperationContextGetter()
		{
			var currentProperty =
				OperationContextType.GetProperty(
					"Current",
					BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy) ??
				throw new InvalidOperationException("Unable to find OperationContext.Current static property");
			var propertyExpression = Expression.Property(null, currentProperty);
			var convertedExpression = Expression.Convert(propertyExpression, typeof(object));
			return (Func<object>) Expression.Lambda(convertedExpression).Compile();
		}

		private static Func<object, object> CreateOperationContextExtensionsGetter()
		{
			var contextParam = Expression.Parameter(typeof(object), "context");
			var convertedParam = Expression.Convert(contextParam, OperationContextType);
			var extensionsProperty = Expression.Property(convertedParam, "Extensions");
			var convertedExpression = Expression.Convert(extensionsProperty, typeof(object));
			return (Func<object, object>) Expression.Lambda(convertedExpression, contextParam).Compile();
		}

		private static Action<object, object> CreateOperationContextExtensionsAdder()
		{
			var extensionsParam = Expression.Parameter(typeof(object), "extensions");
			var convertedExtensionsParam = Expression.Convert(extensionsParam, ExtensionsType);
			var wcfStateParam = Expression.Parameter(typeof(object), "wcfState");
			var convertedWcfStateParam = Expression.Convert(wcfStateParam, ExtensionElementType);
			var addMethod = ExtensionsType.GetMethod("Add") ?? throw new InvalidOperationException("Unable to find Extensions.Add method");
			var addCall = Expression.Call(convertedExtensionsParam, addMethod, convertedWcfStateParam);
			return (Action<object, object>) Expression.Lambda(addCall, extensionsParam).Compile();
		}
	}
}
