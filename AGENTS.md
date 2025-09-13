# Инструкция - Конвертация стратегий MQL в StockSharp

## Основные требования

### 1. Стиль кода и структура
- **Namespace**: `StockSharp.Samples.Strategies;` (без фигурных скобок, в начале файла)
- **Название класса**: должно заканчиваться на `Strategy` и совпадать с названием файла
- **Табуляция**: ОБЯЗАТЕЛЬНО использовать табы (Tab) для отступов, НЕ пробелы
- **Переменные**: использовать `var` где возможен вывод типа
- **Комментарии**: только на английском языке
- **Общение**: на русском языке
- **MQL**: лежат в папке MQL.
- **Куда сохранять**: API, в конец добавляя новые номера и описание как у других стратегий.

### 2. Подход к написанию
- **Приоритет**: Использовать высокоуровневый API (как в SmaHighLevelStrategy.cs)
- **Fallback**: Низкоуровневый API только если высокоуровневый не подходит
- **Индикаторы**: Использовать готовые индикаторы вместо самописных калькуляций
- **Методы**: Не изобретать методы поиска/регистрации без примеров

### 3. Работа с индикаторами
- **Запрет**: Не использовать `GetValue` или `GetCurrentValue` у индикаторов
- **Предпочтительно**: Использовать значения, передаваемые в обработчике `Bind`
- **Запрет**: Не обращаться к предыдущим значениям через `GetValue` с глубиной
- **Альтернатива**: Хранить результаты в переменных внутри стратегии

### 4. Параметры стратегии
- Создавать через `Param()` в конструкторе
- Использовать `StrategyParam<T>` с соответствующими свойствами
- Применять `SetDisplay()` для UI отображения
- Поддерживать оптимизацию через `SetCanOptimize()`

### 5. Подписки и данные
- **Запрет**: Не использовать накопительные методы типа `GetTrades`, `GetAveragePrice`
- **Предпочтительно**: Использовать подписки для получения данных
- **Переменные**: Обновлять внутренние переменные класса вместо пересчета

### 6. Override методы
- Использовать `/// <inheritdoc />` XML комментарий без дублирования текста

### 7. Цвета и визуализация
- **По умолчанию**: Не указывать явные цвета (автоподбор)
- **Исключение**: Использовать явные цвета только при необходимости
- **Запрет**: Избегать `System.Drawing.Color.Blue` без веской причины

### 8. Коллекции и LINQ
- **Запрет**: Не создавать внутренние коллекции (использовать индикаторы)
- **Запрет**: Избегать LINQ на всей коллекции типа `_recentHighs.Take(_recentHighs.Count - 1).Max()`
- **Альтернатива**: Аккумулирующие переменные или индикаторы

### 9. Высокоуровневый API
- При использовании `Bind` не добавлять индикаторы в `Strategy.Indicators` напрямую
- Использовать `SubscribeCandles()` для подписки на свечи
- Связывать через `.Bind(indicator1, indicator2, OnProcess)`

#### Bind vs BindEx
- **Bind**: Автоматически распаковывает значения индикаторов в decimal
- **BindEx**: Передает типизированное значение индикатора (IIndicatorValue), которое нужно привести к конкретному типу

```cs
// Bind - получаем автоматически распакованные decimal значения
subscription.Bind(sma, OnProcess);
private void OnProcess(ICandleMessage candle, decimal smaValue)

// Bind для комплексных индикаторов - автоматически распаковывает все компоненты
subscription.Bind(bollingerBands, OnProcessBollinger);
private void OnProcessBollinger(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand)

// BindEx - получаем типизированное значение, которое нужно привести к конкретному типу
subscription.BindEx(bollingerBands, OnProcessEx);
private void OnProcessEx(ICandleMessage candle, IIndicatorValue value)
{
	// Приведение к конкретному типу значения индикатора
	var bb = (BollingerBandsValue)value;
	
	// Проверка на null и получение значений
	if (bb.UpBand is not decimal upperBand ||
		bb.LowBand is not decimal lowerBand ||
		bb.MovingAverage is not decimal middleBand)
	{
		return; // Not enough data
	}
	
	// Использование значений
	// ...
}

// BindEx для простых индикаторов
subscription.BindEx(rsi, OnProcessRsiEx);
private void OnProcessRsiEx(ICandleMessage candle, IIndicatorValue rsiValue)
{
	// Проверка финальности значения
	if (!rsiValue.IsFinal)
		return;
		
	var rsi = rsiValue.GetValue<decimal>();
	// ...
}
```

