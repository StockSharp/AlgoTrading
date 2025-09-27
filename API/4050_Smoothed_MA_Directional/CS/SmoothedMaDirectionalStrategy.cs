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

public class SmoothedMaDirectionalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _movingAverage;

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SmoothedMaDirectionalStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetDisplay("Stop Loss Points", "Protective stop distance in price steps", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetOptimize(10m, 300m, 10m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
			.SetDisplay("Take Profit Points", "Profit target distance in price steps", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetOptimize(10m, 300m, 10m);

		_maPeriod = Param(nameof(MaPeriod), 12)
			.SetDisplay("MA Period", "Number of bars for the smoothed moving average", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(5, 100, 5);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Order size used for market entries", "Trading")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for price analysis", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		// Drop the cached indicator reference to rebuild it on the next start
		_movingAverage = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Align the strategy volume with the configured trade size
		Volume = TradeVolume;

		// Create the smoothed moving average used to detect the trend direction
		_movingAverage = new SmoothedMovingAverage
		{
			Length = MaPeriod
		};

		// Subscribe to candle updates and bind the moving average indicator
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_movingAverage, ProcessCandle)
			.Start();

		// Draw the price series, indicator and trades if a chart area is available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _movingAverage);
			DrawOwnTrades(area);
		}

		// Configure stop-loss and take-profit protection to emulate MQL SL/TP behaviour
		ConfigureRiskProtection();
	}

	private void ConfigureRiskProtection()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			priceStep = 1m;
		}

		var stopLossDistance = StopLossPoints * priceStep;
		var takeProfitDistance = TakeProfitPoints * priceStep;

		if (stopLossDistance <= 0m && takeProfitDistance <= 0m)
		{
			return;
		}

		StartProtection(
			stopLoss: stopLossDistance > 0m ? new Unit(stopLossDistance, UnitTypes.Absolute) : null,
			takeProfit: takeProfitDistance > 0m ? new Unit(takeProfitDistance, UnitTypes.Absolute) : null,
			useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		// Only work with completed candles to replicate the MQL behaviour
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		// Ensure the indicator exists and has enough data points
		if (_movingAverage == null || !_movingAverage.IsFormed)
		{
			return;
		}

		// Keep the indicator length in sync with the current parameter value
		if (_movingAverage.Length != MaPeriod)
		{
			_movingAverage.Length = MaPeriod;
		}

		// Skip trading until the strategy is ready and trading is allowed
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		// Refresh the working volume so on-the-fly parameter changes take effect
		Volume = TradeVolume;

		// Avoid sending orders when the trade volume is not positive
		if (Volume <= 0m)
		{
			return;
		}

		var closePrice = candle.ClosePrice;

		// Align the position with the price relative to the moving average
		if (closePrice > maValue && Position <= 0m)
		{
			// Price is above the moving average - go long or flip from short to long
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (closePrice < maValue && Position >= 0m)
		{
			// Price is below the moving average - go short or flip from long to short
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
