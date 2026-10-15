namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// False Breakout Trap strategy.
/// Detects when price breaks a recent high/low range then reverses back.
/// Trades against the failed breakout direction.
/// Exits on the opposite failed breakout or a percent stop beyond the failed breakout level.
/// </summary>
public class FalseBreakoutTrapStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal? _stopPrice;
	private Order _exitOrder;

	/// <summary>
	/// Lookback period for range.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in percent beyond the failed breakout level.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
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
	public FalseBreakoutTrapStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetRange(5, 50)
			.SetDisplay("Lookback", "Period for high/low range", "Range");

		_stopLoss = Param(nameof(StopLoss), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop distance in percent beyond the failed breakout level", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_highs.Clear();
		_lows.Clear();
		_stopPrice = null;
		_exitOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highs.Clear();
		_lows.Clear();
		_stopPrice = null;
		_exitOrder = null;

		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ProcessQuote).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private bool IsExitPending => _exitOrder is not null &&
		_exitOrder.State is not (OrderStates.Done or OrderStates.Failed);

	private void ProcessQuote(Level1ChangeMessage message)
	{
		var price = Position > 0 ? message.TryGetDecimal(Level1Fields.BestBidPrice)
			: Position < 0 ? message.TryGetDecimal(Level1Fields.BestAskPrice)
			: null;

		if (price is decimal executable && executable > 0m)
			TryStopOut(executable);
	}

	// Long positions are tested against a bid or bar low, short positions against an ask or bar high.
	private bool TryStopOut(decimal price)
	{
		if (_stopPrice is not decimal stop || IsExitPending)
			return false;

		if (Position > 0 && price <= stop || Position < 0 && price >= stop)
		{
			ExitPosition();
			return true;
		}

		return false;
	}

	private void ExitPosition()
	{
		_exitOrder = Position > 0 ? SellMarket(Position) : BuyMarket(-Position);
		_stopPrice = null;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Maintain rolling high/low window
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		if (_highs.Count > LookbackPeriod + 1)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < LookbackPeriod + 1)
			return;

		if (TryStopOut(Position > 0 ? candle.LowPrice : candle.HighPrice))
			return;

		// Find highest high and lowest low of the previous N bars (excluding current)
		decimal rangeHigh = decimal.MinValue;
		decimal rangeLow = decimal.MaxValue;
		for (int i = 0; i < _highs.Count - 1; i++)
		{
			if (_highs[i] > rangeHigh) rangeHigh = _highs[i];
			if (_lows[i] < rangeLow) rangeLow = _lows[i];
		}

		// False upside breakout: candle broke above range high but closed below it
		var falseBreakUp = candle.HighPrice > rangeHigh && candle.ClosePrice < rangeHigh;
		// False downside breakout: candle broke below range low but closed above it
		var falseBreakDown = candle.LowPrice < rangeLow && candle.ClosePrice > rangeLow;

		if (Position > 0 && falseBreakUp || Position < 0 && falseBreakDown)
		{
			if (!IsExitPending)
				ExitPosition();
		}
		else if (Position == 0 && falseBreakDown)
		{
			_stopPrice = rangeLow * (1m - StopLoss / 100m);
			BuyMarket();
		}
		else if (Position == 0 && falseBreakUp)
		{
			_stopPrice = rangeHigh * (1m + StopLoss / 100m);
			SellMarket();
		}
	}
}
