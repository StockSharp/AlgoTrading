# Схема стратегии Open Drive
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Схема работает по одной импульсной свече — такой, чьё тело больше доли текущего Average True Range. Цвет этого тела задаёт сторону, SMA 20 должна с ним согласиться, время должно попадать в первые шесть часов UTC-суток, а позиция должна быть нулевой. Выйти из сделки можно только по тейк-профиту или стоп-лоссу.

![schema](schema.svg)

## Обзор стратегии

- Завершённые пятиминутные свечи через два блока Converter отдают Close и Open и питают SMA 20 и ATR 14. Оба индикатора работают только по сформированным значениям, поэтому ни одно сравнение не выносит вердикт, пока каждый из них не наберёт достаточно свечей.
- Одна Formula измеряет тело текущей свечи как `abs(Close - Open)`, вторая превращает текущий ATR в порог `ATR x 0.3`. Comparison признаёт свечу импульсной, когда тело строго больше этого порога, — то есть схема действует по бару, необычно крупному для волатильности момента.
- Два блока Comparison читают цвет той же свечи — `Close > Open` и `Close < Open`, ещё два определяют её сторону относительно SMA 20 — `Close > SMA` и `Close < SMA`. Один только импульс сделок не открывает: цвет и тренд должны смотреть в одну сторону.
- Блок Current time передаёт время стратегии в блок Working time, охватывающий период с 00:00:00 до 06:00:00 UTC. Его ответ true/false хранится в блоке Variable, который переиздаёт значение с приходом свечи, поэтому фильтр сессии решается на том же такте, что и все ценовые сравнения, а не по собственным часам.
- Ещё один Variable снимает срез позиции на каждой свече, а Comparison с нулём говорит, находится ли схема вне рынка. Чтение позиции через срез не даёт сделке, исполнившейся между двумя свечами, заново запустить логику входа внутри бара.
- Logical condition для длинной стороны — это `impulse AND bullish body AND close above SMA AND inside the window AND flat`, для короткой — его зеркало. Каждый ждёт все пять входов, поэтому выдаёт ровно один вердикт на завершённую свечу.
- Вердикт true запускает блок Modify position в режиме OpenPosition: он отправляет рыночную заявку на заданный объём и отклоняет её, если позиция на самом деле не нулевая. Поэтому одна свеча никогда не откроет две сделки, а открытая позиция полностью блокирует новые входы.
- Исполнения входов с обеих сторон проходят через Combination в Position protection, который взводит тейк-профит 3% и стоп-лосс 2% и проверяет их по цене закрытия каждой следующей завершённой свечи.

## Правила входа и выхода

- **Вход в лонг**: В интервале 00:00:00-06:00:00 UTC, когда SMA 20 и ATR 14 сформированы, а позиция нулевая: `abs(Close - Open) > ATR x 0.3`, `Close > Open` и `Close > SMA 20` отправляют рыночную заявку на покупку одной единицы в режиме OpenPosition.
- **Вход в шорт**: В интервале 00:00:00-06:00:00 UTC, когда SMA 20 и ATR 14 сформированы, а позиция нулевая: `abs(Close - Open) > ATR x 0.3`, `Close < Open` и `Close < SMA 20` отправляют рыночную заявку на продажу одной единицы в режиме OpenPosition.
- **Выход**: Выхода по сигналу нет, переворота тоже. Position protection закрывает сделку при прибыли 3% или убытке 2% от цены исполнения входа, а уровни проверяются по закрытию каждой завершённой свечи, поэтому прокол уровня внутри бара отрабатывается только после того, как свеча завершится. Паузы между сделками схема не выдерживает: как только позиция закрыта, уже следующая подходящая свеча внутри окна может открыть новую.

## Параметры

| Параметр | По умолчанию | Описание |
|---|---|---|
| Candles | 00:05:00 | Таймфрейм завершённых свечей; все сравнения, оба индикатора и защитные уровни рассчитываются по их закрытиям. |
| MA Period | 20 | Период простой скользящей средней, работающей только по сформированным значениям, которая определяет, по какую сторону тренда закрылась свеча. |
| ATR Period | 14 | Период Average True Range, работающего только по сформированным значениям, который описывает обычный размер свечи в данный момент. |
| ATR Multiplier | 0.3 | Доля текущего ATR, которую тело свечи должно превысить, чтобы считаться импульсом. Увеличение требует более редких и крупных свечей, уменьшение пропускает и обычные. |
| Window Begin | 00:00:00 | Начало торгового окна по UTC. До него импульсы измеряются и рисуются, но сделок по ним нет. |
| Window End | 06:00:00 | Конец торгового окна по UTC. Расширьте пару до 00:00:00-23:59:59, чтобы схема торговала круглосуточно. |
| Order Volume | 1 | Объём, отправляемый обоими входами; позиция всегда равна одной единице, потому что второй вход отклоняется, пока она открыта. |
| Take Profit | 3% | Расстояние до тейк-профита в процентах от цены исполнения входа. |
| Stop Loss | 2% | Расстояние до стоп-лосса в процентах от цены исполнения входа. |

## Детали диаграммы

- Блок [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) выдаёт завершённые пятиминутные свечи, которые можно построить по входящей в комплект минутной истории. Два блока [Converters](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) читают Close и Open, а два блока [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html), работающих только по сформированным значениям, считают SMA 20 и ATR 14.
- Два блока [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) строят тело свечи и порог по ATR, а пять блоков [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) превращают тело, цвет, сторону тренда и позицию в сигналы.
- Блок [Current time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) подаёт время стратегии в [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html), ответ которого меняется намного чаще, чем приходит свеча. Блок [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) с выключенным Input as trigger хранит этот ответ и отдаёт его только тогда, когда его запустит следующая свеча.
- Значение блока [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) снимается вторым Variable и сравнивается с константой ноль. Оба блока [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) с пятью входами ждут каждый вход, поэтому каждая свеча даёт один вердикт для длинной стороны и один для короткой.
- Два блока [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) в режиме OpenPosition торгуют по рынку. [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) объединяет исполнения обоих входов для [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html), а [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) рисует свечи, SMA, ATR, все заявки, включая защитную пару, и все сделки.

## Использование

Импортируйте файл `.json` в Designer, прогоните схему на исторических данных в тестере, затем подстройте параметры или сами кубики под свой инструмент, прежде чем торговать вживую.
