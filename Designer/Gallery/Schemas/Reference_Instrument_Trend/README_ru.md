# Схема стратегии с подтверждением тренда опорного инструмента
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Эта схема сначала синхронизирует завершённые пятиминутные свечи BTCUSDT@BNBFT и TONUSDT@BNBFT, а затем торгует точными пересечениями EMA на BTC, когда тренд EMA на TON подтверждает ту же сторону. Условия позиции и паузы фильтруют каждое решение, а две ветви рыночных заявок фиксированного объёма управляют позицией.

![schema](schema.svg)

## Обзор стратегии

- Кубик Sync получает два потока завершённых пятиминутных свечей с Interval `00:05:00` и включённым ClearSockets. Он выдаёт согласованную пару BTC–TON только при наличии обеих свечей; неполный интервал отбрасывается, если одной из них нет.
- Каждая согласованная пара затем поступает в быструю EMA 7 и медленную EMA 18 для BTC, а также в быструю EMA 47 и медленную EMA 50 для TON. Фильтрация только сформированных значений отключена у всех четырёх индикаторов.
- Пересечение BTC вверх требует `PrevFast <= PrevSlow` и `Fast > Slow`, а пересечение вниз — `PrevFast >= PrevSlow` и `Fast < Slow`. Текущее соотношение TON подтверждает покупку при `Fast > Slow` и продажу при `Fast < Slow`.
- Ветвь покупки дополнительно требует `Position <= 0`, а ветвь продажи — `Position >= 0`. Обе ветви отправляют рыночные заявки `NoCondition` с фиксированным Volume 1.
- Первые пять согласованных пар BTC–TON заблокированы, и каждый сигнал заявки блокирует следующие пять согласованных пар; шестая пара снова допускается. Стоп-лосса, тейк-профита и отдельного кубика выхода нет, а график показывает свечи BTC, обе EMA BTC и оба потока исполнений.

## Правила входа и выхода

- **Вход в лонг**: Когда для BTC выполнены `PrevFast <= PrevSlow` и `Fast > Slow`, у TON сейчас `Fast > Slow`, синхронизированная проверка позиции даёт `Position <= 0` и пауза завершена, схема отправляет рыночную заявку на покупку с Volume 1.
- **Вход в шорт**: Когда для BTC выполнены `PrevFast >= PrevSlow` и `Fast < Slow`, у TON сейчас `Fast < Slow`, синхронизированная проверка позиции даёт `Position >= 0` и пауза завершена, схема отправляет рыночную заявку на продажу с Volume 1.
- **Выход**: Отдельного кубика выхода или защиты нет. Следующая допустимая заявка в противоположную сторону уменьшает позицию; при позиции `+1` или `-1` фиксированный объём 1 сводит её к нулю, а не открывает противоположную сторону.

## Параметры

| Параметр | По умолчанию | Описание |
|---|---|---|
| Main Fast EMA | 7 | Период быстрой ExponentialMovingAverage по завершённым пятиминутным свечам BTCUSDT@BNBFT; фильтрация только сформированных значений отключена. |
| Main Slow EMA | 18 | Период медленной ExponentialMovingAverage по завершённым пятиминутным свечам BTCUSDT@BNBFT; фильтрация только сформированных значений отключена. |
| Reference Fast EMA | 47 | Период быстрой ExponentialMovingAverage, рассчитываемой по согласованной завершённой пятиминутной свече TONUSDT@BNBFT; фильтрация только сформированных значений отключена. |
| Reference Slow EMA | 50 | Период медленной ExponentialMovingAverage, рассчитываемой по согласованной завершённой пятиминутной свече TONUSDT@BNBFT; фильтрация только сформированных значений отключена. |
| Cooldown Bars | 5 | Число начальных согласованных пар и пар после сигнала, которые блокируются до допуска следующей согласованной пары. |
| Volume | 1 | Фиксированный объём для обоих кубиков рыночных заявок с NoCondition. |

## Детали диаграммы

- Два кубика [Свечи](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) направляют завершённые пятиминутные свечи BTCUSDT@BNBFT и TONUSDT@BNBFT прямо на синхронизацию.
- Кубик [Sync](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/sync.html) согласует два свечных входа с Interval `00:05:00` и ClearSockets `true`. Он выдаёт обе свечи одной парой; если одна из сторон отсутствует, неполный интервал очищается и не попадает в цепочку индикаторов.
- Только синхронизированная пара поступает в четыре кубика [Индикатор](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/indicator.html): ExponentialMovingAverage 7 и 18 для BTC и 47 и 50 для TON. Параметр выдачи только сформированных значений у них равен `false`, а на график дополнительно поступают только две EMA BTC.
- Кубики [Предыдущее значение](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) хранят прошлые значения быстрой и медленной EMA BTC из согласованной пары. Кубики [Сравнение](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) задают обе стороны каждого точного пересечения и два текущих соотношения тренда TON; отдельные ветви [Логическое условие](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) объединяют их для покупки и продажи.
- Текущая [Позиция](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/positions/current.html) предоставляет проверки `Position <= 0` и `Position >= 0`. Шлюз [N values](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) получает выход согласованных свечей BTC от Sync и с N=5 подавляет первые пять согласованных пар и пять пар после каждого сигнала заявки, затем вновь допускает решения на шестой.
- Кубики покупки и продажи [Изменение позиции](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) выставляют рыночные заявки с `NoCondition` и общим Volume 1. Стоп-лосс, тейк-профит, защита позиции и отдельный элемент выхода отсутствуют.
- [Панель графика](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/chart.html) получает завершённые свечи BTC, BTC EMA 7, BTC EMA 18 и выходы MyTrade кубиков покупки и продажи.

## Использование

Импортируйте файл `.json` в Designer, прогоните схему на исторических данных в тестере, затем подстройте параметры или сами кубики под свой инструмент, прежде чем торговать вживую.
