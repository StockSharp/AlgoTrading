using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using Williams %R crossings with optional trend direction.
/// </summary>
public class FractalWprStrategy : Strategy
{
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<TrendMode> _trend;
	private readonly StrategyParam<bool> _buyPositionOpen;
	private readonly StrategyParam<bool> _sellPositionOpen;
	private readonly StrategyParam<bool> _buyPositionClose;
	private readonly StrategyParam<bool> _sellPositionClose;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<DataType> _candleType;

	private WilliamsR _wpr;
	private decimal? _prevWpr;

	/// <summary>
	/// Williams %R calculation period.
	/// </summary>
	public int WprPeriod { get => _wprPeriod.Value; set => _wprPeriod.Value = value; }

	/// <summary>
	/// Overbought threshold.
	/// </summary>
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }

	/// <summary>
	/// Oversold threshold.
	/// </summary>
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }

	/// <summary>
	/// Trading direction mode.
	/// </summary>
	public TrendMode Trend { get => _trend.Value; set => _trend.Value = value; }

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPositionOpen { get => _buyPositionOpen.Value; set => _buyPositionOpen.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPositionOpen { get => _sellPositionOpen.Value; set => _sellPositionOpen.Value = value; }

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyPositionClose { get => _buyPositionClose.Value; set => _buyPositionClose.Value = value; }

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellPositionClose { get => _sellPositionClose.Value; set => _sellPositionClose.Value = value; }

	/// <summary>
	/// Stop loss distance in ticks.
	/// </summary>
	public int StopLossTicks { get => _stopLossTicks.Value; set => _stopLossTicks.Value = value; }

	/// <summary>
	/// Take profit distance in ticks.
	/// </summary>
	public int TakeProfitTicks { get => _takeProfitTicks.Value; set => _takeProfitTicks.Value = value; }

	/// <summary>
	/// Candle type and timeframe.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="FractalWprStrategy"/> class.
	/// </summary>
	public FractalWprStrategy()
	{
		_wprPeriod = Param(nameof(WprPeriod), 30)
			.SetDisplay("WPR Period", "Williams %R calculation period", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_highLevel = Param(nameof(HighLevel), -30m)
			.SetDisplay("High Level", "Overbought threshold", "Levels");

		_lowLevel = Param(nameof(LowLevel), -70m)
			.SetDisplay("Low Level", "Oversold threshold", "Levels");

		_trend = Param(nameof(Trend), TrendMode.Direct)
			.SetDisplay("Trend Mode", "Trading direction mode", "General");

		_buyPositionOpen = Param(nameof(BuyPositionOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Permissions");

		_sellPositionOpen = Param(nameof(SellPositionOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Permissions");

		_buyPositionClose = Param(nameof(BuyPositionClose), true)
			.SetDisplay("Buy Close", "Allow closing long positions", "Permissions");

		_sellPositionClose = Param(nameof(SellPositionClose), true)
			.SetDisplay("Sell Close", "Allow closing short positions", "Permissions");

		_stopLossTicks = Param(nameof(StopLossTicks), 1000)
			.SetDisplay("Stop Loss", "Stop loss distance in ticks", "Protection")
			.SetGreaterThanZero();

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 2000)
			.SetDisplay("Take Profit", "Take profit distance in ticks", "Protection")
			.SetGreaterThanZero();

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

		_wpr = new WilliamsR { Length = WprPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_wpr, ProcessCandle)
			.Start();

		var step = Security.StepPrice ?? 1m;
		StartProtection(
			stopLoss: new Unit(step * StopLossTicks, UnitTypes.Absolute),
			takeProfit: new Unit(step * TakeProfitTicks, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _wpr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var wpr = wprValue.ToDecimal();

		if (_prevWpr.HasValue && IsFormedAndOnlineAndAllowTrading())
		{
			if (Trend == TrendMode.Direct)
			{
				if (_prevWpr > LowLevel && wpr <= LowLevel)
				{
					if (BuyPositionOpen && Position <= 0)
						BuyMarket(Volume + Math.Abs(Position));
					if (SellPositionClose && Position < 0)
						BuyMarket(Math.Abs(Position));
				}

				if (_prevWpr < HighLevel && wpr >= HighLevel)
				{
					if (SellPositionOpen && Position >= 0)
						SellMarket(Volume + Math.Abs(Position));
					if (BuyPositionClose && Position > 0)
						SellMarket(Position);
				}
			}
			else
			{
				if (_prevWpr > LowLevel && wpr <= LowLevel)
				{
					if (SellPositionOpen && Position >= 0)
						SellMarket(Volume + Math.Abs(Position));
					if (BuyPositionClose && Position > 0)
						SellMarket(Position);
				}

				if (_prevWpr < HighLevel && wpr >= HighLevel)
				{
					if (BuyPositionOpen && Position <= 0)
						BuyMarket(Volume + Math.Abs(Position));
					if (SellPositionClose && Position < 0)
						BuyMarket(Math.Abs(Position));
				}
			}
		}

		_prevWpr = wpr;
	}

	/// <summary>
	/// Trend trading modes.
	/// </summary>
	public enum TrendMode
	{
		Direct,
		Against
	}
}
