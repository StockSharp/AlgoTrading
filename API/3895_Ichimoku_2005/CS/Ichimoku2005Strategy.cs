using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader strategy "ichimok2005" built around Ichimoku cloud breakouts.
/// Generates long signals when price climbs above Senkou Span B with two consecutive bullish candles.
/// Generates short signals when price falls below Senkou Span B with two consecutive bearish candles.
/// Optional money management adjusts order volume based on portfolio size and maximum risk.
/// </summary>
public class Ichimoku2005Strategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouBPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage[] _candleHistory = Array.Empty<ICandleMessage>();
	private decimal?[] _senkouBHistory = Array.Empty<decimal?>();
	private int _lastSignal;

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Offset used to reference past candles when validating signals.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	/// <summary>
	/// Fixed trade volume when money management is disabled.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Maximum percentage of portfolio value used to size orders when money management is enabled.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Enables dynamic position sizing based on <see cref="MaximumRisk"/>.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Tenkan-sen period for the Ichimoku indicator.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen period for the Ichimoku indicator.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Senkou Span B period for the Ichimoku indicator.
	/// </summary>
	public int SenkouBPeriod
	{
		get => _senkouBPeriod.Value;
		set => _senkouBPeriod.Value = value;
	}

	/// <summary>
	/// The candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="Ichimoku2005Strategy"/>.
	/// </summary>
	public Ichimoku2005Strategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 30m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(10m, 100m, 10m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 60m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Take Profit", "Take profit distance in points", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(20m, 200m, 20m);

		_shift = Param(nameof(Shift), 1)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Shift", "Number of bars back to validate breakouts", "General")
		.SetCanOptimize(true)
		.SetOptimize(0, 3, 1);

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Fixed trade volume when MM is disabled", "Trading")
		.SetCanOptimize(false);

		_maximumRisk = Param(nameof(MaximumRisk), 10m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Maximum Risk %", "Portfolio percent used when MM is enabled", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(1m, 20m, 1m);

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
		.SetDisplay("Use Money Management", "Adjust volume using portfolio risk", "Trading");

		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("Tenkan Period", "Tenkan-sen length", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(5, 15, 1);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("Kijun Period", "Kijun-sen length", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(20, 40, 1);

		_senkouBPeriod = Param(nameof(SenkouBPeriod), 52)
		.SetGreaterThanZero()
		.SetDisplay("Senkou Span B Period", "Senkou Span B length", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(40, 70, 2);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for signals", "General");
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

		_candleHistory = Array.Empty<ICandleMessage>();
		_senkouBHistory = Array.Empty<decimal?>();
		_lastSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouBPeriod }
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(ichimoku, ProcessCandle)
			.Start();

		var historyLength = Math.Max(Shift + 2, 3);
		_candleHistory = new ICandleMessage[historyLength];
		_senkouBHistory = new decimal?[historyLength];

		var priceStep = Security?.PriceStep ?? 0m;
		Unit stopLossUnit = null;
		Unit takeProfitUnit = null;

		if (StopLossPoints > 0m)
		{
			var stopDistance = priceStep > 0m ? StopLossPoints * priceStep : StopLossPoints;
			stopLossUnit = new Unit(stopDistance, UnitTypes.Absolute);
		}

		if (TakeProfitPoints > 0m)
		{
			var takeDistance = priceStep > 0m ? TakeProfitPoints * priceStep : TakeProfitPoints;
			takeProfitUnit = new Unit(takeDistance, UnitTypes.Absolute);
		}

		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit, useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		// Ignore incomplete candles to avoid premature Ichimoku readings.
		if (candle.State != CandleStates.Finished)
			return;

		// Make sure the strategy is connected, formed and allowed to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ichimokuValue = (IchimokuValue)indicatorValue;

		// Require both Senkou spans to be available before evaluating breakouts.
		if (ichimokuValue.SenkouB is not decimal currentSenkouB ||
			ichimokuValue.SenkouA is not decimal currentSenkouA)
			return;

		var chinkouInsideCloud = false;

		// Track whether the lagging span is trapped inside the cloud (avoid congestion).
		if (ichimokuValue.Chinkou is decimal chinkou)
		{
			var upperCloud = Math.Max(currentSenkouA, currentSenkouB);
			var lowerCloud = Math.Min(currentSenkouA, currentSenkouB);
			chinkouInsideCloud = chinkou <= upperCloud && chinkou >= lowerCloud && currentSenkouA < currentSenkouB;
		}

		var shift = Math.Max(0, Shift);

		// Make sure we have enough candle and Senkou Span B history for the breakout pattern.
		var hasRequiredHistory = TryGetCandle(candle, shift + 2, out var shiftPlusTwoCandle) &&
			TryGetCandle(candle, shift + 1, out var shiftPlusOneCandle) &&
			TryGetCandle(candle, shift, out var shiftCandle) &&
			TryGetSenkouB(currentSenkouB, shift + 2, out var senkouBPlusTwo) &&
			TryGetSenkouB(currentSenkouB, shift + 1, out var senkouBPlusOne) &&
			TryGetSenkouB(currentSenkouB, shift, out var senkouBCurrent);

		if (!hasRequiredHistory || shiftPlusTwoCandle == null || shiftPlusOneCandle == null || shiftCandle == null)
		{
			UpdateHistory(candle, currentSenkouB);
			return;
		}

		// Replicate the bullish breakout structure from the original expert advisor.
		var bullishBreakout =
			shiftPlusTwoCandle.OpenPrice < senkouBPlusTwo &&
			shiftPlusOneCandle.OpenPrice > senkouBPlusOne &&
			shiftPlusOneCandle.ClosePrice > senkouBPlusOne &&
			shiftCandle.OpenPrice > senkouBCurrent &&
			shiftCandle.ClosePrice > senkouBCurrent &&
			shiftPlusOneCandle.ClosePrice > shiftPlusOneCandle.OpenPrice &&
			shiftCandle.ClosePrice > shiftCandle.OpenPrice &&
			!chinkouInsideCloud;

		// Mirror the bearish breakout conditions.
		var bearishBreakout =
			shiftPlusTwoCandle.OpenPrice > senkouBPlusTwo &&
			shiftPlusOneCandle.OpenPrice < senkouBPlusOne &&
			shiftPlusOneCandle.ClosePrice < senkouBPlusOne &&
			shiftCandle.OpenPrice < senkouBCurrent &&
			shiftCandle.ClosePrice < senkouBCurrent &&
			shiftPlusOneCandle.ClosePrice < shiftPlusOneCandle.OpenPrice &&
			shiftCandle.ClosePrice < shiftCandle.OpenPrice &&
			!chinkouInsideCloud;

		if (Position > 0)
		{
			// Close long positions when a bearish breakout appears.
			if (bearishBreakout)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			// Close short positions when a bullish breakout appears.
			if (bullishBreakout)
				BuyMarket(Math.Abs(Position));
		}
		else
		{
			// Open new long trades only when the last executed signal was not long.
			if (bullishBreakout && _lastSignal != 1)
			{
				var volume = GetOrderVolume();
				if (volume > 0m)
				{
					BuyMarket(volume);
					_lastSignal = 1;
				}
			}
			// Open new short trades only when the last executed signal was not short.
			else if (bearishBreakout && _lastSignal != -1)
			{
				var volume = GetOrderVolume();
				if (volume > 0m)
				{
					SellMarket(volume);
					_lastSignal = -1;
				}
			}
		}

		UpdateHistory(candle, currentSenkouB);
	}

	private bool TryGetCandle(ICandleMessage currentCandle, int offset, out ICandleMessage candle)
	{
		if (offset == 0)
		{
			candle = currentCandle;
			return true;
		}

		var index = offset - 1;
		if (index >= 0 && index < _candleHistory.Length)
		{
			candle = _candleHistory[index];
			return candle != null;
		}

		candle = null;
		return false;
	}

	private bool TryGetSenkouB(decimal currentSenkouB, int offset, out decimal value)
	{
		if (offset == 0)
		{
			value = currentSenkouB;
			return true;
		}

		var index = offset - 1;
		if (index >= 0 && index < _senkouBHistory.Length && _senkouBHistory[index] is decimal stored)
		{
			value = stored;
			return true;
		}

		value = 0m;
		return false;
	}

	private void UpdateHistory(ICandleMessage candle, decimal senkouB)
	{
		if (_candleHistory.Length == 0)
			return;

		for (var i = _candleHistory.Length - 1; i > 0; i--)
		{
			_candleHistory[i] = _candleHistory[i - 1];
			_senkouBHistory[i] = _senkouBHistory[i - 1];
		}

		_candleHistory[0] = candle;
		_senkouBHistory[0] = senkouB;
	}

	private decimal GetOrderVolume()
	{
		if (!UseMoneyManagement)
			return OrderVolume;

		var portfolio = Portfolio;
		if (portfolio?.CurrentValue is decimal balance && balance > 0m)
		{
			var riskFraction = MaximumRisk / 100m;
			var rawVolume = balance * riskFraction;
			var lotStep = Security?.LotStep ?? 0m;

			if (lotStep <= 0m)
				lotStep = 1m;

			var steps = Math.Max(1m, Math.Round(rawVolume / lotStep, MidpointRounding.AwayFromZero));
			var volume = steps * lotStep;

			if (volume >= lotStep)
				return volume;
		}

		return OrderVolume;
	}
}

