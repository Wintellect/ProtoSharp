using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProtoSharp
{
    public class ProtoObject : DynamicObject
    {
        private Dictionary<String, Object> _members = new Dictionary<String, object>();
        private static readonly Dictionary<Type, ExpandoObject> _prototypes = new Dictionary<Type, ExpandoObject>();

        public ProtoObject()
        {
            if (Prototype == null)
            {
                _prototypes.Add(GetType(), new ExpandoObject());
            }
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            Object member = null;
            result = null;

            using (Proto.CreateContext(this))
            {
                var success = _members.TryGetValue(binder.Name, out member);

                if (success && member is Delegate)
                {
                    result = InvokeDelegate((Delegate)member, args);
                }

                if (!success)
                {
                    member = FindPrototypeMember(binder.Name, GetType());

                    if (member is Delegate)
                    {
                        result = InvokeDelegate((Delegate)member, args);
                        success = true;
                    }
                }

                return success;
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var success = _members.TryGetValue(binder.Name, out result);

            if (!success)
            {
                var member = FindPrototypeMember(binder.Name, GetType());

                if (member != null)
                {
                    result = member;
                    success = true;
                }
            }

            if (result == null)
            {
                result = Proto.Undefined;
                success = true;
            }

            return success;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (_members.ContainsKey(binder.Name))
                _members[binder.Name] = value;
            else
                _members.Add(binder.Name, value);

            return true;
        }

        private object InvokeDelegate(Delegate member, object[] args)
        {
            object result = null;

            try
            {
                result = member.DynamicInvoke(args);
            }
            catch (TargetInvocationException ex)
            {
                var newEx = typeof(Exception)
                    .GetMethod("PrepForRemoting", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(ex.InnerException, new object[0]);

                throw (Exception)newEx;
            }

            return result;
        }

        private object FindPrototypeMember(string memberName, Type type)
        {
            if (String.IsNullOrWhiteSpace(memberName) || type == null) return null;

            if (!_prototypes.ContainsKey(type)) return null;

            var prototype = _prototypes[type] as IDictionary<String, Object>;

            if (prototype.ContainsKey(memberName))
                return prototype[memberName];
            else
                return FindPrototypeMember(memberName, type.BaseType);
        }

        public dynamic Prototype
        {
            get
            {
                if (_prototypes.ContainsKey(GetType()))
                    return _prototypes[GetType()];
                else
                    return null;
            }
        }
    }
}
