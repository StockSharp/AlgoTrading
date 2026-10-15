# Схема стратегии Twenty Pips Once a Day
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Эта схема открывает не более одной контртрендовой позиции в день. Один раз в сутки, в заданный час и только при отсутствии открытой позиции, она сравнивает цену закрытия завершившейся часовой свечи с ценой закрытия свечи на 29 баров раньше и играет против смещения этого окна: покупает после падения и продаёт после роста. За выход отвечают небольшой тейк-профит, более широкий стоп и жёсткое ограничение на время жизни позиции.

![schema](schema.svg)

## Обзор стратегии

- Всё работает на завершённых часовых свечах. Внутри формирующегося бара ничего не оценивается, поэтому каждое решение принимается по закрытой цене.
- Previous value хранит свечу 29 баров назад. Её цена закрытия сравнивается с текущей, и это измеряет смещение примерно за последние сутки с четвертью.
- Сравнение задаёт направление против этого смещения: если прежняя цена закрытия выше текущей, рынок упал и схема покупает; если ниже — рынок вырос и схема продаёт. Используются два строгих сравнения, поэтому окно, которое заканчивается ровно там же, где началось, не даёт сигнала вовсе.
- Time выдаёт отсчёт времени, соответствующий только что закрывшейся свече, конвертер берёт из него час, а сравнение с параметром Trading Hour открывает окно входа на одну свечу в сутки.
- Текущая позиция должна быть нулевой. Вместе с фильтром по часу «раз в день» и условием Open position на блоках входа именно это удерживает схему в рамках одной позиции одновременно.
- Оба входа — рыночные заявки фиксированного объёма. Их исполнения объединяются и передаются в Position protection, который закрывает позицию по тейк-профиту 0.1% или стоп-лоссу 0.5% — то самое соотношение один к пяти, на котором построена идея.
- Счётчик N values запускается принятым входом и отсчитывает 21 завершённую свечу. Когда счёт исчерпан, блок Position modify в режиме Close position закрывает всё, что ещё открыто, чтобы позиция, не дошедшая ни до одной из целей, не удерживалась бесконечно.
- Is trade allowed следит за разрешением платформы на реальную торговлю. При каждом принятом входе схема фиксирует, каким это разрешение было в тот момент, и пишет одну строку в лог — это отчёт, а не запрет: в историческом воспроизведении разрешение никогда не выдаётся, поэтому вход, поставленный от него в зависимость, заглушил бы всю схему.

## Правила входа и выхода

- **Вход в лонг**: На завершённой часовой свече, час которой равен Trading Hour, при нулевой позиции и цене закрытия 29 баров назад выше текущей — купить Volume по рынку под условием Open position.
- **Вход в шорт**: На завершённой часовой свече, час которой равен Trading Hour, при нулевой позиции и цене закрытия 29 баров назад ниже текущей — продать Volume по рынку под условием Open position.
- **Выход**: Position protection закрывает позицию по тейк-профиту 0.1% или стоп-лоссу 0.5%, отсчитанным от цены исполнения входа, а проверки цены питает закрытие свечи. Если ни один из уровней не достигнут, счётчик N values срабатывает через 21 завершённую свечу после входа и блок Close position закрывает остаток; если защита уже закрыла позицию, этому действию закрывать нечего и оно ничего не делает.

## Параметры

| Параметр | По умолчанию | Описание |
|---|---|---|
| Candles | 01:00:00 | Таймфрейм рабочих свечей. Обрабатываются только завершённые свечи, поэтому заявка никогда не может быть датирована внутри ещё формирующегося бара. |
| Lookback Bars | 29 | На сколько баров назад берётся опорная цена закрытия. Это ширина окна, против смещения которого играет вход. |
| Trading Hour | 7 | Час, в который открывается суточное окно входа; читается из времени стратегии, сопровождающего завершённую свечу. |
| Volume | 0.1 | Фиксированный объём обеих входных заявок. Адаптивного расчёта объёма нет: каждый вход одного и того же размера. |
| Max Position Bars | 21 | Сколько завершённых свечей позиция может жить, прежде чем будет закрыта независимо от прибыли или убытка. |
| Take Profit % | 0.1 | Благоприятное движение, при котором Position protection закрывает позицию, в процентах от цены входа. |
| Stop Loss % | 0.5 | Неблагоприятное движение, при котором Position protection закрывает позицию, в процентах от цены входа. Стоп фиксированный, не скользящий. |

## Детали диаграммы

- Блок [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) настроен только на завершённые свечи, а [Previous value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) поставлен на саму свечу, а не на цену, и за ним идёт [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html). Два блока [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) превращают две цены закрытия в сторону покупки и сторону продажи; поскольку оба сравнения строгие, неизменившееся окно не даёт ни той, ни другой.
- [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) здесь источник данных, а не подпись: конвертер читает из него Hour, а сравнение сопоставляет его с [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html). И проверка часа, и проверка нулевой позиции привязаны к свече, потому что константы, с которыми они сравниваются, запускаются потоком свечей, — поэтому условие входа может выполниться только один раз на завершённый бар.
- [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) и сравнение с нулём дают проверку отсутствия позиции, а два блока [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) сводят смещение, час и позицию в один сигнал на каждую сторону. Оба блока [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) несут условие Open position — это вторая защита от повторного входа, пока позиция открыта.
- Принятый сигнал также запускает [N values](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html), который отсчитывает завершённые свечи и затем включает третий блок Position modify в режиме Close position. Объём в этом блоке не задан: закрываемое количество выводится из открытой позиции, а при нулевой позиции заявки просто не будет.
- [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) объединяет исполнения обеих сторон входа для [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html). Параллельно [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) выдаёт ровно один импульс на вход и сбрасывается счётчиком времени жизни; этот импульс защёлкивает показание [Is trade allowed](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html) в переменную, которую блок [String format](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) превращает в одну строку лога [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) на каждую взятую позицию.

## Использование

Импортируйте файл `.json` в Designer, прогоните схему на исторических данных в тестере, затем подстройте параметры или сами кубики под свой инструмент, прежде чем торговать вживую.
