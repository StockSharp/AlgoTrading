# Описание стратегии Parabolic SAR
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Обзор стратегии

Стратегия «Parabolic SAR» предназначена для захвата разворотов и продолжения трендов с использованием индикатора Parabolic Stop and Reverse (SAR) в [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html). Стратегия предоставляет чёткие сигналы входа и выхода на основе движения цены относительно точек Parabolic SAR.

![schema](schema.png)

## Детали стратегии

### Компоненты

- **Формирование свечей**: использует 5-минутный [таймфрейм](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) для анализа ценового движения, обеспечивая эффективный захват краткосрочных рыночных движений.
- **Индикатор Parabolic SAR**: [настроен](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) с начальным коэффициентом ускорения 0,02, шагом ускорения 0,02 и максимальным ускорением 0,2. Эти настройки позволяют индикатору адаптироваться к волатильности рынка.

### Исполнение сделок

- **Сигнал входа**: сигнал на покупку генерируется, когда цена пересекает точки Parabolic SAR [снизу вверх](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html), указывая на возможный восходящий тренд.
- **Сигнал выхода**: сигнал на продажу формируется, когда цена опускается [ниже](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) точек Parabolic SAR, сигнализируя о возможном нисходящем тренде.

### Визуализация

- **Отображение на графике**: точки Parabolic SAR отображаются на [графике](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) рядом со свечами цены, обеспечивая визуальное представление тренда и потенциальных торговых сигналов.

## Детали реализации

- **Платформа**: реализована на платформе StockSharp с использованием её широких возможностей получения данных в реальном времени, вычисления индикаторов и исполнения сделок.
- **Применение индикатора**: Parabolic SAR применяется непосредственно к ценовому графику, что позволяет незамедлительно визуально оценивать смену тренда и корректность торговой установки.

## Заключение

Стратегия «Parabolic SAR» идеально подходит для трейдеров, которым необходимы точные и автоматические торговые сигналы, основанные на паттернах разворота тренда. Она использует динамическую природу Parabolic SAR для своевременных входов и выходов, повышая потенциал прибыли на быстродвижущихся рынках.
