![TypedWorkflow Logo](/icon.png)

# General description
Allows you to create a simple workflow structure from specially marked up handler methods, in which the input and output types are the links between the work items.

This implementation is an IoC framework that allows you to execute a sequence of specially marked up class methods, in the order determined by the input and output value types of those methods.
```C#
public class SimpleComponent
{
    [TwEntrypoint]
    public int FirstRun()
        => 1;

    [TwEntrypoint]
    public float SecondRun(int i)
        => (float)i;
}

public class OtherSimpleComponent
{
    [TwEntrypoint]
    public void ThirdRun(float i){}
}

```
> For such a system to work, one mandatory rule must be observed - **uniqueness of the return type of the handler method**.

The following code cannot be executed:
```C#
public class SimpleComponent
{
    [TwEntrypoint]
    public int Run()
        => 1;
}
public class OtherSimpleComponent
{
    [TwEntrypoint]
    public int Run()
        => 2;
}
```
> The second mandatory rule is that - **all return types must be in the input parameters of the handler methods**.

When trying to execute this schema, the framework will throw an exception due to the presence of an unused `int` parameter
```C#
public class SimpleComponent
{
    [TwEntrypoint]
    public (int, float) Run()
        => (1, 2.0);
}
public class OtherSimpleComponent
{
    [TwEntrypoint]
    public void Run(float i){}
}

```
Each handler can return multiple values (types) in one execution as a value tuple.

Each type in such a tuple will be considered a separate result of execution and can be used separately from neighboring types as input values to other handler methods.

## Usage example

```C#
var builder = new TwContainerBuilder();
var container = builder
    .AddAssemblies(typeof(SimpleComponent).Assembly)
    .Build();
container.Run().AsTask().Wait();
```
This code will create a container for executing a sequence of handler methods and execute one cycle of work while waiting for the execution result.
The container's `Run` method is thread-safe and can be used any number of times during the lifetime of the container.

## Dependency Injection (DI)
Dependency injection is supported for constructors (Constructor Injection) and static methods of the `TwInject` tagged classes that contain the tagged handler methods.
This can be done through the implementation of the `TypedWorkflow.IResolver` interface and passing an instance of such a class when creating a container.
```C#
var resolver = new MyDiResolver();
var builder = new TwContainerBuilder();
var container = builder
    .AddAssemblies(typeof(SimpleComponent).Assembly)
    .RegisterExternalDi(resolver)
    .Build();
```

### Singleton components
It is possible to create static handler methods
```C#
public static class SingletonComponent
{
    [TwEntrypoint]
    public static void Run(int i, float f){}
}
```

## Passing parameters from the external environment
At each cycle of the container, an arbitrary number of values can be entered into the system
```C#
var builder = new TwContainerBuilder();
var container = builder
    .AddAssemblies(typeof(SimpleComponent).Assembly)
    .Build<(int, float)>();
container.Run((1, 2.0)).AsTask().Wait();
```
It is necessary that at least one handler method has a dependence on the input values.
```C#
public class SimpleComponent
{
    [TwEntrypoint]
    public void Run(int i, float f){}
}
```

## Returning the results of the system operation to the external environment
Upon completion of the work, the system can generate a value with the specified type as a result of the entire system
```C#
var builder = new TwContainerBuilder();
var container = builder
    .AddAssemblies(typeof(SimpleComponent).Assembly)
    .Build<int, float>();
float result = container.Run(2).AsTask().Result;
```
```C#
public class SimpleComponent
{
    [TwEntrypoint]
    public float Run(int i)
        => i * 2.0;
}
```

## Optional parameters
Skip execution of a handler method depending on the value of the result.
If the result model is wrapped in a `TypedWorkflow.Option` wrapper, then it will allow returning an empty value as a response.

Handler methods whose model values cannot be empty (not wrapped by `TypedWorkflow.Option`) will be skipped if the values of these models are empty.
Thus, all handler methods dependent on the execution result will be skipped.

```C#
public class SimpleComponent
{
    [TwEntrypoint]
    public TypedWorkflow.Option<int> Run()
        => TypedWorkflow.Option<float>.None;

    [TwEntrypoint]
    public float NeverRun(int i)
        => i * 2.0;

    [TwEntrypoint]
    public void NeverRun(float i) {}
}
```

## Execution constraints
They are implemented as the `TwConstraintAttribute` attribute and are intended to block the execution of a handler method based on the presence or absence of a given model value.
Using such restrictions, it is possible to implement the operation of the cache, this is when the presence of a value in the cache blocks the execution of an operation to obtain this value from its slower storage (for example, a database).
A constraint condition can be assigned both to the base or main class of the component, and to the handler method itself.
The result of a blocked handler method will have an empty value, so all handler methods dependent on this value will be skipped and their results will also have an empty value.
```C#
[TwConstraint(typeof(FromCache), HasNone = true)]
public class ConstrainedComponent
{
    [TwEntrypoint]
    public Option<FromDb> GetModelFromDb()
    {
        return Option.Create(new FromDb(new SomeModel()));
    }
}
```
A more detailed example can be found in the tests `TypedWorkflowTests.Components.11-ConstrainedComponent.cs`

## Performance issues
* Return instances of classes, not structures (the mechanism of boxing and unboxing in cases of calling handler methods through the use of reflection nullifies all the advantages of structures).
* Use `singlеton` components whenever possible (using static methods as handler methods).

## Working use cases
Can be found in the project `TypedWorkflowTests` in the class` WorkflowBuilderTest` (all components are located in the folder `Components` of the specified project)

### Credits
Icons made by [Freepik](https://www.flaticon.com/authors/freepik) from [www.flaticon.com](https://www.flaticon.com/)
