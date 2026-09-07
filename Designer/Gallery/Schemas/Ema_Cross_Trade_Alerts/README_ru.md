# Схема стратегии EMA Cross Trade Alerts
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Эта схема торгует пересечения быстрой EMA с периодом 120 и медленной EMA с периодом 450 вверх и вниз на завершённых минутных свечах. Снимок позиции фильтрует каждый сигнал, рыночные заявки фиксированного объёма управляют позицией, каждая собственная сделка записывается в журнал, а график показывает свечи, обе EMA и исполнения покупок и продаж.

![schema](schema.svg)

## Обзор стратегии

- Завершённые минутные свечи поступают в быструю EMA 120 и медленную EMA 450. Фильтрация только сформированных значений у обоих индикаторов отключена, поэтому их значения доступны с начала расчёта.
- Кубик Crossing выдаёт `true` при пересечении быстрой EMA над медленной. Кубик NOT превращает событие `false` при пересечении вниз в положительный триггер ветки продажи.
- При оценке каждой свечи текущее значение позиции выдаётся защёлкой, запускаемой свечой, до обработки сигналов EMA. Сравнения разрешают покупку только при `Position <= 0`, а продажу — только при `Position >= 0`.
- Оба кубика рыночных заявок работают с `NoCondition` и фиксированным Volume 1. Встречный сигнал может уменьшить или обнулить позицию и может пересечь ноль, если модуль позиции меньше Volume, но полный разворот не гарантируется.
- Кубик Strategy trades передаёт каждую собственную сделку через точный шаблон сообщения об исполнении в уведомление типа Log. График получает завершённые свечи, обе EMA и потоки исполнений покупок и продаж.

## Правила входа и выхода

- **Вход в лонг**: Когда быстрая EMA пересекает медленную снизу вверх, а снимок позиции на момент свечи меньше или равен нулю, схема отправляет рыночную заявку на покупку объёмом 1.
- **Вход в шорт**: Когда быстрая EMA пересекает медленную сверху вниз, а снимок позиции на момент свечи больше или равен нулю, схема отправляет рыночную заявку на продажу объёмом 1.
- **Выход**: Отдельных кубиков выхода и защиты нет. Более поздняя встречная заявка фиксированного объёма может уменьшить текущую позицию, закрыть равную ей позицию или пересечь ноль, если позиция меньше Volume; полный разворот не гарантируется.

## Параметры

| Параметр | По умолчанию | Описание |
|---|---|---|
| Candles | 00:01:00 | Минутный таймфрейм; цепочку EMA и принятия решения запускают только завершённые свечи. |
| Fast EMA Period | 120 | Период быстрой ExponentialMovingAverage; фильтрация только сформированных значений отключена. |
| Slow EMA Period | 450 | Период медленной ExponentialMovingAverage; фильтрация только сформированных значений отключена. |
| Volume | 1 | Фиксированный объём для обоих кубиков рыночных заявок с NoCondition. |

## Детали диаграммы

- Кубик [Свечи](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) выдаёт только завершённые минутные свечи, сначала запускает снимок позиции, а затем расчёт EMA.
- Два кубика [Индикатор](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) рассчитывают ExponentialMovingAverage с периодами 120 и 450. Их параметр выдачи только сформированных значений равен `false`, а оба потока также поступают на график.
- Выход [Пересечения](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) равен `true` при движении вверх и `false` при движении вниз. Оператор NOT кубика [Логическое условие](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) превращает событие вниз в положительный триггер продажи; отдельные AND объединяют направление и позицию.
- Текущая [Позиция](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/positions/current.html) непрерывно сохраняется и выдаётся один раз на свечу до ветки EMA. Кубики [Сравнение](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) проверяют `Position <= 0` и `Position >= 0` в той же причинной волне свечи, что и пересечение.
- Кубики покупки и продажи [Изменение позиции](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) выставляют рыночные заявки с `NoCondition` и общим фиксированным Volume. Стопа, тейк-профита и отдельного кубика выхода нет.
- [Сделки стратегии](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html) выдают каждую собственную `MyTrade`. [Форматирование строки](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) использует в точности `EMA cross fill: {Order.Side} {Trade.TradeVolume:0.########} {Order.Security.Id} @ {Trade.TradePrice:0.########}`.
- Кубик [Уведомление](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) записывает каждое отформатированное исполнение с Type `Log` и Caption `EMA cross trade`. График показывает свечи, быструю EMA, медленную EMA и отдельные потоки исполнений покупок и продаж.

## Использование

Импортируйте файл `.json` в Designer, прогоните схему на исторических данных в тестере, затем подстройте параметры или сами кубики под свой инструмент, прежде чем торговать вживую.
