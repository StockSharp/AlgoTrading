using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Three Red / Three Green Strategy with ATR filter.
/// </summary>
public class ThreeRedGreenVolatilityStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maxTradeDuration;
	private readonly StrategyParam<bool> _useGreenExit;
	private readonly StrategyParam<int> _atrPeriod;

	private AverageTrueRange _atr;
	private SimpleMovingAverage _atrAverage;

	private int _redCount;
	private int _greenCount;
	private int _barIndex;
	private int? _entryBarIndex;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum trade duration in bars.
	/// </summary>
	public int MaxTradeDuration
	{
		get => _maxTradeDuration.Value;
		set => _maxTradeDuration.Value = value;
	}

	/// <summary>
	/// Use three green candles exit.
	/// </summary>
	public bool UseGreenExit
	{
		get => _useGreenExit.Value;
		set => _useGreenExit.Value = value;
	}

	/// <summary>
	/// ATR period (0 disables filter).
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ThreeRedGreenVolatilityStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maxTradeDuration = Param(nameof(MaxTradeDuration), 22)
			.SetGreaterThanZero()
			.SetDisplay("Max Trade Duration", "Maximum bars in position", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_useGreenExit = Param(nameof(UseGreenExit), true)
			.SetDisplay("Use Green Exit", "Exit after three green candles", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 12)
			.SetRange(0, 100)
			.SetDisplay("ATR Period", "ATR period (0 disables)", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(0, 30, 1);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_redCount = 0;
		_greenCount = 0;
		_barIndex = 0;
		_entryBarIndex = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = Math.Max(1, AtrPeriod) };
		_atrAverage = new SimpleMovingAverage { Length = 30 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawIndicator(area, _atrAverage);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atr = atrValue.ToDecimal();
		var atrAvg = _atrAverage.Process(new DecimalIndicatorValue(_atrAverage, atr, candle.ServerTime)).ToDecimal();

		var atrEntry = AtrPeriod <= 0 || !_atr.IsFormed || !_atrAverage.IsFormed || atr > atrAvg;

		var redDay = candle.ClosePrice < candle.OpenPrice;
		var greenDay = candle.ClosePrice > candle.OpenPrice;

		_redCount = redDay ? _redCount + 1 : 0;
		_greenCount = greenDay ? _greenCount + 1 : 0;

		var threeRed = _redCount >= 3;
		var threeGreen = _greenCount >= 3;

		if (Position == 0 && threeRed && atrEntry)
		{
			BuyMarket();
			_entryBarIndex = _barIndex;
		}

		var tradeDuration = _entryBarIndex.HasValue ? _barIndex - _entryBarIndex.Value : 0;
		var exitCondition = (UseGreenExit && threeGreen) || (tradeDuration >= MaxTradeDuration);

		if (Position > 0 && exitCondition)
		{
			SellMarket();
			_entryBarIndex = null;
		}

		_barIndex++;
	}
}
