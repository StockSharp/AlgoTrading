using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive regime strategy based on ATR, moving averages, Bollinger Bands and ADX.
/// Generates signals from candlestick patterns near support and resistance.
/// </summary>
public class DskyzDafeAdaptiveRegimeQuantMachineProStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<decimal> _maStrengthThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly SimpleMovingAverage _atrAvg = new() { Length = 50 };
	private readonly SimpleMovingAverage _bollWidthAvg = new() { Length = 50 };
	private readonly Highest _highest = new() { Length = 20 };
	private readonly Lowest _lowest = new() { Length = 20 };
	private readonly SimpleMovingAverage _volAvg = new() { Length = 20 };

	private ICandleMessage _prev;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public int FastMaLength { get => _fastMaLength.Value; set => _fastMaLength.Value = value; }
	public int SlowMaLength { get => _slowMaLength.Value; set => _slowMaLength.Value = value; }
	public decimal MaStrengthThreshold { get => _maStrengthThreshold.Value; set => _maStrengthThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DskyzDafeAdaptiveRegimeQuantMachineProStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR period", "ATR")
			.SetCanOptimize();

		_fastMaLength = Param(nameof(FastMaLength), 20)
			.SetDisplay("Fast MA Length", "Fast moving average length", "MA")
			.SetCanOptimize();

		_slowMaLength = Param(nameof(SlowMaLength), 50)
			.SetDisplay("Slow MA Length", "Slow moving average length", "MA")
			.SetCanOptimize();

		_maStrengthThreshold = Param(nameof(MaStrengthThreshold), 0.5m)
			.SetDisplay("MA Strength Threshold", "Trend threshold", "MA")
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prev = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var fastMa = new ExponentialMovingAverage { Length = FastMaLength };
		var slowMa = new ExponentialMovingAverage { Length = SlowMaLength };
		var boll = new BollingerBands { Length = 20, Width = 2m };
		var adx = new AverageDirectionalIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, fastMa, slowMa, boll, adx, ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal fastMa, decimal slowMa, decimal middle, decimal upper, decimal lower, decimal adx)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atrAvg = _atrAvg.Process(atr).GetValue<decimal>();
		var width = (upper - lower) / middle;
		var widthAvg = _bollWidthAvg.Process(width).GetValue<decimal>();
		var volAvg = _volAvg.Process(candle.TotalVolume).GetValue<decimal>();
		var volSpike = candle.TotalVolume > volAvg * 1.5m;
		var recentHigh = _highest.Process(candle.HighPrice).GetValue<decimal>();
		var recentLow = _lowest.Process(candle.LowPrice).GetValue<decimal>();
		var isNearSupport = candle.LowPrice <= recentLow * 1.01m;
		var isNearResistance = candle.HighPrice >= recentHigh * 0.99m;

		var trendDir = 0;
		if (fastMa > slowMa + atr * MaStrengthThreshold)
			trendDir = 1;
		else if (fastMa < slowMa - atr * MaStrengthThreshold)
			trendDir = -1;

		var priceRange = recentHigh - recentLow;
		var rangeRatio = priceRange / candle.ClosePrice;

		var isTrending = adx > 20 && Math.Abs(fastMa - slowMa) > atr * 0.3m;
		var isRange = adx < 25 && rangeRatio < 0.03m;
		var isVolatile = width > widthAvg * 1.5m && atr > atrAvg * 1.2m;
		var isQuiet = width < widthAvg * 0.8m && atr < atrAvg * 0.9m;
		var regime = isTrending ? 1 : isRange ? 2 : isVolatile ? 3 : isQuiet ? 4 : 5;

		var body = Math.Abs(candle.OpenPrice - candle.ClosePrice);
		var full = candle.HighPrice - candle.LowPrice;
		var lowerPart = full == 0 ? 0 : (candle.ClosePrice - candle.LowPrice) / full;
		var upperPart = full == 0 ? 0 : (candle.HighPrice - candle.ClosePrice) / full;

		var bullishEngulfing = _prev != null &&
			_prev.ClosePrice < _prev.OpenPrice &&
			candle.ClosePrice > candle.OpenPrice &&
			candle.ClosePrice > _prev.OpenPrice &&
			candle.OpenPrice < _prev.ClosePrice &&
			isNearSupport && volSpike;

		var bearishEngulfing = _prev != null &&
			_prev.ClosePrice > _prev.OpenPrice &&
			candle.ClosePrice < candle.OpenPrice &&
			candle.ClosePrice < _prev.OpenPrice &&
			candle.OpenPrice > _prev.ClosePrice &&
			isNearResistance && volSpike;

		var hammer = full > 3 * body && lowerPart > 0.6m && isNearSupport && volSpike;
		var shootingStar = full > 3 * body && upperPart > 0.6m && isNearResistance && volSpike;

		var bullSignal = (bullishEngulfing ? 0.5m : 0m) + (hammer ? (regime == 2 ? 0.4m : 0.2m) : 0m);
		var bearSignal = (bearishEngulfing ? 0.5m : 0m) + (shootingStar ? (regime == 2 ? 0.4m : 0.2m) : 0m);

		var longCondition = bullSignal > bearSignal && trendDir >= 0;
		var shortCondition = bearSignal > bullSignal && trendDir <= 0;

		if (longCondition && Position <= 0)
			BuyMarket();
		if (shortCondition && Position >= 0)
			SellMarket();

		_prev = candle;
	}
}
