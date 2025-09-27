namespace StockSharp.Samples.Strategies;

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

public class AdxSystemDiCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;

	private AverageDirectionalIndex _adx;

	private decimal? _prevAdx;
	private decimal? _prevPrevAdx;
	private decimal? _prevPlusDi;
	private decimal? _prevPrevPlusDi;
	private decimal? _prevMinusDi;
	private decimal? _prevPrevMinusDi;

	private decimal? _trailingStopPrice;

public AdxSystemDiCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "General");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX period", "Number of candles used to build the Average Directional Index.", "Indicator")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade volume", "Default volume for market orders.", "Trading")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
			.SetNotNegative()
			.SetDisplay("Take profit (points)", "Target distance expressed in price points.", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 30m)
			.SetNotNegative()
			.SetDisplay("Stop loss (points)", "Protective stop distance expressed in price points.", "Risk")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Trailing stop (points)", "Trailing stop distance expressed in price points.", "Risk")
			.SetCanOptimize(true);
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume; // Align default volume with the configured lot size

		_adx = new AverageDirectionalIndex
		{
			Length = AdxPeriod
		};

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return; // Work only with completed candles as in the MetaTrader expert

		if (_adx == null)
			return;

		if (!adxValue.IsFinal || adxValue is not AverageDirectionalIndexValue adxData)
			return; // Wait for the ADX indicator to deliver a final value

		if (adxData.MovingAverage is not decimal currentAdx)
			return; // Indicator has not accumulated enough data yet

		var dx = adxData.Dx;
		if (dx.Plus is not decimal currentPlus || dx.Minus is not decimal currentMinus)
			return; // Require both +DI and -DI components before trading

		var previousAdx = _prevAdx;
		var previousPlus = _prevPlusDi;
		var previousMinus = _prevMinusDi;
		var earlierAdx = _prevPrevAdx;
		var earlierPlus = _prevPrevPlusDi;
		var earlierMinus = _prevPrevMinusDi;

		if (previousAdx is null || previousPlus is null || previousMinus is null || earlierAdx is null || earlierPlus is null || earlierMinus is null)
		{
			StoreCurrentValues(currentAdx, currentPlus, currentMinus);
			return; // Need two completed candles to replicate the MetaTrader shift logic
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			StoreCurrentValues(currentAdx, currentPlus, currentMinus);
			return; // Strategy is not ready to trade yet
		}

		var adxPrev = previousAdx.Value; // ADX value on the last closed candle (shift = 1)
		var adxEarlier = earlierAdx.Value; // ADX value on the candle before that (shift = 2)
		var plusPrev = previousPlus.Value; // +DI on the last closed candle
		var plusEarlier = earlierPlus.Value; // +DI on the candle before that
		var minusPrev = previousMinus.Value; // -DI on the last closed candle
		var minusEarlier = earlierMinus.Value; // -DI on the candle before that

		var volume = TradeVolume;
		if (volume <= 0m)
		{
			StoreCurrentValues(currentAdx, currentPlus, currentMinus);
			return; // Do not trade when the configured lot size is not positive
		}

		var takeOffset = GetPriceOffset(TakeProfitPoints);
		var stopOffset = GetPriceOffset(StopLossPoints);
		var trailOffset = GetPriceOffset(TrailingStopPoints);

		if (Position > 0m)
		{
			var entryPrice = PositionPrice;

			if (adxEarlier > adxPrev && plusEarlier > adxEarlier && plusPrev < adxPrev)
			{
				SellMarket(Position);
				ResetTrailing();
				StoreCurrentValues(currentAdx, currentPlus, currentMinus);
				return; // Close long position when the ADX reversal pattern appears
			}

			if (takeOffset > 0m && entryPrice != 0m && candle.ClosePrice - entryPrice >= takeOffset)
			{
				SellMarket(Position);
				ResetTrailing();
				StoreCurrentValues(currentAdx, currentPlus, currentMinus);
				return; // Lock in profits once the target distance is reached
			}

			if (stopOffset > 0m && entryPrice != 0m && entryPrice - candle.ClosePrice >= stopOffset)
			{
				SellMarket(Position);
				ResetTrailing();
				StoreCurrentValues(currentAdx, currentPlus, currentMinus);
				return; // Exit the long position when the stop loss is triggered
			}

			if (trailOffset > 0m && entryPrice != 0m)
			{
				var profit = candle.ClosePrice - entryPrice;
				if (profit > trailOffset)
				{
					var candidate = candle.ClosePrice - trailOffset;
					if (_trailingStopPrice is null || candidate > _trailingStopPrice)
						_trailingStopPrice = candidate; // Update the trailing level only in the direction of profit

					if (_trailingStopPrice is decimal stopPrice && candle.ClosePrice <= stopPrice)
					{
						SellMarket(Position);
						ResetTrailing();
						StoreCurrentValues(currentAdx, currentPlus, currentMinus);
						return; // Trailing stop closed the long position
					}
				}
			}
		}
		else if (Position < 0m)
		{
			var entryPrice = PositionPrice;

			if (adxEarlier > adxPrev && minusEarlier > adxEarlier && minusPrev < adxPrev)
			{
				BuyMarket(Math.Abs(Position));
				ResetTrailing();
				StoreCurrentValues(currentAdx, currentPlus, currentMinus);
				return; // Close short position when the ADX reversal pattern appears
			}

			if (takeOffset > 0m && entryPrice != 0m && entryPrice - candle.ClosePrice >= takeOffset)
			{
				BuyMarket(Math.Abs(Position));
				ResetTrailing();
				StoreCurrentValues(currentAdx, currentPlus, currentMinus);
				return; // Take profit for the short position
			}

			if (stopOffset > 0m && entryPrice != 0m && candle.ClosePrice - entryPrice >= stopOffset)
			{
				BuyMarket(Math.Abs(Position));
				ResetTrailing();
				StoreCurrentValues(currentAdx, currentPlus, currentMinus);
				return; // Stop loss for the short position
			}

			if (trailOffset > 0m && entryPrice != 0m)
			{
				var profit = entryPrice - candle.ClosePrice;
				if (profit > trailOffset)
				{
					var candidate = candle.ClosePrice + trailOffset;
					if (_trailingStopPrice is null || candidate < _trailingStopPrice)
						_trailingStopPrice = candidate; // Move the trailing stop closer to the market

					if (_trailingStopPrice is decimal stopPrice && candle.ClosePrice >= stopPrice)
					{
						BuyMarket(Math.Abs(Position));
						ResetTrailing();
						StoreCurrentValues(currentAdx, currentPlus, currentMinus);
						return; // Trailing stop closed the short position
					}
				}
			}
		}
		else
		{
			var longCondition = adxEarlier < adxPrev && plusEarlier < adxEarlier && plusPrev > adxPrev;
			if (longCondition)
			{
				BuyMarket(volume);
				ResetTrailing();
				StoreCurrentValues(currentAdx, currentPlus, currentMinus);
				return; // Open a new long position when +DI crosses above ADX
			}

			var shortCondition = adxEarlier < adxPrev && minusEarlier < adxEarlier && minusPrev > adxPrev;
			if (shortCondition)
			{
				SellMarket(volume);
				ResetTrailing();
				StoreCurrentValues(currentAdx, currentPlus, currentMinus);
				return; // Open a new short position when -DI crosses above ADX
			}
		}

		StoreCurrentValues(currentAdx, currentPlus, currentMinus);
	}

	private decimal GetPriceOffset(decimal points)
	{
		if (points <= 0m)
			return 0m;

		var step = Security?.PriceStep;
		if (step is decimal priceStep && priceStep > 0m)
			return points * priceStep; // Convert MetaTrader points to actual price distance

		return points; // Fall back to raw price units when the instrument step is unknown
	}

	private void StoreCurrentValues(decimal adx, decimal plusDi, decimal minusDi)
	{
		_prevPrevAdx = _prevAdx;
		_prevPrevPlusDi = _prevPlusDi;
		_prevPrevMinusDi = _prevMinusDi;

		_prevAdx = adx;
		_prevPlusDi = plusDi;
		_prevMinusDi = minusDi;
	}

	private void ResetState()
	{
		_prevAdx = null;
		_prevPrevAdx = null;
		_prevPlusDi = null;
		_prevPrevPlusDi = null;
		_prevMinusDi = null;
		_prevPrevMinusDi = null;
		_trailingStopPrice = null;
	}

	private void ResetTrailing()
	{
		_trailingStopPrice = null; // Clear trailing information whenever the position direction changes
	}

	/// <inheritdoc />
	protected override void OnPositionChanged()
	{
		base.OnPositionChanged();

		ResetTrailing();
	}
}
