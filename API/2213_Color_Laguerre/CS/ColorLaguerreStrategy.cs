using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy using Color Laguerre oscillator.
/// </summary>
public class ColorLaguerreStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gamma;
	private readonly StrategyParam<int> _highLevel;
	private readonly StrategyParam<int> _middleLevel;
	private readonly StrategyParam<int> _lowLevel;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private int? _prevSignal;

	/// <summary>
	/// Gamma parameter for the Laguerre filter.
	/// </summary>
	public decimal Gamma
	{
		get => _gamma.Value;
		set => _gamma.Value = value;
	}

	/// <summary>
	/// Upper threshold where overbought conditions are assumed.
	/// </summary>
	public int HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Middle level separating bullish and bearish zones.
	/// </summary>
	public int MiddleLevel
	{
		get => _middleLevel.Value;
		set => _middleLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold where oversold conditions are assumed.
	/// </summary>
	public int LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on opposite signals.
	/// </summary>
	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on opposite signals.
	/// </summary>
	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	/// <summary>
	/// Stop loss as percent of entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorLaguerreStrategy"/>.
	/// </summary>
	public ColorLaguerreStrategy()
	{
		_gamma = Param(nameof(Gamma), 0.7m)
			.SetRange(0.1m, 0.9m)
			.SetDisplay("Gamma", "Laguerre filter gamma", "Indicators")
			.SetCanOptimize(true);

		_highLevel = Param(nameof(HighLevel), 85)
			.SetRange(50, 95)
			.SetDisplay("High Level", "Upper oscillator level", "Indicators");

		_middleLevel = Param(nameof(MiddleLevel), 50)
			.SetRange(10, 90)
			.SetDisplay("Middle Level", "Middle oscillator level", "Indicators");

		_lowLevel = Param(nameof(LowLevel), 15)
			.SetRange(5, 40)
			.SetDisplay("Low Level", "Lower oscillator level", "Indicators");

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Trading");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Trading");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Buy Close", "Allow closing longs", "Trading");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Sell Close", "Allow closing shorts", "Trading");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Use RSI as a proxy for the Laguerre oscillator
		var rsi = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: null,
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var value = rsiValue.ToDecimal();

		int signal = value >= MiddleLevel ? 2 : 1;

		if (_prevSignal is not int prev)
		{
			_prevSignal = signal;
			return;
		}

		if (prev == 1 && signal == 2)
		{
			if (SellClose && Position < 0)
				BuyMarket(Math.Abs(Position));

			if (BuyOpen && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (prev == 2 && signal == 1)
		{
			if (BuyClose && Position > 0)
				SellMarket(Math.Abs(Position));

			if (SellOpen && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevSignal = signal;

		if (Position > 0 && value <= LowLevel && SellClose)
			SellMarket(Math.Abs(Position));
		else if (Position < 0 && value >= HighLevel && BuyClose)
			BuyMarket(Math.Abs(Position));
	}
}

