using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy for Nifty 50 based on DEMA, VWAP and Bollinger Bands.
/// </summary>
public class Nifty505mintStrategy : Strategy
{
	private readonly StrategyParam<int> _demaPeriod;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerStdDev;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHighest;
	private decimal _prevLowest;

	public Nifty505mintStrategy()
	{
		_demaPeriod = Param(nameof(DemaPeriod), 6).SetDisplay("DEMA Period").SetCanOptimize();
		_bollingerLength = Param(nameof(BollingerLength), 20).SetDisplay("Bollinger Length").SetCanOptimize();
		_bollingerStdDev = Param(nameof(BollingerStdDev), 2m).SetDisplay("Bollinger StdDev").SetCanOptimize();
		_lookbackPeriod = Param(nameof(LookbackPeriod), 5).SetDisplay("Lookback Period").SetCanOptimize();
		_stopLossPoints = Param(nameof(StopLossPoints), 25m).SetDisplay("Stop Loss Points").SetCanOptimize();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type");
	}

	public int DemaPeriod { get => _demaPeriod.Value; set => _demaPeriod.Value = value; }
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }
	public decimal BollingerStdDev { get => _bollingerStdDev.Value; set => _bollingerStdDev.Value = value; }
	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			stopLoss: new Unit(StopLossPoints, UnitTypes.Absolute),
			isStopTrailing: false,
			useMarketOrders: true);

		var bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerStdDev
		};
		var dema = new DoubleExponentialMovingAverage { Length = DemaPeriod };
		var vwap = new VolumeWeightedMovingAverage();
		var highest = new Highest { Length = LookbackPeriod };
		var lowest = new Lowest { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bollinger, dema, vwap, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, dema);
			DrawIndicator(area, vwap);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal demaValue, decimal vwapValue, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var buy = candle.ClosePrice > _prevHighest && candle.ClosePrice > upper && demaValue > vwapValue && Position == 0;
		var sell = candle.ClosePrice < _prevLowest && candle.ClosePrice < lower && demaValue < vwapValue && Position == 0;

		if (buy)
			BuyMarket();
		else if (sell)
			SellMarket();

		_prevHighest = highestValue;
		_prevLowest = lowestValue;
	}
}
