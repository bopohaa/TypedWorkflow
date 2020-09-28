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
> For such a system to work, one mandatory rule must be observed - **uniqueness of the return type of the handler method **.

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
> The second mandatory rule is that - **all return types must be in the input parameters of the handler methods **.

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
Dependency injection is supported only in Constructor Injection classes that contain marked-up handler methods.
This can be done through the implementation of the `TypedWorkflow.IResolver` interface and passing an instance of such a class when creating a container.
```C#
var resolver = new MyDiResolver();
var builder = new TwContainerBuilder();
var container = builder
    .AddAssemblies(typeof(SimpleComponent).Assembly)
    .RegisterExternalDi(resolver)
    .Build();
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

## Performance issues
* Return instances of classes, not structures (the mechanism of boxing and unboxing in cases of calling handler methods through the use of reflection nullifies all the advantages of structures).
* If possible, use `singlеton` components (for this you need to mark a class with handler methods with a special attribute` TwSingletonAttribute`).

## Working use cases
Can be found in the project `TypedWorkflowTests` in the class` WorkflowBuilderTest` (all components are located in the folder `Components` of the specified project)

### Credits
Icons made by [Freepik](https://www.flaticon.com/authors/freepik) from [www.flaticon.com](https://www.flaticon.com/)