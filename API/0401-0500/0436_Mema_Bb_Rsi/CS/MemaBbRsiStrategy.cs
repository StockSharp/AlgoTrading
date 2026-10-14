using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi EMA + Bollinger Bands + RSI strategy.
/// Goes long when the close is above the fast EMA on a candle whose low pierced the lower band, and short when the
/// close is below the fast EMA on a candle whose high pierced the upper band while RSI is above 50. A long closes when
/// RSI rises above RSIOversold and a short when the close drops below the lower band. After XBars bars a position that
/// is in profit is closed as well. Each side can be switched off. The slow EMA is plotted as a trend reference.
/// </summary>
public class MemaBbRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _xBars;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private int _barsInPosition;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int Ma1Period
	{
		get => _ma1Period.Value;
		set => _ma1Period.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int Ma2Period
	{
		get => _ma2Period.Value;
		set => _ma2Period.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BBLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BBMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RSILength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI level above which a long is closed.
	/// </summary>
	public decimal RSIOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
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
	/// Allow long trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
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
	public MemaBbRsiStrategy()
	{
		_ma1Period = Param(nameof(Ma1Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Period", "Fast EMA period", "Moving Average");

		_ma2Period = Param(nameof(Ma2Period), 55)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Period", "Slow EMA period", "Moving Average");

		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands");

		_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("BB Multiplier", "Bollinger Bands standard deviation multiplier", "Bollinger Bands");

		_rsiLength = Param(nameof(RSILength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_rsiOversold = Param(nameof(RSIOversold), 71m)
			.SetDisplay("RSI Exit Level", "RSI level above which a long is closed", "RSI");

		_xBars = Param(nameof(XBars), 12)
			.SetNotNegative()
			.SetDisplay("X Bars", "Bars after which a profitable position is closed, 0 disables", "Exit");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long trades", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short trades", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = 0m;
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0m;
		_barsInPosition = 0;

		var ma1 = new ExponentialMovingAverage { Length = Ma1Period };
		var ma2 = new ExponentialMovingAverage { Length = Ma2Period };
		var bollinger = new BollingerBands { Length = BBLength, Width = BBMultiplier };
		var rsi = new RelativeStrengthIndex { Length = RSILength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ma1, ma2, bollinger, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma1);
			DrawIndicator(area, ma2);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);

			var rsiArea = CreateChartArea();
			if (rsiArea != null)
				DrawIndicator(rsiArea, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ma1Value, IIndicatorValue ma2Value, IIndicatorValue bollingerValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position != 0)
			_barsInPosition++;

		if (!ma1Value.IsFormed || !bollingerValue.IsFormed || !rsiValue.IsFormed)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ma1 = ma1Value.ToDecimal();
		var rsi = rsiValue.ToDecimal();
		var close = candle.ClosePrice;

		var longSignal = EnableLong && close > ma1 && candle.LowPrice < lower;
		var shortSignal = EnableShort && close < ma1 && candle.HighPrice > upper && rsi > 50m;

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
		else if (Position > 0 && (rsi > RSIOversold || (timeUp && close > _entryPrice)))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (close < lower || (timeUp && close < _entryPrice)))
		{
			BuyMarket(-Position);
		}
	}
}
