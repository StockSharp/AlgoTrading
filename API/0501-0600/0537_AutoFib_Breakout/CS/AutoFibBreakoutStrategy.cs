using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AutoFib breakout strategy.
/// The swing low and high of the previous PivotPeriod candles span a Fibonacci extension at low + (high - low) * FibLevel. A
/// close above that level while the close is above the EMA opens a long. Each entry fixes a 1.5 ATR stop-loss and a 3 ATR
/// take-profit from the entry close. Long only.
/// </summary>
public class AutoFibBreakoutStrategy : Strategy
{
	private const decimal _stopAtrMultiple = 1.5m;
	private const decimal _takeAtrMultiple = 3m;

	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _fibLevel;
	private readonly StrategyParam<int> _pivotPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Trend EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// ATR period of the stop-loss and take-profit.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Fibonacci extension level of the breakout.
	/// </summary>
	public decimal FibLevel
	{
		get => _fibLevel.Value;
		set => _fibLevel.Value = value;
	}

	/// <summary>
	/// Candles that define the swing high and low.
	/// </summary>
	public int PivotPeriod
	{
		get => _pivotPeriod.Value;
		set => _pivotPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AutoFibBreakoutStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Trend EMA period", "Trend");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period of the stop-loss and take-profit", "Risk");

		_fibLevel = Param(nameof(FibLevel), 1.618m)
			.SetGreaterThanZero()
			.SetDisplay("Fib Level", "Fibonacci extension level of the breakout", "Fibonacci");

		_pivotPeriod = Param(nameof(PivotPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Period", "Candles that define the swing high and low", "Fibonacci");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_prevHigh = null;
		_prevLow = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var highest = new Highest { Length = PivotPeriod };
		var lowest = new Lowest { Length = PivotPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal atr, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The swing range comes from the candles before this one.
		var prevHigh = _prevHigh;
		var prevLow = _prevLow;
		_prevHigh = highest;
		_prevLow = lowest;

		if (prevHigh is not decimal swingHigh || prevLow is not decimal swingLow)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket(Position);

			return;
		}

		var extension = swingLow + (swingHigh - swingLow) * FibLevel;

		if (Position == 0 && close > extension && close > ema)
		{
			BuyMarket(Volume);
			_stopPrice = close - _stopAtrMultiple * atr;
			_takePrice = close + _takeAtrMultiple * atr;
		}
	}
}
