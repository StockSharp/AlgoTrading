namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Basic RSI template converted from the MetaTrader expert advisor.
/// </summary>
public class BasicRsiEaTemplateStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _orderVolume;

	private RelativeStrengthIndex? _rsi;

	/// <summary>
	/// Candle type and timeframe used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of periods for the RSI calculation.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level that triggers short signals.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// RSI level that triggers long signals.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Order volume to send with market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BasicRsiEaTemplateStrategy"/> class.
	/// </summary>
	public BasicRsiEaTemplateStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to compute indicators", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Number of bars for Relative Strength Index", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
			.SetRange(0m, 100m)
			.SetDisplay("Overbought Level", "RSI threshold that allows short entries", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("Oversold Level", "RSI threshold that allows long entries", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_stopLossPips = Param(nameof(StopLossPips), 30m)
			.SetRange(0m, 1000m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetRange(0m, 1000m)
			.SetDisplay("Take Profit (pips)", "Profit target distance expressed in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_orderVolume = Param(nameof(OrderVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Default volume for market orders", "Trading");
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

		// Reset indicator reference between runs.
		_rsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var takeProfitUnit = CreateProtectionUnit(TakeProfitPips);
		var stopLossUnit = CreateProtectionUnit(StopLossPips);

		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		// Only react on finished candles to avoid duplicate orders.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the strategy is ready and trading is allowed.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Wait until the indicator has enough data to produce stable output.
		if (_rsi == null || !_rsi.IsFormed)
			return;

		var volume = OrderVolume;
		if (volume <= 0m)
			return;

		if (Position == 0m)
		{
			if (rsiValue < OversoldLevel)
			{
				BuyMarket(volume);
				LogInfo($"Opening long position: RSI {rsiValue:F2} below {OversoldLevel:F2}.");
			}
			else if (rsiValue > OverboughtLevel)
			{
				SellMarket(volume);
				LogInfo($"Opening short position: RSI {rsiValue:F2} above {OverboughtLevel:F2}.");
			}
		}
	}

	private Unit? CreateProtectionUnit(decimal distanceInPips)
	{
		if (distanceInPips <= 0m)
			return null;

		var security = Security;
		var step = security?.PriceStep;
		if (step == null || step <= 0m)
			return null;

		var multiplier = security!.Decimals is 3 or 5 ? 10m : 1m;
		var distance = distanceInPips * step.Value * multiplier;

		return distance > 0m ? new Unit(distance, UnitTypes.Absolute) : null;
	}
}
