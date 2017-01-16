using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LkeServices.Triggers.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

namespace LkeServices.Triggers.Bindings
{
    public class TriggerBindingCollector<T> where T : ITriggerBinding
    {
        public List<T> CollectFromEntryAssembly(IServiceProvider serviceProvider)
        {
            var defineAttribute = typeof(T).GetTypeInfo().GetCustomAttribute<TriggerDefineAttribute>();
            if (defineAttribute == null)
                throw new Exception("Type T must have TriggerDefineAttribute");
            
            return Assembly.GetEntryAssembly().GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(
                m => m.GetCustomAttribute(defineAttribute.Type, false) != null))
                .Select(m =>
                    {
                        var binding = serviceProvider.GetService<T>();
                        binding.InitBinding(serviceProvider, m);
                        return binding;
                    }).ToList();
        }

        public List<T> CollectFromAssemblies(List<Assembly> assemblies, IServiceProvider serviceProvider)
        {
            var defineAttribute = typeof(T).GetTypeInfo().GetCustomAttribute<TriggerDefineAttribute>();
            if (defineAttribute == null)
                throw new Exception("Type T must have TriggerDefineAttribute");

            return assemblies.SelectMany(x=>x.GetTypes()).SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(
                m => m.GetCustomAttribute(defineAttribute.Type, false) != null))
                .Select(m =>
                {
                    var binding = serviceProvider.GetService<T>();
                    binding.InitBinding(serviceProvider, m);
                    return binding;
                }).ToList();
        }
    }
}
