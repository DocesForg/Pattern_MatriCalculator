using MatrixCalculator;
using System.Diagnostics;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("Матричный калькулятор. Формат: строки через ';', элементы через пробел (например: 1 2; 3 4).");

try
{
    Console.Write("Первая матрица: ");
    var first = Matrix.Parse(Console.ReadLine() ?? string.Empty);
    Console.Write("Операция (+, -, *, T, det, inv): ");
    var operation = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
    Matrix? result = operation switch
    {
        "t" => first.Transpose(),
        "inv" => first.Inverse(),
        "+" or "-" or "*" => CalculateBinary(first, operation),
        "det" => null,
        _ => throw new ArgumentException("Неизвестная операция.")
    };
    Console.WriteLine(operation == "det" ? $"Результат: {first.Determinant():0.######}" : $"Результат:{Environment.NewLine}{result}");
}
catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException)
{
    Console.Error.WriteLine($"Ошибка ввода: {exception.Message}");
}

static Matrix CalculateBinary(Matrix first, string operation)
{
    Console.Write("Вторая матрица: ");
    var second = Matrix.Parse(Console.ReadLine() ?? string.Empty);
    return operation switch { "+" => first.Add(second), "-" => first.Subtract(second), "*" => first.Multiply(second), _ => throw new UnreachableException() };
}