### 10. Лямбда функции
- **Короткие**: 1-2 строчки можно оставить как лямбда
- **Длинные**: Создавать отдельные методы

### 11. Защита позиций
- `StartProtection()` вызывается только 1 раз
- Лучше всего в методе `OnStarted`
- Метод сам отслеживает ненулевую позицию

## Структура файлов и папок

### Топология проекта стратегий
При создании новых стратегий необходимо создать следующую структуру папок:

```
NNNN_StrategyName/
├── CS/
│   └── StrategyNameStrategy.cs    // Основной файл стратегии на C#
├── PY/                            // Папка для Python версии (если будет)
├── README.md                      // Описание на английском
├── README_cn.md                   // Описание на китайском
└── README_ru.md                   // Описание на русском
```

### Пример README файлов

Каждая стратегия должна иметь три README файла с описанием на разных языках:

**README.md** (английский):
```markdown
# Strategy Name
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy description in English with trading logic explanation.

## Details
- **Entry Criteria**: Entry conditions
- **Long/Short**: Direction
- **Exit Criteria**: Exit conditions  
- **Stops**: Stop loss info
- **Default Values**: Parameter defaults
- **Filters**: Strategy characteristics
```

**README_ru.md** (русский):
```markdown
# Название стратегии
[English](README.md) | [中文](README_cn.md)

Описание стратегии на русском языке с объяснением торговой логики.

## Подробности
- **Условия входа**: Условия входа в позицию
- **Направление**: Направление торговли
- **Условия выхода**: Условия выхода из позиции
- **Стопы**: Информация о стоп-лоссах
- **Параметры по умолчанию**: Значения параметров по умолчанию
- **Фильтры**: Характеристики стратегии
```

**README_cn.md** (китайский):
```markdown
# 策略名称
[English](README.md) | [Русский](README_ru.md)

中文策略描述和交易逻辑说明。

## 细节
- **入场条件**: 入场条件
- **方向**: 交易方向
- **出场条件**: 出场条件
- **止损**: 止损信息
- **默认参数**: 默认参数值
- **过滤器**: 策略特征
```

## Структура стратегии (шаблон)

### Простой пример с Bind

```cs
namespace StockSharp.Samples.Strategies;

using System;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

/// <summary>
/// Strategy description in English.
/// </summary>
public class ExampleStrategy : Strategy
{
	private bool? _previousCondition;

	public ExampleStrategy()
	{
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_parameter1 = Param(nameof(Parameter1), 14)
			.SetDisplay("Parameter 1", "Description", "Settings");
	}

	private readonly StrategyParam<DataType> _candleType;
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private readonly StrategyParam<int> _parameter1;
	public int Parameter1
	{
		get => _parameter1.Value;
		set => _parameter1.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		var indicator = new SMA { Length = Parameter1 };

		// Subscribe to candles and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(indicator, OnProcess)
			.Start();

		// Setup visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, indicator);
			DrawOwnTrades(area);
		}

		// Start position protection
		StartProtection(TakeValue, StopValue);
	}

	private void OnProcess(ICandleMessage candle, decimal indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Trading logic here
		
		// Example position opening
		if (/* buy condition */)
		{
			BuyMarket();
		}
		else if (/* sell condition */)
		{
			SellMarket();
		}
	}
}
```

### Пример с BindEx для типизированных значений

