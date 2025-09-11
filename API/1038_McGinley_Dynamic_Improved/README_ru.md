# Улучшенный McGinley Dynamic
[English](README.md) | [中文](README_cn.md)

Стратегия реализует индикатор "McGinley Dynamic (Improved)" Джона Р. МакГинли младшего и открывает позиции при пересечении цены и линии индикатора. Поддерживаются формулы Modern, Original и пользовательское значение k. Для сравнения можно вывести неконстролируемый вариант.

## Детали

- **Вход Long**: цена закрытия выше McGinley Dynamic.
- **Вход Short**: цена закрытия ниже McGinley Dynamic.
- **Индикаторы**: McGinley Dynamic, опционально Unconstrained McGinley Dynamic, EMA для сравнения.
- **Значения по умолчанию**: Period = 14, Formula = Modern, Custom k = 0.5, Exponent = 4.
- **Направление**: обе стороны.
