# Описание стратегии Bollinger Bands
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Обзор стратегии

Стратегия «Bollinger Bands» разработана для [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) и направлена на использование полос Bollinger Bands для извлечения выгоды из паттернов волатильности. Стратегия определяет моменты пересечения ценой полос для нахождения точек входа и выхода на рынке.

![schema](schema.png)

## Детали стратегии

### Компоненты

1. **Формирование свечей**: Использует пятиминутный таймфрейм для генерации [свечей](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) и запускает анализ при закрытии каждой свечи.
2. **Индикатор Bollinger Bands**: Вычисляет верхнюю и нижнюю полосы [Bollinger Bands](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) с периодом 32 и множителем стандартного отклонения 2.0.
3. **Торговые сигналы**:
   - **Сигнал на покупку**: Генерируется, когда [минимальная цена](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) свечи [пересекает](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) нижнюю полосу Bollinger Bands снизу вверх, что свидетельствует о перепроданности.
   - **Сигнал на продажу**: Генерируется, когда [максимальная цена](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) свечи [пересекает](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) верхнюю полосу Bollinger Bands, указывая на перекупленность.

### Исполнение сделок

- **Тип ордера**: [Рыночные ордера](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) используются как для входа, так и для выхода с целью обеспечения быстрого исполнения.
- **Управление позицией**: Позиции открываются на основе сигналов пересечения и закрываются либо при пересечении в противоположном направлении, либо на основе заранее заданных условий стоп-лосса или тейк-профита.

### Управление рисками

- **Стоп-лосс и тейк-профит**: Настраиваемые параметры позволяют устанавливать фиксированные или процентные уровни [стоп-лосса и тейк-профита](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html) для эффективного управления рисками.
- **Управление капиталом**: Стратегия включает параметры для корректировки объёма сделок на основе доступного баланса счёта и уровней риска.

## Заключение

Стратегия «Bollinger Bands» обеспечивает системный подход к торговле, основанный на волатильности и рыночных условиях, что делает её подходящей для трейдеров, ищущих надёжную автоматизированную торговую систему в рамках платформы StockSharp. Она сочетает технические индикаторы с точными правилами исполнения сделок для повышения эффективности торговли в различных рыночных условиях.
