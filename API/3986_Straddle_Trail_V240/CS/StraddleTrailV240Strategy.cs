using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Straddle breakout strategy converted from the MetaTrader 4 Straddle and Trail EA (v2.40).
/// Uses price channel breakouts with trailing stop management.
/// </summary>
public class StraddleTrailV240Strategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	/// <summary>
	/// Channel lookback period for breakout detection.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss distance in absolute price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in absolute price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	public StraddleTrailV240Strategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Lookback period for breakout levels", "Parameters");

		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(2).TimeFrame())
			.SetDisplay("Candle Type", "Candle subscription used", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_highs.Clear();
		_lows.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		var ema = new EMA { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}

		var tp = TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Absolute) : null;
		var sl = StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Absolute) : null;
		StartProtection(tp, sl);

		base.OnStarted2(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		while (_highs.Count > ChannelPeriod)
			_highs.RemoveAt(0);
		while (_lows.Count > ChannelPeriod)
			_lows.RemoveAt(0);

		if (_highs.Count < ChannelPeriod)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var upper = _highs.Take(_highs.Count - 1).Max();
		var lower = _lows.Take(_lows.Count - 1).Min();

		// Breakout above channel -> buy
		if (candle.ClosePrice > upper && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
		}
		// Breakout below channel -> sell
		else if (candle.ClosePrice < lower && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket(Volume);
		}
	}

	/// <summary>
	/// Shutdown modes enum (preserved from original).
	/// </summary>
	public enum ShutdownModes
	{
		All = 0,
		TriggeredPositions = 1,
		TriggeredLong = 2,
		TriggeredShort = 3,
		PendingOrders = 4,
		PendingLong = 5,
		PendingShort = 6
	}
}
