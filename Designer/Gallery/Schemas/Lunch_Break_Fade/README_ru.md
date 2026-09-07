# Схема стратегии Lunch Break Fade
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Эта схема торгует откатом краткосрочного движения в обеденном окне 11:00:00–14:59:59 по завершённым пятиминутным свечам. Вход разрешён только из нулевой позиции, выход определяется сравнением закрытия со сформированной SMA за 20 периодов, а после сигнала заявки все пути входа и выхода блокируются на следующие 30 завершённых свечей.

![schema](schema.svg)

## Обзор стратегии

- В цепочку решений поступают только завершённые пятиминутные свечи. SMA начинает выдавать значения после прогрева за 20 периодов, а два кубика Previous value предоставляют два непосредственно предшествующих закрытия.
- Кубик Working time считывает время открытия каждой свечи и разрешает входы с 11:00:00 по 14:59:59 включительно. Временное окно не ограничивает выходы.
- Рост двух предыдущих закрытий вместе с текущей медвежьей свечой даёт вход в шорт из нулевой позиции. Падение двух предыдущих закрытий вместе с бычьей свечой даёт вход в лонг из нулевой позиции.
- Лонг закрывается, когда цена закрытия ниже SMA, а шорт — когда цена закрытия выше SMA. Четыре отдельные ветви отправляют рыночные заявки фиксированного объёма для двух входов и двух выходов.
- Каждый сигнал входа или выхода включает паузу, которая блокирует оба типа действий на следующие 30 завершённых свечей. Кубика защиты позиции нет; график показывает свечи, SMA и исполнения всех четырёх ветвей заявок.

## Правила входа и выхода

- **Вход в лонг**: В обеденном окне при `Close[-1] < Close[-2]`, бычьей текущей свече (`Close > Open`), нулевом снимке позиции и завершённой паузе схема отправляет рыночную заявку на покупку объёмом 1.
- **Вход в шорт**: В обеденном окне при `Close[-1] > Close[-2]`, медвежьей текущей свече (`Close < Open`), нулевом снимке позиции и завершённой паузе схема отправляет рыночную заявку на продажу объёмом 1.
- **Выход**: Когда пауза завершена, для лонга отправляется рыночная продажа при `Close < SMA`, а для шорта — рыночная покупка при `Close > SMA`. Эти проверки уровня работают как внутри, так и вне обеденного окна. Стоп-лосс, тейк-профит и другая защита не подключены.

## Параметры

| Параметр | По умолчанию | Описание |
|---|---|---|
| Candles | 00:05:00 | Пятиминутный таймфрейм; индикатор, история, пауза и решения обрабатывают только завершённые свечи. |
| SMA Period | 20 | Период SimpleMovingAverage, используемой в обеих проверках уровня для выхода. |
| Cooldown Bars | 30 | Число следующих завершённых свечей, в течение которых заблокированы сигналы входа и выхода. |
| Lunch Begin | 11:00:00 | Включительная граница времени открытия свечи, с которой разрешены обеденные входы. |
| Lunch End | 14:59:59 | Включительная граница времени открытия свечи, до которой разрешены обеденные входы. |
| Volume | 1 | Фиксированный объём для всех четырёх кубиков рыночных заявок с NoCondition. |

## Детали диаграммы

- Кубик [Свечи](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) выдаёт завершённые пятиминутные свечи. [Индикатор](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) только со сформированными значениями рассчитывает SimpleMovingAverage 20, а [Формула](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/formula.html) предоставляет её числовое значение. Проверка [Разрешена ли торговля](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html) выпускает сохранённую свечу в цепочку решений.
- Кубики [Конвертер](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) извлекают цены Close и Open. Два кубика [Предыдущее значение](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) используют сдвиги 1 и 2; проверка готовности истории не допускает решений, пока не доступны оба предыдущих закрытия.
- Кубик [Рабочее время](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) получает поток свечей напрямую и сравнивает метаданные времени открытия с включительными границами обеденного окна. Его результат участвует только в двух условиях входа.
- Текущая [Позиция](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/positions/current.html) сохраняется кубиком [Переменная](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) и выдаётся один раз на каждую свечу решения. Кубики [Сравнение](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) и [Логическое условие](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) объединяют сессию, предыдущее направление, направление свечи, позицию, готовность истории, уровень SMA и состояние паузы.
- Кубик [Задержка сигнала](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) отсчитывает 30 последующих завершённых свечей. Переменные состояния блокируют все четыре условия действий во время отсчёта и вновь разрешают их на следующей свече.
- Четыре кубика [Изменение позиции](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) выставляют рыночные заявки с `NoCondition` и общим Volume 1: вход покупкой, вход продажей, выход продажей из лонга и выход покупкой из шорта. Элемента защиты нет.
- [Панель графика](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/chart.html) получает завершённые свечи, сформированный поток SMA и выход MyTrade каждого из четырёх кубиков заявок.

## Использование

Импортируйте файл `.json` в Designer, прогоните схему на исторических данных в тестере, затем подстройте параметры или сами кубики под свой инструмент, прежде чем торговать вживую.
