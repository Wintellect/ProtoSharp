using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace ProtoSharp.Tests
{
    public class ProtoObjectBehavior
    {
        [Fact]
        public void ShouldBeAbleToCreateInstanceOfProtoObject()
        {
            var obj = new ProtoObject();
        }

        [Fact]
        public void ShouldReturnUndefinedForMembersThatDoNotExist()
        {
            dynamic obj = new ProtoObject();

            Assert.Equal(Proto.Undefined, obj.NoWhereElse);
        }

        [Fact]
        public void ShouldBeAbleToAddMembersDynamically()
        {
            dynamic obj = new ProtoObject();

            obj.Name = "Josh";

            Assert.Equal("Josh", obj.Name);

            obj.Age = 34;

            Assert.Equal(34, obj.Age);

            obj.Is21 = (Func<Boolean>)(() => obj.Age >= 21);

            Assert.True(obj.Is21());
        }

        [Fact]
        public void ShouldBeAbleToOverwriteMembers()
        {
            dynamic obj = new ProtoObject();

            obj.Name = "Josh";

            Assert.Equal("Josh", obj.Name);

            obj.Name = 56;

            Assert.Equal(56, obj.Name);
        }

        [Fact]
        public void PrototypeMembersShouldBeAvailableToAllObjects()
        {
            dynamic obj = new ProtoObject();
            dynamic obj2 = new ProtoObject();

            obj.Prototype.Name = "Josh";

            Assert.Equal("Josh", obj.Name);
            Assert.Equal("Josh", obj2.Name);
        }

        [Fact]
        public void PrototypeMembersShouldBeAvailableToDerivedTypes()
        {
            dynamic obj = new ProtoObject();

            obj.Prototype.Name = "Josh";

            dynamic person = new Person();
            dynamic emp = new Employee();
            dynamic sales = new Salesman();
            dynamic mgr = new Manager();

            Assert.Equal("Josh", person.Name);
            Assert.Equal("Josh", emp.Name);
            Assert.Equal("Josh", sales.Name);
            Assert.Equal("Josh", mgr.Name);
        }

        [Fact]
        public void PrototypeMembersShouldOnlyBeInherited()
        {
            dynamic person = new Person();
            dynamic emp = new Employee();
            dynamic sales = new Salesman();
            dynamic mgr = new Manager();

            emp.Prototype.Name = "Josh";

            Assert.Equal("Josh", sales.Name);
            Assert.Equal("Josh", mgr.Name);
            Assert.Equal(Proto.Undefined, person.NoWhereElse);
        }

        [Fact]
        public void InstanceMembersShouldOverridePrototypeMembers()
        {
            dynamic obj = new ProtoObject();
            dynamic obj2 = new ProtoObject();
            dynamic obj3 = new ProtoObject();

            obj.Prototype.Name = "Josh";
            obj2.Name = "Dan";
            obj3.Name = "Kathy";

            Assert.Equal("Josh", obj.Name);
            Assert.Equal("Dan", obj2.Name);
            Assert.Equal("Kathy", obj3.Name);
        }

        [Fact]
        public void ShouldGetMembersFromMostDerivedType()
        {
            dynamic person = new Person();
            dynamic emp = new Employee();
            dynamic mgr = new Manager();

            person.Prototype.WhoAmI = (Func<String>)(() => "Person");
            mgr.Prototype.WhoAmI = (Func<String>)(() => "Manager");

            Assert.Equal("Person", person.WhoAmI());
            Assert.Equal("Person", emp.WhoAmI());
            Assert.Equal("Manager", mgr.WhoAmI());
        }

        [Fact]
        public void ShouldBeAbleToAccessInstanceFromCurrentContext()
        {
            dynamic obj = new ProtoObject();
            dynamic obj2 = new ProtoObject();
            dynamic obj3 = new ProtoObject();

            obj.Prototype.Name = "Josh";
            obj.Prototype.SayHello = (Func<String>)(() => {
                dynamic @this = Proto.CurrentContext;

                return String.Format("Hello, {0}!", @this.Name);
            });

            obj2.Name = "Dan";
            obj3.Name = "Kathy";
            
            Assert.Equal("Hello, Josh!", obj.SayHello());
            Assert.Equal("Hello, Dan!", obj2.SayHello());
            Assert.Equal("Hello, Kathy!", obj3.SayHello());
        }

        [Fact]
        public void ShouldMaintainContextEvenWhenNestingCalls()
        {
            dynamic obj = new ProtoObject();
            dynamic obj2 = new ProtoObject();

            obj.Prototype.PrintName = (Func<String>)(() => {
                return Proto.CurrentContext.Name;
            });

            obj.Prototype.PrintAge = (Func<String>)(() => {
                return Proto.CurrentContext.Age;
            });

            obj.Name = "OBJ_1";
            obj.Age = "32";
            obj2.Name = "OBJ_2";
            obj2.Age = "12";

            obj.PrintAll = (Func<String>)(() => {
                return String.Format("{0}|{1}|{2}|{3}",
                    Proto.CurrentContext.PrintName(), obj2.PrintName(),
                    Proto.CurrentContext.PrintAge(), obj2.PrintAge());
            });

            Assert.Equal("OBJ_1|OBJ_2|32|12", obj.PrintAll());
        }

        [Fact]
        public void ShouldMaintainContextEvenWhenExceptionsAreThrownFromInnerCalls()
        {
            dynamic obj = new ProtoObject();
            dynamic obj2 = new ProtoObject();
                        
            obj.Prototype.PrintName = (Func<String>)(() => {
                return Proto.CurrentContext.Name;
            });

            obj.Prototype.Throws = (Action)(() => {
                throw new InvalidOperationException(String.Format("{0}: Threw an exception", Proto.CurrentContext.Name));
            });

            obj.Prototype.CheckValues = (Action)(() => {
                var name = Proto.CurrentContext.PrintName();

                try
                {
                    Proto.CurrentContext.Throws();
                    Assert.True(false, "Previous line should prevent this from executing");
                }
                catch(Exception ex)
                {
                    Assert.Equal(Proto.CurrentContext.Name + ": Threw an exception", ex.Message);
                }

                Assert.Equal(name, Proto.CurrentContext.Name);
            });

            obj.Name = "OBJ_1";
            obj2.Name = "OBJ_2";

            obj.CheckValues();
            obj2.CheckValues();
        }

        [Fact]
        public void ShouldClearCurrentContextEvenIfExceptionIsThrownDuringInvocation()
        {
            dynamic obj = new ProtoObject();

            obj.Prototype.Throw = (Action)(() => { throw new Exception("It Blew Up!"); });

            Assert.Null(Proto.CurrentContext);
            Assert.Throws<Exception>(() => obj.Throw());
            Assert.Null(Proto.CurrentContext);
        }

        [Fact]
        public void ShouldMaintainCorrectContextEvenWithMultipleThreads()
        {
            dynamic obj = new ProtoObject();
            obj.Prototype.Name = "";
            obj.Prototype.Say = (Func<String>)(() => {
                dynamic @this = Proto.CurrentContext;

                return String.Format("Say: {0}", @this.Name);
            });

            var objects = new List<dynamic>();

            //Using 1000 here to ensure at least 2 threads are used
            for (int i = 0; i < 1000; i++)
            {
                dynamic newObj = new ProtoObject();
                newObj.Name = i;
                objects.Add(newObj);
            }

            Parallel.ForEach(objects, target => {
                var expected = "Say: " + target.Name;
                Assert.Equal(expected, target.Say());
            });
        }
    }

    public class Person : ProtoObject { }
    public class Employee : Person { }
    public class Salesman : Employee { }
    public class Manager : Employee { }
}
