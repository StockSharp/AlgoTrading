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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that waits for price retracements toward the Parabolic SAR value before entering.
/// It sells when price rallies toward a bearish SAR and buys when price pulls back to a bullish SAR.
/// </summary>
public class ParaRetraceStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<decimal> _retraceOffsetPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	// Cached pip size to convert pip-based parameters into absolute price distances.
	private decimal _pipSize;

	// Track the most recent pending trigger levels for transparency in logs and charts.
	private decimal? _pendingSellPrice;
	private decimal? _pendingBuyPrice;

	/// <summary>
	/// Step value for Parabolic SAR acceleration.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// Distance from SAR (in pips) used to trigger entries.
	/// </summary>
	public decimal RetraceOffsetPips
	{
		get => _retraceOffsetPips.Value;
		set => _retraceOffsetPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters with defaults inspired by the original MQL4 script.
	/// </summary>
	public ParaRetraceStrategy()
	{
		_sarStep = Param(nameof(SarStep), 0.01m)
			.SetDisplay("SAR Step", "Acceleration step for Parabolic SAR", "Indicators")
			.SetRange(0.005m, 0.05m)
			.SetCanOptimize(true);

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetDisplay("SAR Max", "Maximum acceleration for Parabolic SAR", "Indicators")
			.SetRange(0.1m, 0.5m)
			.SetCanOptimize(true);

		_retraceOffsetPips = Param(nameof(RetraceOffsetPips), 30m)
			.SetDisplay("Retrace Offset", "Pip distance between price and SAR to trigger entries", "Entries")
			.SetRange(0m, 200m)
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 30m)
			.SetDisplay("Stop Loss", "Protective stop distance in pips", "Risk")
			.SetRange(0m, 300m)
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 30m)
			.SetDisplay("Take Profit", "Profit target distance in pips", "Risk")
			.SetRange(0m, 400m)
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");

		// Default trading volume to a single lot unless configured otherwise.
		Volume = 1m;
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

		_pipSize = 0m;
		_pendingSellPrice = null;
		_pendingBuyPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Estimate pip size from the security metadata (defaulting to one if unavailable).
		var priceStep = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;
		var pipMultiplier = decimals >= 3 ? 10m : 1m;
		_pipSize = priceStep * pipMultiplier;

		// Configure protective orders before any trade is placed.
		if (TakeProfitPips > 0m || StopLossPips > 0m)
		{
			StartProtection(
				takeProfit: TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null,
				stopLoss: StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null);
		}

		// Create and bind the Parabolic SAR indicator using the high-level API.
		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMax
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(parabolicSar, ProcessCandle)
			.Start();

		// Draw candles, indicator, and trades when charting is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		// Only react to finished candles to avoid acting on partial information.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the strategy is connected, warmed up, and allowed to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Without a valid pip size the distance-based parameters cannot be applied.
		if (_pipSize <= 0m)
			return;

		var offset = RetraceOffsetPips * _pipSize;
		if (offset <= 0m)
		{
			_pendingBuyPrice = null;
			_pendingSellPrice = null;
			return;
		}

		var volume = Volume + Math.Abs(Position);

		// Bearish setup: price entirely below SAR, wait for a pullback toward the SAR value.
		if (candle.HighPrice < sarValue && candle.LowPrice < sarValue)
		{
			var targetPrice = sarValue - offset;
			_pendingSellPrice = targetPrice;
			_pendingBuyPrice = null;

			if (Position <= 0 && candle.HighPrice >= targetPrice)
			{
				SellMarket(volume);
				LogInfo($"Sell triggered near {targetPrice} with SAR at {sarValue} and candle close {candle.ClosePrice}.");
			}
		}
		else
		{
			// Bullish setup: price is above or touching SAR, look for a dip toward SAR before buying.
			var targetPrice = sarValue + offset;
			_pendingBuyPrice = targetPrice;
			_pendingSellPrice = null;

			if (Position >= 0 && candle.LowPrice <= targetPrice)
			{
				BuyMarket(volume);
				LogInfo($"Buy triggered near {targetPrice} with SAR at {sarValue} and candle close {candle.ClosePrice}.");
			}
		}
	}
}

