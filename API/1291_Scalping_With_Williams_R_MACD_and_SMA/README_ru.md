# Скальпинг на Williams R, MACD и SMA
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Стратегия скальпинга, использующая индикаторы Williams %R, гистограмму MACD и простую скользящую среднюю на минутных свечах.

## Детали

- **Вход**: Williams %R пересекает уровни активации и гистограмма MACD меняет знак по направлению тренда.
- **Длинные/короткие**: Обе стороны.
- **Выход**: Разворот гистограммы.
- **Стопы**: Нет.
- **Параметры по умолчанию**:
  - `WilliamsLength` = 140
  - `MacdFast` = 24
  - `MacdSlow` = 52
  - `MacdSignal` = 9
  - `SmaLength` = 7
