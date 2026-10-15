# Диаграмма стратегии RSI Alert
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Эта схема превращает экстремальные значения RSI в сделки и понятные уведомления. Она обрабатывает завершённые пятиминутные свечи, покупает при значении 30 или ниже и продаёт при значении 70 или выше только при отсутствии позиции, а к каждому исполненному входу применяет процентную защиту. Каждый принятый сигнал также фиксирует числовое значение RSI, форматирует его и записывает уведомление.

![schema](schema.svg)

## Обзор стратегии

- Один поток только завершённых пятиминутных свечей управляет индикатором, снимком позиции, решениями о входе, проверками защитной цены и графиком.
- RelativeStrengthIndex использует период 14. Фильтр только сформированных значений отключён (`IsFormed = false`), поэтому значения периода прогрева не отбрасываются только из-за того, что индикатор ещё не сформирован.
- Кубик Formula с выражением `a` преобразует IndicatorValue индикатора RSI в числовое значение, используемое сравнениями и сообщениями.
- Числовое значение RSI сравнивается с уровнями перепроданности и перекупленности. Каждый сигнал направления объединяется со снимком позиции, сделанным при обработке текущей свечи, а оба кубика входа используют условие Open position.
- Исполненные входы активируют защиту позиции с тейк-профитом 2% и стоп-лоссом 1%. Принятые сигналы входа также передают зафиксированное значение RSI через форматтер в уведомление типа Log.

## Правила входа и выхода

- **Вход в лонг**: Числовое значение RSI находится на уровне Oversold Level или ниже, а снимок позиции показывает её отсутствие. Схема покупает заданный объём по рынку и записывает уведомление о покупке со значением сигнала.
- **Вход в шорт**: Числовое значение RSI находится на уровне Overbought Level или выше, а снимок позиции показывает её отсутствие. Схема продаёт заданный объём по рынку и записывает уведомление о продаже со значением сигнала.
- **Выход**: Защита позиции закрывает сделку, когда цена закрытия завершённой свечи достигает уровня тейк-профита 2% или стоп-лосса 1% относительно цены входа. Встречный сигнал RSI не разворачивает открытую позицию, а исполнение защитной заявки не может вызвать новый вход на той же свече.

## Параметры

| Параметр | По умолчанию | Описание |
|---|---|---|
| RSI Period | 14 | Число свечей, используемых для расчёта RelativeStrengthIndex. |
| Oversold Level | 30 | Значения RSI на этом уровне или ниже разрешают вход в лонг, когда схема находится без позиции. |
| Overbought Level | 70 | Значения RSI на этом уровне или выше разрешают вход в шорт, когда схема находится без позиции. |
| Take Profit | 2% | Расстояние защитного тейк-профита от цены входа. |
| Stop Loss | 1% | Расстояние защитного стоп-лосса от цены входа. |
| Volume | 0.01 | Объём заявки входа в лотах. |
| Candles | 00:05:00 | Пятиминутный таймфрейм свечей; обрабатываются только завершённые свечи. |

## Детали диаграммы

- Выход кубика [Candles](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) сначала запускает создание снимка текущей позиции, затем обновляет кубик [Indicator](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) и в завершение обновляет [Converter](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) цены закрытия.
- Выход RSI поступает в кубик [Formula](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/formula.html) с выражением `a`. Его числовой выход поступает в обе защёлки значений для сообщений до того, как вычисляется любое из сравнений с порогами.
- Два кубика [Comparison](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) проверяют числовое значение RSI относительно общих значений Oversold Level и Overbought Level с помощью `<=` и `>=`.
- Значение [Position](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/positions/current.html) удерживается кубиком [Variable](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) на время обработки текущей свечи и сравнивается с нулём. Два кубика [Logical condition](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) объединяют результат проверки отсутствия позиции с сигналами RSI для лонга и шорта.
- Оба кубика входа [Modify position](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) используют рыночные заявки с условием Open position и получают значение `0.01` из одного общего значения объёма.
- Выходы MyTrade обоих кубиков входа поступают в кубик [Position protection](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/positions/protect.html). Конвертер цены закрытия подаёт значение на его вход Price, а кубик использует тейк-профит 2% и стоп-лосс 1%.
- Каждый объединённый сигнал входа запускает собственную защёлку значения RSI. Кубик [String format](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) формирует строку `RSI {0:0.0} <= 30 — buy` или `RSI {0:0.0} >= 70 — sell`, а кубики [Notification](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) типа Log публикуют сообщения.
- Chart panel получает завершённые свечи, значения RSI, оба потока сделок входа и сделки защитного выхода.

## Использование

Импортируйте файл `.json` в Designer и запустите его в бэктестере на исторических данных. Следите в журнале за отформатированными уведомлениями RSI и проверяйте защитные выходы по ценам закрытия свечей. Если вы измените один из порогов RSI, обновите также соответствующий шаблон форматтера, чтобы текст уведомления оставался точным.
