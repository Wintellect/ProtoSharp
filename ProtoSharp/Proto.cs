using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProtoSharp
{
    public static class Proto
    {
        private static readonly UndefinedType _undefined = UndefinedType.Instance;

        public static dynamic Undefined
        {
            get { return _undefined; }
        }
        
        private static ThreadLocal<Stack<ProtoObject>> _context = new ThreadLocal<Stack<ProtoObject>>(() => new Stack<ProtoObject>());
        public static dynamic CurrentContext
        {
            get
            {
                return _context.Value.Count > 0 ? _context.Value.Peek() : null;
            }
        }

        internal static IDisposable CreateContext(ProtoObject context)
        {
            _context.Value.Push(context);
            return new ProtoContext(() => _context.Value.Pop());
        }

        class ProtoContext : IDisposable
        {
            private readonly Action _callback;

            public ProtoContext(Action callback)
            {
                _callback = callback;
            }

            public void Dispose()
            {
                _callback();
            }
        }
    }

    
    public sealed class UndefinedType
    {
        private static volatile UndefinedType instance;
        private static object syncRoot = new Object();

        private UndefinedType() { }

        public static UndefinedType Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new UndefinedType();
                    }
                }

                return instance;
            }
        }

        public override string ToString()
        {
            return "undefined";
        }
    }
}
