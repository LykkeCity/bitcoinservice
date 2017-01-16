using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using BackgroundWorker.Commands;

namespace BackgroundWorker.Handlers
{
    public class HandlersFactory
    {
        private readonly IComponentContext _context;

        private readonly Dictionary<CommandType, Type> _handlersMap = new Dictionary<CommandType, Type>();

        public HandlersFactory(IComponentContext context)
        {
            _context = context;
            //_handlersMap.Add(CommandType.GenerateFeeOutputs, typeof(GenerateFeeOutputsCommandHandler));
            ValidateMap();
        }

        private void ValidateMap()
        {
            foreach (var type in _handlersMap.Values)
            {
                if (!type.IsAssignableTo<IHandler>())
                    throw new Exception($"Type {type.FullName} doesn't implement IHandler");
            }
        }

        public IHandler Create(CommandType type)
        {
            if (!_handlersMap.ContainsKey(type))
                throw new Exception($"Unknown command type {type}");
            var handlerType = _handlersMap[type];
            return _context.Resolve(handlerType) as IHandler;
        }
    }
}
