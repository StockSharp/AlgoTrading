using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA/SMA + RSI crossover strategy.
/// Goes long when the fast EMA crosses above the medium EMA on a bullish candle closing above the slow EMA, and short
/// on the mirrored setup. A long closes when RSI rises above 70 or when it has been held XBars bars and is in profit;
/// a short closes when RSI falls below 30 or after XBars bars in profit.
/// </summary>
public class EmaSmaRsiStrategy : Strategy
{
	private const decimal _rsiOverbought = 70m;
	private const decimal _rsiOversold = 30m;

	private readonly StrategyParam<int> _emaFast;
	private readonly StrategyParam<int> _emaMedium;
	private readonly StrategyParam<int> _emaSlow;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _xBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevMedium;
	private decimal _entryPrice;
	private int _barsInPosition;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int EMA_fast
	{
		get => _emaFast.Value;
		set => _emaFast.Value = value;
	}

	/// <summary>
	/// Medium EMA period.
	/// </summary>
	public int EMA_medium
	{
		get => _emaMedium.Value;
		set => _emaMedium.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int EMA_slow
	{
		get => _emaSlow.Value;
		set => _emaSlow.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RSI_length
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Bars after which a profitable position is closed. 0 disables the time exit.
	/// </summary>
	public int XBars
	{
		get => _xBars.Value;
		set => _xBars.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public EmaSmaRsiStrategy()
	{
		_emaFast = Param(nameof(EMA_fast), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Fast", "Fast EMA period", "Moving Averages");

		_emaMedium = Param(nameof(EMA_medium), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Medium", "Medium EMA period", "Moving Averages");

		_emaSlow = Param(nameof(EMA_slow), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA Slow", "Slow EMA period", "Moving Averages");

		_rsiLength = Param(nameof(RSI_length), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_xBars = Param(nameof(XBars), 24)
			.SetNotNegative()
			.SetDisplay("X Bars", "Bars after which a profitable position is closed", "Exit");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = null;
		_prevMedium = null;
		_entryPrice = 0m;
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevMedium = null;
		_entryPrice = 0m;
		_barsInPosition = 0;

		var fast = new ExponentialMovingAverage { Length = EMA_fast };
		var medium = new ExponentialMovingAverage { Length = EMA_medium };
		var slow = new ExponentialMovingAverage { Length = EMA_slow };
		var rsi = new RelativeStrengthIndex { Length = RSI_length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fast, medium, slow, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, medium);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);

			var rsiArea = CreateChartArea();
			if (rsiArea != null)
				DrawIndicator(rsiArea, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue mediumValue, IIndicatorValue slowValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFormed || !mediumValue.IsFormed || !slowValue.IsFormed || !rsiValue.IsFormed)
			return;

		var fast = fastValue.ToDecimal();
		var medium = mediumValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();

		var prevFast = _prevFast;
		var prevMedium = _prevMedium;
		_prevFast = fast;
		_prevMedium = medium;

		if (Position != 0)
			_barsInPosition++;

		if (prevFast is not decimal lastFast || prevMedium is not decimal lastMedium)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		var longSignal = fast > medium && lastFast <= lastMedium && close > slow && close > candle.OpenPrice;
		var shortSignal = fast < medium && lastFast >= lastMedium && close < slow && close < candle.OpenPrice;

		var timeUp = XBars > 0 && _barsInPosition >= XBars;

		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = close;
			_barsInPosition = 0;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = close;
			_barsInPosition = 0;
		}
		else if (Position > 0 && (rsi > _rsiOverbought || (timeUp && close > _entryPrice)))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (rsi < _rsiOversold || (timeUp && close < _entryPrice)))
		{
			BuyMarket(-Position);
		}
	}
}
