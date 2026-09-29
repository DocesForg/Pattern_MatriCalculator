using System.Collections;
using System.Collections.Concurrent;

namespace MatrixCalculator;

// ЛР 1. Singleton и Builder.
public sealed class CalculatorSettings
{
    private CalculatorSettings() { }
    public static CalculatorSettings Instance { get; } = new();
    public int DecimalPlaces { get; set; } = 6;
}

public sealed class MatrixBuilder
{
    private readonly double[,] _values;
    public MatrixBuilder(int rows, int columns) => _values = new double[rows, columns];
    public MatrixBuilder WithValue(int row, int column, double value) { _values[row, column] = value; return this; }
    public Matrix Build() => new(_values);
}

// ЛР 2. Abstract Factory, Factory Method и Prototype.
public interface IMatrixOperation { Matrix Apply(Matrix left, Matrix? right = null); }
public sealed class AdditionOperation : IMatrixOperation { public Matrix Apply(Matrix left, Matrix? right = null) => left.Add(right ?? throw new ArgumentNullException(nameof(right))); }
public sealed class MultiplicationOperation : IMatrixOperation { public Matrix Apply(Matrix left, Matrix? right = null) => left.Multiply(right ?? throw new ArgumentNullException(nameof(right))); }
public interface IOperationFactory { IMatrixOperation CreateAddition(); IMatrixOperation CreateMultiplication(); }
public sealed class StandardOperationFactory : IOperationFactory
{
    public IMatrixOperation CreateAddition() => new AdditionOperation();
    public IMatrixOperation CreateMultiplication() => new MultiplicationOperation();
}
public abstract class OperationCreator
{
    public abstract IMatrixOperation Create();
    public Matrix Execute(Matrix left, Matrix right) => Create().Apply(left, right);
}
public sealed class AdditionCreator : OperationCreator { public override IMatrixOperation Create() => new AdditionOperation(); }
public sealed record CalculationRequest(Matrix Left, Matrix? Right, string Operation)
{
    public CalculationRequest Copy() => this with { Left = new MatrixCopy(Left).Value, Right = Right is null ? null : new MatrixCopy(Right).Value };
    private sealed class MatrixCopy
    {
        public Matrix Value { get; }
        public MatrixCopy(Matrix source) => Value = Matrix.Parse(source.ToString().Replace(Environment.NewLine, ";"));
    }
}

// ЛР 3--6. Структурные паттерны: Adapter, Bridge, Composite, Decorator, Facade, Flyweight, Proxy.
public sealed class MatrixTextAdapter
{
    public Matrix FromCsv(string csv) => Matrix.Parse(csv.Replace("\r", string.Empty).Replace("\n", ";").Replace(",", " "));
}
public interface IMatrixFormatter { string Format(Matrix matrix); }
public sealed class PlainMatrixFormatter : IMatrixFormatter { public string Format(Matrix matrix) => matrix.ToString(); }
public sealed class LabeledMatrixFormatter : IMatrixFormatter { public string Format(Matrix matrix) => $"Матрица {matrix.Rows}x{matrix.Columns}:{Environment.NewLine}{matrix}"; }
public abstract class MatrixView
{
    protected MatrixView(IMatrixFormatter formatter) => Formatter = formatter;
    protected IMatrixFormatter Formatter { get; }
    public abstract string Render(Matrix matrix);
}
public sealed class ConsoleMatrixView : MatrixView
{
    public ConsoleMatrixView(IMatrixFormatter formatter) : base(formatter) { }
    public override string Render(Matrix matrix) => Formatter.Format(matrix);
}
public interface IMatrixExpression { Matrix Evaluate(); }
public sealed class ValueExpression : IMatrixExpression { private readonly Matrix _value; public ValueExpression(Matrix value) => _value = value; public Matrix Evaluate() => _value; }
public sealed class SumExpression : IMatrixExpression
{
    private readonly List<IMatrixExpression> _children = [];
    public SumExpression Add(IMatrixExpression child) { _children.Add(child); return this; }
    public Matrix Evaluate() => _children.Skip(1).Aggregate(_children.First().Evaluate(), (sum, child) => sum.Add(child.Evaluate()));
}
public sealed class TimedOperationDecorator(IMatrixOperation inner) : IMatrixOperation
{
    public TimeSpan LastDuration { get; private set; }
    public Matrix Apply(Matrix left, Matrix? right = null)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var result = inner.Apply(left, right);
        watch.Stop(); LastDuration = watch.Elapsed;
        return result;
    }
}
public sealed class MatrixCalculatorFacade
{
    public Matrix Calculate(string left, string operation, string? right = null)
    {
        var first = Matrix.Parse(left);
        return operation.ToLowerInvariant() switch
        {
            "t" => first.Transpose(), "inv" => first.Inverse(),
            "+" => first.Add(Matrix.Parse(RequireRight(right))),
            "-" => first.Subtract(Matrix.Parse(RequireRight(right))),
            "*" => first.Multiply(Matrix.Parse(RequireRight(right))),
            _ => throw new ArgumentException("Операция не возвращает матрицу или не поддерживается.")
        };
    }
    public double Determinant(string matrix) => Matrix.Parse(matrix).Determinant();
    private static string RequireRight(string? value) => value ?? throw new ArgumentException("Для бинарной операции нужна вторая матрица.");
}
public sealed class OperationSymbol
{
    internal OperationSymbol(string value) => Value = value;
    public string Value { get; }
}
public sealed class OperationSymbolFactory
{
    private readonly Dictionary<string, OperationSymbol> _symbols = new();
    public OperationSymbol Get(string value) => _symbols.TryGetValue(value, out var symbol) ? symbol : _symbols[value] = new OperationSymbol(value);
}
public interface ICalculationService { Matrix Calculate(Matrix left, string operation, Matrix? right = null); }
public sealed class CalculationService : ICalculationService
{
    public Matrix Calculate(Matrix left, string operation, Matrix? right = null) => operation switch
    {
        "+" => left.Add(right ?? throw new ArgumentNullException(nameof(right))),
        "-" => left.Subtract(right ?? throw new ArgumentNullException(nameof(right))),
        "*" => left.Multiply(right ?? throw new ArgumentNullException(nameof(right))),
        "t" => left.Transpose(), "inv" => left.Inverse(), _ => throw new ArgumentException("Неизвестная операция.")
    };
}
public sealed class LoggingCalculationProxy(ICalculationService service, Action<string> log) : ICalculationService
{
    public Matrix Calculate(Matrix left, string operation, Matrix? right = null)
    {
        log($"Выполняется операция {operation} для {left.Rows}x{left.Columns}.");
        return service.Calculate(left, operation, right);
    }
}

