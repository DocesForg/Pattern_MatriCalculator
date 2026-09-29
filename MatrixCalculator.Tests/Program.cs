using MatrixCalculator;

var tests = new (string Name, string Data, string Expected, Action Body)[]
{
    ("Сложение", "A = 1 2; 3 4, B = 5 6; 7 8", "6 8; 10 12", () => Equal(Lines("6 8", "10 12"), Matrix.Parse("1 2; 3 4").Add(Matrix.Parse("5 6; 7 8")).ToString())),
    ("Умножение", "A = 1 2; 3 4, B = 5 6; 7 8", "19 22; 43 50", () => Equal(Lines("19 22", "43 50"), Matrix.Parse("1 2; 3 4").Multiply(Matrix.Parse("5 6; 7 8")).ToString())),
    ("Транспонирование", "A = 1 2 3; 4 5 6", "1 4; 2 5; 3 6", () => Equal(Lines("1 4", "2 5", "3 6"), Matrix.Parse("1 2 3; 4 5 6").Transpose().ToString())),
    ("Определитель", "A = 1 2; 3 4", "-2", () => Equal(-2, Matrix.Parse("1 2; 3 4").Determinant())),
    ("Определитель 3x3", "A = 6 1 1; 4 -2 5; 2 8 7", "-306", () => Equal(-306, Matrix.Parse("6 1 1; 4 -2 5; 2 8 7").Determinant())),
    ("Обратная матрица", "A = 4 7; 2 6", "0.6 -0.7; -0.2 0.4", () => Equal(Lines("0.6 -0.7", "-0.2 0.4"), Matrix.Parse("4 7; 2 6").Inverse().ToString())),
    ("Несовместимые размеры", "A = 1 2, B = 1; 2", "ArgumentException", () => Throws<ArgumentException>(() => Matrix.Parse("1 2").Add(Matrix.Parse("1; 2")))),
    ("Вырожденная матрица", "A = 1 2; 2 4", "InvalidOperationException", () => Throws<InvalidOperationException>(() => Matrix.Parse("1 2; 2 4").Inverse()))
};
foreach (var test in tests)
{
    Console.WriteLine($"ТЕСТ: {test.Name}. Данные: {test.Data}. Ожидаемый результат: {test.Expected}");
    test.Body();
    Console.WriteLine($"PASS: {test.Name}");
}
Console.WriteLine($"Всего успешно: {tests.Length}");

static void Equal<T>(T expected, T actual) where T : notnull { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Ожидалось '{expected}', получено '{actual}'."); }
static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception($"Ожидалось исключение {typeof(T).Name}."); }
static string Lines(params string[] lines) => string.Join(Environment.NewLine, lines);
