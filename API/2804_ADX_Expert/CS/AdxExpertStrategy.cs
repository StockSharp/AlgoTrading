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
/// ADX crossover strategy translated from the original MQL expert.
/// Opens a single position when DI lines cross while ADX remains weak.
/// </summary>
public class AdxExpertStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _maxSpreadPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx = null!;
	private decimal _previousPlusDi;
	private decimal _previousMinusDi;
	private bool _hasPreviousDi;

	/// <summary>
	/// Trading volume for every market order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Maximum ADX level that still allows new trades.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Maximum allowed bid-ask spread measured in price points.
	/// </summary>
	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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
	/// Initializes a new instance of <see cref="AdxExpertStrategy"/>.
	/// </summary>
	public AdxExpertStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade volume", "Order volume used for entries", "Risk management")
			
			.SetOptimize(0.1m, 1m, 0.1m);

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX period", "Smoothing length for the ADX indicator", "Indicators")
			
			.SetOptimize(7, 28, 7);

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetGreaterThanZero()
			.SetDisplay("ADX threshold", "Upper ADX limit that allows trades", "Signals")
			
			.SetOptimize(15m, 35m, 5m);

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 20m)
			.SetNotNegative()
			.SetDisplay("Max spread (points)", "Maximum allowed bid-ask spread in points", "Risk management")
			
			.SetOptimize(5m, 40m, 5m);

		_stopLossPoints = Param(nameof(StopLossPoints), 200m)
			.SetNotNegative()
			.SetDisplay("Stop loss (points)", "Protective stop distance in price points", "Risk management")
			
			.SetOptimize(100m, 400m, 50m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 400m)
			.SetNotNegative()
			.SetDisplay("Take profit (points)", "Target distance in price points", "Risk management")
			
			.SetOptimize(200m, 600m, 100m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Type of candles used for ADX", "General");
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

		_previousPlusDi = 0m;
		_previousMinusDi = 0m;
		_hasPreviousDi = false;
		_entryPrice = 0m;
	}

	private decimal _entryPrice;

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

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

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var adxResult = _adx.Process(candle);
		if (adxResult.IsEmpty || !_adx.IsFormed)
			return;

		if (adxResult is not AverageDirectionalIndexValue adxData)
			return;

		var plusDi = adxData.Dx.Plus ?? 0m;
		var minusDi = adxData.Dx.Minus ?? 0m;

		if (adxData.MovingAverage is not decimal currentAdx)
		{
			_previousPlusDi = plusDi;
			_previousMinusDi = minusDi;
			_hasPreviousDi = true;
			return;
		}

		if (!_hasPreviousDi)
		{
			_previousPlusDi = plusDi;
			_previousMinusDi = minusDi;
			_hasPreviousDi = true;
			return;
		}

		// Manage open position SL/TP
		if (Position != 0)
		{
			var step = Security?.PriceStep ?? 1m;
			if (Position > 0)
			{
				if (StopLossPoints > 0m && candle.LowPrice <= _entryPrice - StopLossPoints * step)
				{
					SellMarket(Position);
					goto updateDi;
				}
				if (TakeProfitPoints > 0m && candle.HighPrice >= _entryPrice + TakeProfitPoints * step)
				{
					SellMarket(Position);
					goto updateDi;
				}
			}
			else
			{
				var vol = Math.Abs(Position);
				if (StopLossPoints > 0m && candle.HighPrice >= _entryPrice + StopLossPoints * step)
				{
					BuyMarket(vol);
					goto updateDi;
				}
				if (TakeProfitPoints > 0m && candle.LowPrice <= _entryPrice - TakeProfitPoints * step)
				{
					BuyMarket(vol);
					goto updateDi;
				}
			}
		}

		var bullishCross = _previousPlusDi <= _previousMinusDi && plusDi > minusDi;
		var bearishCross = _previousPlusDi >= _previousMinusDi && plusDi < minusDi;

		if (currentAdx < AdxThreshold && Position == 0)
		{
			if (bullishCross)
			{
				BuyMarket(TradeVolume);
				_entryPrice = candle.ClosePrice;
			}
			else if (bearishCross)
			{
				SellMarket(TradeVolume);
				_entryPrice = candle.ClosePrice;
			}
		}

		updateDi:
		_previousPlusDi = plusDi;
		_previousMinusDi = minusDi;
	}
}