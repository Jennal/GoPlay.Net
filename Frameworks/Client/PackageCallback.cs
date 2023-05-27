using System.Reflection;
using GoPlay.Services.Core.Protocols;

namespace GoPlay.Services
{
    public class PackageCallback
    {
        public object Task { get; private set; }

        public PackageCallback(object task)
        {
            Task = task;
        }

        public void Invoke(Package pack)
        {
            var dataType = Task.GetType().GenericTypeArguments[0];
            var method = typeof(Package).GetMethod("ParseFromRaw", BindingFlags.Static | BindingFlags.Public);
            method = method.MakeGenericMethod(dataType);
            var data = method.Invoke(null, new object[] {pack});

            //task set result
            method = Task.GetType().GetMethod("SetResult", BindingFlags.Instance | BindingFlags.Public);
            method.Invoke(Task, new object[] {data});
        }
    }
}