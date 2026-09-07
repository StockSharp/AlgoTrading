# Лимитный вход на откате Фибоначчи и SAR
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Эта схема сочетает два Parabolic SAR с разной скоростью и диапазон трёх свечей, размещает не более одной лимитной заявки на откате Фибоначчи, отменяет активную заявку при развороте условий и закрывает исполненную позицию по уровням диапазона, сохранённым при входе.

![schema](schema.svg)

## Обзор стратегии

- Завершённые часовые свечи BTCUSDT поступают в быстрый и медленный Parabolic SAR, а также в Highest(3) и Lowest(3). Решения начинаются только после формирования всех индикаторов.
- Сформированный выход Lowest выпускает один пакет решения после фиксации текущих Close, SAR, максимума, минимума, позиции и состояния ожидающей заявки.
- Общая блокировка ожидающей заявки разрешает только один активный вход. Она снимается при переходе заявки в конечное состояние, а выбранное значение позиции запрещает новый вход после исполнения.
- Условия входа, отмены и выхода рассчитываются в скрытых хранилищах оценок и выпускаются один раз на завершённую свечу, поэтому значения соседних свечей не смешиваются.
- На графике показаны свечи, обе линии SAR, границы диапазона и сохранённые уровни защиты, зарегистрированные и отменённые лимиты, рыночные выходы и все исполнения.

## Правила входа и выхода

- **Длинный вход**: когда `Slow SAR < Fast SAR < Close`, позиция нулевая и нет ожидающего входа, отправляется лимитная заявка Buy по цене `Low3 + (High3 - Low3) * 50%`. До исполнения она отменяется, если `Slow SAR > Fast SAR` или `Fast SAR >= Close`.
- **Короткий вход**: когда `Slow SAR > Fast SAR > Close`, позиция нулевая и нет ожидающего входа, отправляется лимитная заявка Sell по цене `High3 - (High3 - Low3) * 50%`. До исполнения она отменяется, если `Slow SAR < Fast SAR` или `Fast SAR <= Close`.
- **Выход**: при принятии сигнала входа сохраняются уровни соответствующей стороны. Для длинной позиции стоп равен `Low3 - 30`, а цель — `Low3 + (High3 - Low3) * 161%`; для короткой стоп равен `High3 + 30`, а цель — `High3 - (High3 - Low3) * 161%`. Достижение любого сохранённого уровня завершённой ценой Close запускает одну встречную рыночную заявку объёмом 1.

## Параметры

| Параметр | Значение по умолчанию | Описание |
|---|---|---|
| Security | BTCUSDT@BNBFT | Инструмент подписки на завершённые свечи. Для заявок и исполнений задайте Strategy Security тот же инструмент. |
| Candle Series | 01:00:00 | Завершённые часовые свечи для индикаторов, решений, выходов и графика. |
| Fast SAR Acceleration | 0.02 | Начальное ускорение быстрого Parabolic SAR. |
| Fast SAR Increment | 0.02 | Приращение ускорения быстрого Parabolic SAR. |
| Fast SAR Maximum | 0.20 | Максимальное ускорение быстрого Parabolic SAR. |
| Slow SAR Acceleration | 0.01 | Начальное ускорение медленного Parabolic SAR. |
| Slow SAR Increment | 0.02 | Приращение ускорения медленного Parabolic SAR. |
| Slow SAR Maximum | 0.10 | Максимальное ускорение медленного Parabolic SAR. |
| High Lookback | 3 | Число завершённых свечей в Highest для расчёта `High3`. |
| Low Lookback | 3 | Число завершённых свечей в Lowest для расчёта `Low3`. |
| Entry Fibonacci, % | 50 | Положение лимитной цены внутри текущего диапазона трёх свечей. |
| Target Fibonacci, % | 161 | Множитель диапазона для каждой сохранённой цели прибыли. |
| Stop Offset | 30 | Абсолютное расстояние цены за минимумом или максимумом трёх свечей для сохранённого стопа. |
| Order Volume | 1 | Объём каждого входа и каждого рыночного выхода с проверкой стороны. |

## Детали схемы

- [Variable](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) Security настраивает завершённые [Candles](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html); транзакционные блоки используют Strategy Security и Strategy Portfolio.
- Четыре сформированных блока [Indicator](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) рассчитывают оба значения Parabolic SAR и отдельные максимум и минимум трёх свечей. Выход Lowest служит общим тактом пакета.
- Блоки [Formula](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/formula.html), Variable и [Comparison](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) синхронизируют числовые входы, учитывают нулевую позицию и сторону ожидающей заявки и выпускают только истинные импульсы действий.
- Каждый принятый сигнал сохраняет рассчитанные стоп и цель до запуска блока [Order registering](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/orders/register.html). Сохранённые значения не перемещаются, пока позиция открыта.
- Ссылка на зарегистрированную заявку хранится для адресной [Order cancellation](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html). Блокировка снимается только событием Finished блока регистрации после исполнения, подтверждённой отмены или ошибки регистрации.
- Блоки [Modify position](https://doc.stocksharp.com/ru/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) с проверкой стороны отправляют фиксированную встречную рыночную заявку объёмом в одну единицу, когда завершённая цена Close достигает сохранённого стопа или цели. График получает все значимые потоки цен, заявок, отмен и MyTrade.

## Использование

Импортируйте файл `.json` в Designer, задайте Strategy Security равным BTCUSDT@BNBFT и запустите схему на часовой истории. Перед реальной торговлей проверьте масштаб цены инструмента, уровни Фибоначчи, отступ стопа, жизненный цикл заявок и работу рыночного выхода.
