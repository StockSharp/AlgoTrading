using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on zero crossing of the linear regression slope.
/// </summary>
public class CoeffofLineTrueStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _slopePeriod;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	private readonly List<decimal> _slopes = [];
	private LinearRegSlope _slope = null!;

	/// <summary>
	/// Timeframe for candle subscription.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Period for linear regression slope.
	/// </summary>
	public int SlopePeriod { get => _slopePeriod.Value; set => _slopePeriod.Value = value; }

	/// <summary>
	/// Bar index used for signal evaluation.
	/// </summary>
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }

	/// <summary>
	/// Permission to open long positions.
	/// </summary>
	public bool BuyPosOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }

	/// <summary>
	/// Permission to open short positions.
	/// </summary>
	public bool SellPosOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }

	/// <summary>
	/// Permission to close long positions.
	/// </summary>
	public bool BuyPosClose { get => _buyClose.Value; set => _buyClose.Value = value; }

	/// <summary>
	/// Permission to close short positions.
	/// </summary>
	public bool SellPosClose { get => _sellClose.Value; set => _sellClose.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="CoeffofLineTrueStrategy"/>.
	/// </summary>
	public CoeffofLineTrueStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");

		_slopePeriod = Param(nameof(SlopePeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slope Period", "Linear regression length", "Parameters");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Signal Bar", "Historical bar index for signal", "Parameters");

		_buyOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Trading");

		_sellOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Trading");

		_buyClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Buy Close", "Allow closing long positions", "Trading");

		_sellClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Sell Close", "Allow closing short positions", "Trading");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_slope = new LinearRegSlope { Length = SlopePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_slope, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal slope)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_slopes.Add(slope);

		if (_slopes.Count > SignalBar + 2)
		_slopes.RemoveAt(0);

		if (_slopes.Count <= SignalBar + 1)
		return;

		var prev = _slopes[^ (SignalBar + 1)];
		var prev2 = _slopes[^ (SignalBar + 2)];

		var buyOpen = BuyPosOpen && prev2 <= 0m && prev > 0m;
		var sellOpen = SellPosOpen && prev2 >= 0m && prev < 0m;
		var buyClose = BuyPosClose && prev2 >= 0m && prev < 0m;
		var sellClose = SellPosClose && prev2 <= 0m && prev > 0m;

		if (buyClose && Position > 0)
		SellMarket(Position);

		if (sellClose && Position < 0)
		BuyMarket(-Position);

		if (buyOpen)
		{
		if (Position < 0)
		BuyMarket(-Position);
		BuyMarket();
		}

		if (sellOpen)
		{
		if (Position > 0)
		SellMarket(Position);
		SellMarket();
		}
	}
}

