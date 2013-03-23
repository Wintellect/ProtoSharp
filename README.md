ProtoSharp
==========

ProtoSharp allows you to simulate prototypal inheritence using the Dynamic Language Runtime features added in .Net 4

ProtoSharp is primarily a thought experiment, and an example of how we can augment C# with dynamic features using the DLR.

**What does it look like?**

By inheriting from `ProtoObject`, you can add new members to a type dynamically at runtime. 

```csharp
public class Foo: ProtoObject {}
public class Bar: Foo {}

dynamic myFoo = new Foo();
dynamic yourFoo = new Foo();
dynamic myBar = new Bar();
 
myFoo.Prototype.Name = "Josh";
myFoo.Prototype.SayHello = new Action(s => Console.WriteLine("Hello, " + s));
 
yourFoo.SayHello(myBar.Name); // 'Hello, Josh'
```

**Preserving member state***

Becuase ProtoSharp walks the inheritence chain, members can be overriden on any single instance.

```csharp
public class Parent: ProtoObject {}
public class Child: ProtoObject {}

dynamic dad = new Parent();
dynamic son = new Child();
dynamic daughter = new Child();

dad.Prototype.Name = "Josh";
daughter.Name = "Hadassah";

Console.Write(dad.Name); // "Josh"
Console.Write(son.Name); // "Josh"
Console.Write(daughter.Name); // "Hadassah"
```

**Checking for 'Undefined'**

You might need to check for the existence of a member before trying to use it. ProtoSharp makes this easy using the `Proto` helper class.

```csharp
dynamic obj = new ProtoObject();

//Evaluates to true since Name does not exist
if(obj.Name == Proto.Undefined){
   Console.Write("Name is Undefined");
}
```

**Accessing `this` from dynamically added methods**

Adding methods dynamically isn't all that useful unless you can access the current instance. Luckily the `Proto` helper class provides a thread safe way of accessing the current instance for dynamic methods.

```csharp
dynamic obj = new ProtoObject();
dynamic obj2 = new ProtoObject();
dynamic obj3 = new ProtoObject();

obj.Prototype.Name = "Josh";
obj.Prototype.SayHello = (Func<String>)(() => {
	//Get a reference to the current instance
	dynamic @this = Proto.CurrentContext;

	return String.Format("Hello, {0}!", @this.Name);
});

obj2.Name = "Dan";
obj3.Name = "Kathy";

Console.Write(obj.SayHello()); // "Hello, Josh!"
Console.Write(obj2.SayHello()); // "Hello, Dan!"
Console.Write(obj3.SayHello()); // "Hello, Kathy!"
```