// ЛР 7--11. Поведенческие паттерны.
public interface ICalculationCommand { Matrix Execute(); }
public sealed class CalculationCommand(ICalculationService service, Matrix left, string operation, Matrix? right) : ICalculationCommand
{ public Matrix Execute() => service.Calculate(left, operation, right); }
public sealed class CalculationInvoker { public Matrix Run(ICalculationCommand command) => command.Execute(); }
public sealed class MatrixRows(Matrix matrix) : IEnumerable<double[]>
{
    public IEnumerator<double[]> GetEnumerator()
    {
        for (var row = 0; row < matrix.Rows; row++)
            yield return Enumerable.Range(0, matrix.Columns).Select(column => matrix[row, column]).ToArray();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
public abstract class MatrixValidationHandler
{
    private MatrixValidationHandler? _next;
    public MatrixValidationHandler SetNext(MatrixValidationHandler next) { _next = next; return next; }
    public void Validate(Matrix left, Matrix? right) { Check(left, right); _next?.Validate(left, right); }
    protected abstract void Check(Matrix left, Matrix? right);
}
public sealed class NonEmptyValidation : MatrixValidationHandler { protected override void Check(Matrix left, Matrix? right) { if (left.Rows == 0) throw new ArgumentException("Матрица пуста."); } }
public sealed class SameSizeValidation : MatrixValidationHandler { protected override void Check(Matrix left, Matrix? right) { if (right is not null && (left.Rows != right.Rows || left.Columns != right.Columns)) throw new ArgumentException("Размеры матриц различаются."); } }
public interface ICalculatorMediator { void PublishResult(Matrix result); }
public sealed class CalculatorMediator : ICalculatorMediator
{
    public Matrix? LastResult { get; private set; }
    public void PublishResult(Matrix result) => LastResult = result;
}
public sealed record CalculatorMemento(string Input);
public sealed class InputHistory
{
    private readonly Stack<CalculatorMemento> _history = new();
    public void Save(string input) => _history.Push(new CalculatorMemento(input));
    public string Restore() => _history.TryPeek(out var snapshot) ? snapshot.Input : string.Empty;
}
public sealed class CalculationSubject
{
    public event Action<Matrix>? Calculated;
    public void Notify(Matrix result) => Calculated?.Invoke(result);
}
public interface ICalculationState { string Name { get; } }
public sealed class ReadyState : ICalculationState { public string Name => "Готов к расчёту"; }
public sealed class ErrorState : ICalculationState { public string Name => "Ошибка ввода"; }
public sealed class CalculationContext { public ICalculationState State { get; private set; } = new ReadyState(); public void SetState(ICalculationState state) => State = state; }
public interface IDeterminantStrategy { double Calculate(Matrix matrix); }
public sealed class GaussianDeterminantStrategy : IDeterminantStrategy { public double Calculate(Matrix matrix) => matrix.Determinant(); }
public abstract class MatrixAlgorithm
{
    public Matrix Run(Matrix matrix) { Validate(matrix); return Transform(matrix); }
    protected virtual void Validate(Matrix matrix) => ArgumentNullException.ThrowIfNull(matrix);
    protected abstract Matrix Transform(Matrix matrix);
}
public sealed class TransposeAlgorithm : MatrixAlgorithm { protected override Matrix Transform(Matrix matrix) => matrix.Transpose(); }

// ЛР 12--15. Архитектурный, системный и идиоматический паттерны.
public sealed class CalculatorController(MatrixCalculatorFacade facade) // MVC controller
{ public Matrix Calculate(string left, string operation, string? right) => facade.Calculate(left, operation, right); }
public interface IMatrixRepository { void Save(string name, Matrix matrix); Matrix Get(string name); }
public sealed class InMemoryMatrixRepository : IMatrixRepository
{
    private readonly Dictionary<string, Matrix> _matrices = new();
    public void Save(string name, Matrix matrix) => _matrices[name] = matrix;
    public Matrix Get(string name) => _matrices.TryGetValue(name, out var matrix) ? matrix : throw new KeyNotFoundException(name);
}
public sealed class CalculationUnitOfWork(IMatrixRepository repository)
{
    private readonly List<(string Name, Matrix Matrix)> _pending = [];
    public void Add(string name, Matrix matrix) => _pending.Add((name, matrix));
    public void Commit() { foreach (var item in _pending) repository.Save(item.Name, item.Matrix); _pending.Clear(); }
}
public sealed class CalculationQueue
{
    private readonly BlockingCollection<ICalculationCommand> _queue = new();
    public void Produce(ICalculationCommand command) => _queue.Add(command);
    public IEnumerable<Matrix> Consume() => _queue.GetConsumingEnumerable().Select(command => command.Execute());
    public void Complete() => _queue.CompleteAdding();
}
