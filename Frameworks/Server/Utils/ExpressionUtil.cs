using System.Linq.Expressions;
using System.Reflection;

namespace GoPlay.Services.Core.Utils
{
    public static class ExpressionUtil
    {
        // this delegate is just, so you don't have to pass an object array. _(params)_
        public delegate object CustomDelegate(params object[] args);

        public static CustomDelegate CreateMethod<T>(T inst, MethodInfo method)
        {
            // Get the parameters
            var parameters = method.GetParameters().Select(o => o.ParameterType);

            // define a object[] parameter
            var paramExpr = Expression.Parameter(typeof(Object[]));

            // To feed the constructor with the right parameters, we need to generate an array 
            // of parameters that will be read from the initialize object array argument.
            var callParameters =  parameters.Select((paramType, index) => //Expression.Parameter(paramType)
                // convert the object[index] to the right constructor parameter type.
                Expression.Convert(
                    // read a value from the object[index]
                    Expression.ArrayAccess(
                        paramExpr,
                        Expression.Constant(index)),
                    paramType)
                ).ToArray();

            // just call the constructor.
            var instance = Expression.Constant(inst);
            var body = Expression.Call(instance, method, callParameters);

            var constructor = Expression.Lambda<CustomDelegate>(body, paramExpr);
            return constructor.Compile();
        }
    }
}