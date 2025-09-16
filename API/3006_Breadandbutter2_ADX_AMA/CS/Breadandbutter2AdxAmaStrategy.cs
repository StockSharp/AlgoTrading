namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Bread and Butter 2 strategy that combines KAMA trend direction with ADX slope confirmation.
/// </summary>
public class Breadandbutter2AdxAmaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _amaPeriod;
	private readonly StrategyParam<int> _amaFastPeriod;
	private readonly StrategyParam<int> _amaSlowPeriod;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;

	private KaufmanAdaptiveMovingAverage _ama = null!;
	private AverageDirectionalIndex _adx = null!;

	private bool _hasPrevious;
	private decimal _previousAma;
	private decimal _previousAdx;
	private decimal _pipSize;

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// ADX averaging period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Base period for Kaufman AMA smoothing.
	/// </summary>
	public int AmaPeriod
	{
		get => _amaPeriod.Value;
		set => _amaPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period within Kaufman AMA.
	/// </summary>
	public int AmaFastPeriod
	{
		get => _amaFastPeriod.Value;
		set => _amaFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period within Kaufman AMA.
	/// </summary>
	public int AmaSlowPeriod
	{
		get => _amaSlowPeriod.Value;
		set => _amaSlowPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss size expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit size expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="Breadandbutter2AdxAmaStrategy"/> parameters.
	/// </summary>
	public Breadandbutter2AdxAmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle type", "General");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Averaging period for ADX", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_amaPeriod = Param(nameof(AmaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("AMA Period", "Base smoothing length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_amaFastPeriod = Param(nameof(AmaFastPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("AMA Fast Period", "Fast EMA length for AMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_amaSlowPeriod = Param(nameof(AmaSlowPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("AMA Slow Period", "Slow EMA length for AMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk");
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

		_hasPrevious = false;
		_previousAma = 0m;
		_previousAdx = 0m;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Pre-compute pip size according to the active security.
		_pipSize = CalculatePipSize();

		_ama = new KaufmanAdaptiveMovingAverage
		{
			Length = AmaPeriod,
			FastSCPeriod = AmaFastPeriod,
			SlowSCPeriod = AmaSlowPeriod
		};

		_adx = new AverageDirectionalIndex
		{
			Length = AdxPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ama, _adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ama);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;

		if (step <= 0m)
			return 1m;

		var value = step;
		var decimals = 0;

		// Count decimal places to mimic the MetaTrader pip adjustment for 3 or 5 digits.
		while (value < 1m && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals is 3 or 5 ? step * 10m : step;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue amaValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!amaValue.IsFinal || !adxValue.IsFinal)
			return;

		var ama = amaValue.ToDecimal();
		var adxData = (AverageDirectionalIndexValue)adxValue;

		if (adxData.MovingAverage is not decimal adx)
			return;

		if (!_hasPrevious)
		{
			_previousAma = ama;
			_previousAdx = adx;
			_hasPrevious = true;
			return;
		}

		var goLong = adx < _previousAdx && ama > _previousAma;
		var goShort = adx > _previousAdx && ama < _previousAma;

		// Enter in the signal direction and flip existing exposure if required.
		if (goLong && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
				BuyMarket(volume);
		}
		else if (goShort && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
				SellMarket(volume);
		}

		ManageRisk(candle);

		_previousAma = ama;
		_previousAdx = adx;
	}

	private void ManageRisk(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var entry = PositionPrice;
			if (entry != null)
			{
				var stopPrice = entry.Value - StopLossPips * _pipSize;
				var takePrice = entry.Value + TakeProfitPips * _pipSize;

				// Exit long position whenever the candle pierces stop-loss or take-profit levels.
				if (StopLossPips > 0m && candle.LowPrice <= stopPrice)
				{
					SellMarket(Position);
				}
				else if (TakeProfitPips > 0m && candle.HighPrice >= takePrice)
				{
					SellMarket(Position);
				}
			}
		}
		else if (Position < 0)
		{
			var entry = PositionPrice;
			if (entry != null)
			{
				var stopPrice = entry.Value + StopLossPips * _pipSize;
				var takePrice = entry.Value - TakeProfitPips * _pipSize;
				var volume = Math.Abs(Position);

				// Exit short position whenever the candle pierces stop-loss or take-profit levels.
				if (StopLossPips > 0m && candle.HighPrice >= stopPrice)
				{
					BuyMarket(volume);
				}
				else if (TakeProfitPips > 0m && candle.LowPrice <= takePrice)
				{
					BuyMarket(volume);
				}
			}
		}
	}
}
