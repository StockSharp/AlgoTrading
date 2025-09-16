using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Williams %R indicator crossing specified levels.
/// </summary>
public class WprLevelCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<TrendMode> _trend;
	private readonly StrategyParam<bool> _enableBuyEntry;
	private readonly StrategyParam<bool> _enableSellEntry;
	private readonly StrategyParam<bool> _enableBuyExit;
	private readonly StrategyParam<bool> _enableSellExit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevWr;

	/// <summary>
	/// Lookback period for Williams %R.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Upper threshold to detect overbought levels.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold to detect oversold levels.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Trend mode: Direct trades with indicator, Against inverts signals.
	/// </summary>
	public TrendMode Trend
	{
		get => _trend.Value;
		set => _trend.Value = value;
	}

	/// <summary>
	/// Enable opening of long positions.
	/// </summary>
	public bool EnableBuyEntry
	{
		get => _enableBuyEntry.Value;
		set => _enableBuyEntry.Value = value;
	}

	/// <summary>
	/// Enable opening of short positions.
	/// </summary>
	public bool EnableSellEntry
	{
		get => _enableSellEntry.Value;
		set => _enableSellEntry.Value = value;
	}

	/// <summary>
	/// Enable closing of short positions.
	/// </summary>
	public bool EnableBuyExit
	{
		get => _enableBuyExit.Value;
		set => _enableBuyExit.Value = value;
	}

	/// <summary>
	/// Enable closing of long positions.
	/// </summary>
	public bool EnableSellExit
	{
		get => _enableSellExit.Value;
		set => _enableSellExit.Value = value;
	}

	/// <summary>
	/// Stop loss value in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit value in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="WprLevelCrossStrategy"/>.
	/// </summary>
	public WprLevelCrossStrategy()
	{
		_wprPeriod = Param(nameof(WprPeriod), 14)
			.SetDisplay("WPR Period", "Lookback period for Williams %R", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 2);

		_highLevel = Param(nameof(HighLevel), -40m)
			.SetDisplay("High Level", "Overbought threshold", "Indicators");

		_lowLevel = Param(nameof(LowLevel), -60m)
			.SetDisplay("Low Level", "Oversold threshold", "Indicators");

		_trend = Param(nameof(Trend), TrendMode.Direct)
			.SetDisplay("Trend Mode", "Direct - trade with indicator; Against - inverse signals", "General");

		_enableBuyEntry = Param(nameof(EnableBuyEntry), true)
			.SetDisplay("Enable Buy Entry", "Allow opening long positions", "Trading");

		_enableSellEntry = Param(nameof(EnableSellEntry), true)
			.SetDisplay("Enable Sell Entry", "Allow opening short positions", "Trading");

		_enableBuyExit = Param(nameof(EnableBuyExit), true)
			.SetDisplay("Enable Buy Exit", "Allow closing short positions", "Trading");

		_enableSellExit = Param(nameof(EnableSellExit), true)
			.SetDisplay("Enable Sell Exit", "Allow closing long positions", "Trading");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
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
		_prevWr = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var wpr = new WilliamsR { Length = WprPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wpr, ProcessCandle).Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Absolute),
			new Unit(StopLoss, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, wpr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure strategy is ready for trading
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevWr = wr;
			return;
		}

		var crossedBelowLow = _prevWr > LowLevel && wr <= LowLevel;
		var crossedAboveHigh = _prevWr < HighLevel && wr >= HighLevel;

		if (Trend == TrendMode.Direct)
		{
			if (crossedBelowLow)
			{
				if (EnableBuyEntry && Position <= 0)
					BuyMarket(Volume + (Position < 0 ? -Position : 0m));

				if (EnableSellExit && Position < 0)
					BuyMarket(-Position);
			}

			if (crossedAboveHigh)
			{
				if (EnableSellEntry && Position >= 0)
					SellMarket(Volume + (Position > 0 ? Position : 0m));

				if (EnableBuyExit && Position > 0)
					SellMarket(Position);
			}
		}
		else
		{
			if (crossedBelowLow)
			{
				if (EnableSellEntry && Position >= 0)
					SellMarket(Volume + (Position > 0 ? Position : 0m));

				if (EnableBuyExit && Position > 0)
					SellMarket(Position);
			}

			if (crossedAboveHigh)
			{
				if (EnableBuyEntry && Position <= 0)
					BuyMarket(Volume + (Position < 0 ? -Position : 0m));

				if (EnableSellExit && Position < 0)
					BuyMarket(-Position);
			}
		}

		_prevWr = wr;
	}

	/// <summary>
	/// Trend modes for interpreting Williams %R signals.
	/// </summary>
	public enum TrendMode
	{
		/// <summary>
		/// Trade in the direction of indicator signals.
		/// </summary>
		Direct,

		/// <summary>
		/// Invert indicator signals for counter-trend trading.
		/// </summary>
		Against
	}
}
