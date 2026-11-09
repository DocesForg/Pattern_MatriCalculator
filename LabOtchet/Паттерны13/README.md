Лабораторная работа № 13. Паттерн микросервисов и распределённых систем: API Gateway

Цель: Реализовать единую точку входа к операциям матричного калькулятора.

Git репозиторий - [https://github.com/DocesForg/Pattern_MatriCalculator](https://github.com/DocesForg/Pattern_MatriCalculator)

Вариант 6:  Разработка приложения «Матричный калькулятор»

Реализация и принцип работы

В локальном приложении роль API Gateway выполняет MatrixCalculatorFacade. Это не сетевой шлюз, но тот же архитектурный принцип: внешний клиент знает один контракт, а фасад маршрутизирует запрос к операциям подсистемы, валидирует обязательный параметр и скрывает внутреннюю модель.

При выделении вычислений в отдельный сервис этот класс может быть адаптирован к HTTP endpoint: контракт Calculate(left, operation, right) останется на границе приложения, а маршрутизация не проникнет в Matrix.

Полный листинг

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

Вывод

В ходе выполнения лабораторной работы № 13 был реализован паттерн «API Gateway». Фасад выступил в роли единой точки входа к операциям матричного калькулятора, маршрутизируя запросы к соответствующим методам и скрывая внутреннюю структуру подсистемы. Это упростило клиентский код и обеспечило возможность будущей миграции к микросервисной архитектуре.

