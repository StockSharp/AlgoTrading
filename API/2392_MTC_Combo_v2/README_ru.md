# Стратегия MTC Combo v2
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Конвертация скрипта MetaTrader "MTC Combo v2 (barabashkakvn's edition)".

## Логика
- Используется наклон скользящей средней для определения тренда.
- Дополнительный фильтр perceptron суммирует изменения цен открытия за несколько лагов.
- Параметр `Pass` выбирает режим:
  - 4: лонг при `perceptron3 > 0` и `perceptron2 > 0`; шорт при `perceptron3 <= 0` и `perceptron1 < 0`.
  - 3: лонг при `perceptron2 > 0`.
  - 2: шорт при `perceptron1 < 0`.
  - другое: торговля только по наклону MA.

Стоп-лосс и тейк-профит берутся из `Sl*` и `Tp*`.

## Параметры
- `MaPeriod` – период MA.
- `P2`, `P3`, `P4` – лаги perceptron.
- `Pass` – режим принятия решений.
- `Sl1`/`Tp1`, `Sl2`/`Tp2`, `Sl3`/`Tp3`.
- `CandleType` – тип свечей.

## Примечание
Открывается только одна позиция. Закрытие по SL/TP.

## Дисклеймер
Стратегия предоставлена исключительно в образовательных целях.
