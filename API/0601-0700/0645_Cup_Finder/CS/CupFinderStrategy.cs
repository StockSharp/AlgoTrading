using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Cup Finder strategy.
/// The previous Lookback candles are split into thirds. A cup has rims (highest highs of the outer thirds) within WidthPercent
/// of each other and a middle third that stays below the lower rim; a close above the higher rim buys. An inverted cup has
/// troughs (lowest lows of the outer thirds) within WidthPercent of each other and a middle third that stays above the higher
/// trough; a close below the lower trough sells short. An opposite breakout reverses the position and a percent stop limits the loss.
/// </summary>
public class CupFinderStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _widthPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<(decimal high, decimal low)> _bars = new();

	/// <summary>
	/// Candles the cup is searched in.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Maximum difference between the two rims in percent.
	/// </summary>
	public decimal WidthPercent
	{
		get => _widthPercent.Value;
		set => _widthPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	public CupFinderStrategy()
	{
		_lookback = Param(nameof(Lookback), 150)
			.SetRange(3, 10000)
			.SetDisplay("Lookback", "Candles the cup is searched in", "Pattern");

		_widthPercent = Param(nameof(WidthPercent), 5m)
			.SetNotNegative()
			.SetDisplay("Width %", "Maximum difference between the two rims in percent", "Pattern");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_bars.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bars.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(new Unit(), StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var ready = _bars.Count >= Lookback;
		var bullish = false;
		var bearish = false;

		if (ready)
		{
			var bars = _bars.ToArray();
			var third = bars.Length / 3;
			var left = bars.Take(third).ToArray();
			var middle = bars.Skip(third).Take(bars.Length - 2 * third).ToArray();
			var right = bars.Skip(bars.Length - third).ToArray();

			var leftRim = left.Max(b => b.high);
			var rightRim = right.Max(b => b.high);
			var middleHigh = middle.Max(b => b.high);
			var rim = Math.Max(leftRim, rightRim);
			var isCup = rim > 0 && (rim - Math.Min(leftRim, rightRim)) / rim * 100m <= WidthPercent && middleHigh < Math.Min(leftRim, rightRim);
			bullish = isCup && candle.ClosePrice > rim;

			var leftTrough = left.Min(b => b.low);
			var rightTrough = right.Min(b => b.low);
			var middleLow = middle.Min(b => b.low);
			var trough = Math.Min(leftTrough, rightTrough);
			var higherTrough = Math.Max(leftTrough, rightTrough);
			var isInverted = higherTrough > 0 && (higherTrough - trough) / higherTrough * 100m <= WidthPercent && middleLow > higherTrough;
			bearish = isInverted && candle.ClosePrice < trough;
		}

		_bars.Enqueue((candle.HighPrice, candle.LowPrice));
		while (_bars.Count > Lookback)
			_bars.Dequeue();

		if (!ready || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (bullish && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (bearish && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
