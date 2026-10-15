# Диаграмма стратегии Early Bird Range Latch
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Эта диаграмма торгует строгий пробой экстремума предыдущей пятиминутной свечи, когда цена согласуется с EMA 20. Суточная защёлка разрешает не более одной новой позиции за день UTC, а текущий ATR 14 задаёт границы стопа и цели.

![schema](schema.svg)

## Обзор стратегии

- Завершённые пятиминутные свечи дают текущее закрытие, максимум и минимум предыдущей свечи, EMA 20 и ATR 14. Кубики «Предыдущее значение» сдвигают только потоки High и Low, поэтому решение не сравнивает свечу с её собственными экстремумами.
- Для лонга требуются `Close > previous High` и `Close > EMA 20`; для шорта — `Close < previous Low` и `Close < EMA 20`. Все сравнения строгие, поэтому равенство не считается сигналом.
- Кубик «Текущее время» ведёт фиксированный интервал суточного сброса с 00:00:00 до 00:04:59 UTC. Время свечи ведёт фиксированный интервал входа с 00:05:00 до 23:59:59, а общий «Флаг» пропускает только первую подходящую направленную комбинацию после каждого сброса.
- Принятый сигнал сохраняет текущее закрытие как цену входа и открывает одну рыночную единицу только при нулевом снимке позиции. После выхода защёлка остаётся занятой и не допускает новый вход до следующего суточного сброса UTC.
- На каждой следующей завершённой свече формулы заново рассчитывают четыре границы по сохранённому входу и текущему ATR: стоп и цель лонга равны `entry − 1.5×ATR` и `entry + 2.5×ATR`, для шорта знаки меняются местами. Рыночные действия ReduceOnly закрывают соответствующую сторону при достижении любой границы.

## Правила входа и выхода

- **Вход в лонг**: После формирования EMA 20, с 00:05:00 до 23:59:59 UTC, нулевая позиция, `Close > previous High`, `Close > EMA 20` и свободный суточный «Флаг» отправляют рыночную покупку OpenPosition объёмом одна единица.
- **Вход в шорт**: После формирования EMA 20, с 00:05:00 до 23:59:59 UTC, нулевая позиция, `Close < previous Low`, `Close < EMA 20` и свободный суточный «Флаг» отправляют рыночную продажу OpenPosition объёмом одна единица.
- **Выход**: Для лонга рыночная продажа ReduceOnly срабатывает при `Close ≤ entry − 1.5×current ATR` или `Close ≥ entry + 2.5×current ATR`. Для шорта рыночная покупка ReduceOnly срабатывает при `Close ≥ entry + 1.5×current ATR` или `Close ≤ entry − 2.5×current ATR`. Выхода по времени, трейлинга, разворота и повторного входа в тот же день нет.

## Параметры

| Параметр | По умолчанию | Описание |
|---|---|---|
| Candles | 00:05:00 | Таймфрейм завершённых свечей, которые запускают все расчёты сигналов и риска. |
| EMA Length | 20 | Период сформированной экспоненциальной скользящей средней для фильтра направления. |
| ATR Length | 14 | Период сформированного Average True Range, пересчитываемого для каждой завершённой свечи. |
| Stop ATR Multiplier | 1.5 | Множитель текущего ATR для неблагоприятной границы относительно сохранённой цены входа. |
| Target ATR Multiplier | 2.5 | Множитель текущего ATR для благоприятной границы относительно сохранённой цены входа. |
| Order Volume | 1 | Фиксированный объём для обоих входов OpenPosition и обоих выходов ReduceOnly. |

## Детали диаграммы

- Кубик [Свечи](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) выдаёт завершённые пятиминутные свечи и может строить их из минутной истории набора данных.
- Три [Конвертера](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html) извлекают Close, High и Low. Два кубика [Предыдущее значение](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) применяют Shift 1 к числовым потокам High и Low.
- Сформированные [Индикаторы](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) рассчитывают EMA 20 для направления и ATR 14 для расстояния риска. Готовность EMA также не допускает входы до накопления достаточных данных обоими индикаторами.
- Поток [Текущее время](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/time.html) подаётся в кубик сброса [Рабочее время](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html). Отдельный кубик «Рабочее время» читает время свечи и непосредственно участвует в обоих условиях входа.
- Общий [Флаг](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) поглощает первый кандидат в лонг или шорт за сутки UTC. Переменные снимают позицию и закрытие принятого входа; вторая переменная цены входа повторно выдаёт сохранённое значение на каждой свече для формул риска.
- Кубики [Формула](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html), [Сравнение](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) и [Логическое условие](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) строят фильтры пробоя и четыре границы ATR. Посвечные флаги выхода исключают повторное закрытие при обновлении нескольких входов за одно вычисление.
- Два кубика OpenPosition и два ReduceOnly [Изменение позиции](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) выполняют рыночные входы и выходы. [Панель графика](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) получает свечи, предыдущие High и Low, EMA, ATR и объединённый поток всех исполнений.

## Использование

Импортируйте файл `.json` в Designer, прогоните схему на исторических данных в тестере, затем подстройте параметры или сами кубики под свой инструмент, прежде чем торговать вживую.
