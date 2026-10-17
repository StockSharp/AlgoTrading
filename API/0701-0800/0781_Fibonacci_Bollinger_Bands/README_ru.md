# Стратегия Fibonacci & Bollinger Bands
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Возврат к среднему только в лонг с полосами Боллинджера и расширением Фибоначчи ниже предшествующего диапазона.

Параметры: `BollingerPeriod = 20`, `BollingerDeviation = 2`, `FibLookback = 50`, `FibonacciLevel = 0.618`, `CandleType = 1 минута`. Используются только завершённые свечи. Полосы рассчитываются по последним 20 ценам закрытия, включая сигнальную свечу, с генеральным стандартным отклонением. Диапазон — максимум High и минимум Low предыдущих 50 завершённых свечей, без сигнальной свечи.

Расширение вниз: `F = previousLow − FibonacciLevel × (previousHigh − previousLow)`. Поддерживаются положительные коэффициенты больше 1, в том числе 1.618. Из нулевой позиции покупка строго при `Close < lowerBand` и `Close > F`. Весь лонг закрывается при `Close >= upperBand` или `Close >= previousHigh`. При равенстве с F входа нет. Шортов, стоп-заявок и использования будущих свечей нет.
