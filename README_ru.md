# Общее описание
Позволяет создать простую структуру рабочего процесса из специально размеченных методов-обработчиков, в которой типы входящих и исходящих данных являются связями между рабочими элементами.

Данная реализация представляет собой IoC framework, позволяющий выполнять последовательность специально размеченных методов класса, в порядке определяемом входящими и исходящими типами значений этих методов.
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
> Для работы такой системы необходимо соблюдать одно обязательное правило - **уникальность типа возвращаемого значения метода-обработчика**.

Следующий код не может быть выполнен:
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
> Второе обязательное правило заключается в том что - **все возвращаемые типы должны быть во входящих параметрах методов-обработчиках**.

При попытке выполнить эту схему framework выдаст исключение из-за наличия неиспользуемого параметра `int`
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
Каждый обработчик может вернуть несколько значений (типов) за одно выполнение в виде значимого кортежа.

Каждый тип в таком кортеже будет считаться отдельным результатом выполнения и может использоваться отдельно от соседних типов в качестве входящих значений в другие методы обработчики.

## Пример использования

```C#
var builder = new TwContainerBuilder();
var container = builder
    .AddAssemblies(typeof(SimpleComponent).Assembly)
    .Build();
container.Run().AsTask().Wait();
```
Данный код создаст контейнер для выполнения последовательности методов-обработчиков и выполнит один цикл работы с ожиданием результата выполнения.
Метод `Run` контейнера является потокобезопасным и может использоваться сколь угодно раз за время существования контейнера.

## Внедрение зависимостей (DI)
Использование внедрения зависимостей поддерживается только в конструкторах (Constructor Injection) классов которые содержат размеченные методы-обработчики.
Это можно сделать через реализацию интерфейса `TypedWorkflow.IResolver` и передачу экземпляра такого класса при создании контейнера
```C#
var resolver = new MyDiResolver();
var builder = new TwContainerBuilder();
var container = builder
    .AddAssemblies(typeof(SimpleComponent).Assembly)
    .RegisterExternalDi(resolver)
    .Build();
```

## Передача параметров из внешнего окружения
На каждом цикле работы контейнера можно вводить в систему произвольное количество значений
```C#
var builder = new TwContainerBuilder();
var container = builder
    .AddAssemblies(typeof(SimpleComponent).Assembly)
    .Build<(int, float)>();
container.Run((1, 2.0)).AsTask().Wait();
```
Необходимо что бы хотя бы один метод-обработчик имел зависимость от вводимых значений.
```C#
public class SimpleComponent
{
    [TwEntrypoint]
    public void Run(int i, float f){}
}
```

## Возвращение результатов работы системы во внешнее окружение
По завершению работы система может сформировать значение с указанным типом как результат работы всей системы
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

## Вопросы производительности
* Возвращайте экземпляры классов, а не структур (механизм boxing и unboxing в случаях вызова методов-обработчиков через использование рефлексии сводит все преимущества структур к нулю).
* По возможности используйте `singlеton` компоненты (для этого необходимо класс с методами-обработчиками пометить специальным атрибутом `TwSingletonAttribute`).

## Рабочие примеры использования
Можно найти в проекте `TypedWorkflowTests` в классе `WorkflowBuilderTest` (все компоненты размещены в папке `Components` указанного проекта)