```cs
namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy demonstrating BindEx usage for typed indicator values.
/// </summary>
public class BollingerSqueezeStrategy : Strategy
{
	private decimal _previousBandWidth;
	private bool _isFirstValue = true;
	private bool _isInSqueeze = false;

	public BollingerSqueezeStrategy()
	{
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)));
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20);
		_squeezeThreshold = Param(nameof(SqueezeThreshold), 0.1m);
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _squeezeThreshold;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal SqueezeThreshold { get => _squeezeThreshold.Value; set => _squeezeThreshold.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollingerBands = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = 2m
		};

		// Use BindEx to get BollingerBandsValue type
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollingerBands, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Cast to specific indicator value type
		var bb = (BollingerBandsValue)bollingerValue;
		
		// Check for valid values using pattern matching
		if (bb.UpBand is not decimal upperBand ||
			bb.LowBand is not decimal lowerBand ||
			bb.MovingAverage is not decimal middleBand)
		{
			return; // Not enough data to calculate bands
		}

		// Calculate band width for squeeze detection
		decimal bandWidth = (upperBand - lowerBand) / middleBand;

		if (_isFirstValue)
		{
			_previousBandWidth = bandWidth;
			_isFirstValue = false;
			return;
		}

		// Detect squeeze and breakout logic
		bool isSqueeze = bandWidth < SqueezeThreshold;

		if (_isInSqueeze && !isSqueeze && bandWidth > _previousBandWidth)
		{
			// Breakout from squeeze
			if (candle.ClosePrice > upperBand && Position <= 0)
			{
				BuyMarket();
			}
			else if (candle.ClosePrice < lowerBand && Position >= 0)
			{
				SellMarket();
			}
		}

		_isInSqueeze = isSqueeze;
		_previousBandWidth = bandWidth;
	}
}
```

## Часто используемые индикаторы

- `SMA` - Simple Moving Average
- `EMA` - Exponential Moving Average  
- `RSI` - Relative Strength Index
- `MACD` - Moving Average Convergence Divergence
- `BollingerBands` - Bollinger Bands
- `Stochastic` - Stochastic Oscillator
- `WilliamsR` - Williams %R
- `ADX` - Average Directional Index
- `ATR` - Average True Range
- `CCI` - Commodity Channel Index

## Правила торговли

- Использовать `BuyMarket()` и `SellMarket()` для рыночных заявок, а `BuyLimit(price)` и `SellLimit(price)` для лимитных
- Каждый метод принимает необязательный параметр `security`: если он не указан, используется `Strategy.Security`, при указании заявки регистрируются по выбранному инструменту
- Проверять `Position` для определения текущей позиции
- Обрабатывать только завершенные свечи (`candle.State == CandleStates.Finished`)
- Сохранять состояние между вызовами в переменных класса

## Что НЕ делать

❌ Не создавать параметры в конструкторе стратегии  
❌ Не использовать `GetValue()` у индикаторов  
❌ Не создавать собственные коллекции данных  
❌ Не использовать LINQ на больших коллекциях  
❌ Не указывать цвета без необходимости  
❌ Не делать длинные лямбда функции  
❌ Не использовать накопительные методы  
❌ Не добавлять индикаторы в `Strategy.Indicators` при использовании `Bind`  
❌ **НИКОГДА не использовать сигнатуру типа `(ICandleMessage candle, BollingerBands bb)` в BindEx** - BindEx передает только IIndicatorValue!

## Правильные сигнатуры методов

```cs
// ✅ Правильно - Bind автоматически распаковывает
private void OnProcess(ICandleMessage candle, decimal smaValue)
private void OnProcessBollinger(ICandleMessage candle, decimal middle, decimal upper, decimal lower)

// ✅ Правильно - BindEx передает IIndicatorValue
private void OnProcessEx(ICandleMessage candle, IIndicatorValue value)

// ❌ НЕПРАВИЛЬНО - BindEx НЕ передает объект индикатора!
private void OnProcessWrong(ICandleMessage candle, BollingerBands bb) // ТАКОГО НЕ БЫВАЕТ!
```

## Примеры для изучения

- `SmaHighLevelStrategy.cs` - высокоуровневый API пример
- `SmaStrategy.cs` - низкоуровневый API пример  
- Документация в `high_level_api.md`
- Список индикаторов в `list_of_indicators.md`
- Совместимость в `compatibility.md`