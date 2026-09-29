namespace MatrixCalculator;

/// <summary>Неизменяемая числовая матрица и базовые линейно-алгебраические операции.</summary>
public sealed class Matrix
{
    private const double Epsilon = 1e-12;
    private readonly double[,] _values;

    public int Rows => _values.GetLength(0);
    public int Columns => _values.GetLength(1);
    public double this[int row, int column] => _values[row, column];

    public Matrix(double[,] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.GetLength(0) == 0 || values.GetLength(1) == 0)
            throw new ArgumentException("Матрица должна иметь хотя бы одну строку и один столбец.", nameof(values));
        _values = (double[,])values.Clone();
    }

    public static Matrix Parse(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var rows = text.Trim().Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(row => row.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(cell => double.Parse(cell, System.Globalization.CultureInfo.InvariantCulture)).ToArray())
            .ToArray();
        if (rows.Length == 0 || rows.Any(row => row.Length == 0) || rows.Any(row => row.Length != rows[0].Length))
            throw new FormatException("Все строки матрицы должны содержать одинаковое число элементов.");

        var values = new double[rows.Length, rows[0].Length];
        for (var i = 0; i < rows.Length; i++)
            for (var j = 0; j < rows[i].Length; j++) values[i, j] = rows[i][j];
        return new Matrix(values);
    }

    public Matrix Add(Matrix other) => Combine(other, (left, right) => left + right, "складывать");
    public Matrix Subtract(Matrix other) => Combine(other, (left, right) => left - right, "вычитать");

    public Matrix Multiply(Matrix other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Columns != other.Rows)
            throw new ArgumentException("Число столбцов первой матрицы должно совпадать с числом строк второй.");
        var result = new double[Rows, other.Columns];
        for (var i = 0; i < Rows; i++)
            for (var j = 0; j < other.Columns; j++)
                for (var k = 0; k < Columns; k++) result[i, j] += this[i, k] * other[k, j];
        return new Matrix(result);
    }

    public Matrix Transpose()
    {
        var result = new double[Columns, Rows];
        for (var i = 0; i < Rows; i++)
            for (var j = 0; j < Columns; j++) result[j, i] = this[i, j];
        return new Matrix(result);
    }

    public double Determinant()
    {
        EnsureSquare();
        var copy = (double[,])_values.Clone();
        var sign = 1d;
        for (var pivot = 0; pivot < Rows; pivot++)
        {
            var pivotRow = FindPivot(copy, pivot);
            if (Math.Abs(copy[pivotRow, pivot]) < Epsilon) return 0;
            if (pivotRow != pivot) { SwapRows(copy, pivot, pivotRow); sign = -sign; }
            for (var row = pivot + 1; row < Rows; row++)
            {
                var factor = copy[row, pivot] / copy[pivot, pivot];
                for (var column = pivot; column < Columns; column++) copy[row, column] -= factor * copy[pivot, column];
            }
        }
        var determinant = sign;
        for (var i = 0; i < Rows; i++) determinant *= copy[i, i];
        return determinant;
    }

    public Matrix Inverse()
    {
        EnsureSquare();
        var size = Rows;
        var augmented = new double[size, size * 2];
        for (var i = 0; i < size; i++)
            for (var j = 0; j < size; j++) { augmented[i, j] = this[i, j]; augmented[i, j + size] = i == j ? 1 : 0; }
        for (var pivot = 0; pivot < size; pivot++)
        {
            var pivotRow = FindPivot(augmented, pivot);
            if (Math.Abs(augmented[pivotRow, pivot]) < Epsilon)
                throw new InvalidOperationException("Обратная матрица не существует: определитель равен нулю.");
            if (pivotRow != pivot) SwapRows(augmented, pivot, pivotRow);
            var divisor = augmented[pivot, pivot];
            for (var column = 0; column < size * 2; column++) augmented[pivot, column] /= divisor;
            for (var row = 0; row < size; row++)
            {
                if (row == pivot) continue;
                var factor = augmented[row, pivot];
                for (var column = 0; column < size * 2; column++) augmented[row, column] -= factor * augmented[pivot, column];
            }
        }
        var result = new double[size, size];
        for (var i = 0; i < size; i++) for (var j = 0; j < size; j++) result[i, j] = augmented[i, j + size];
        return new Matrix(result);
    }

    public override string ToString() => string.Join(Environment.NewLine, Enumerable.Range(0, Rows)
        .Select(i => string.Join(" ", Enumerable.Range(0, Columns).Select(j => _values[i, j].ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)))));

    private Matrix Combine(Matrix other, Func<double, double, double> operation, string verb)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Rows != other.Rows || Columns != other.Columns) throw new ArgumentException($"Матрицы разных размеров нельзя {verb}.");
        var result = new double[Rows, Columns];
        for (var i = 0; i < Rows; i++) for (var j = 0; j < Columns; j++) result[i, j] = operation(this[i, j], other[i, j]);
        return new Matrix(result);
    }
    private void EnsureSquare() { if (Rows != Columns) throw new InvalidOperationException("Операция доступна только для квадратной матрицы."); }
    private static int FindPivot(double[,] values, int column) => Enumerable.Range(column, values.GetLength(0) - column).MaxBy(row => Math.Abs(values[row, column]));
    private static void SwapRows(double[,] values, int first, int second) { for (var j = 0; j < values.GetLength(1); j++) (values[first, j], values[second, j]) = (values[second, j], values[first, j]); }
}
