namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class FlexiMaVarianceTrackerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _stdLength;
	private readonly StrategyParam<decimal> _stdMultiplier;
	private readonly StrategyParam<int> _stAtrPeriod;
	private readonly StrategyParam<decimal> _stMultiplier;
private readonly StrategyParam<Sides?> _direction;

	private SimpleMovingAverage _ma;
	private SimpleMovingAverage _diffAvg;
	private StandardDeviation _stdDev;
	private SuperTrend _superTrend;

public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public int StdLength { get => _stdLength.Value; set => _stdLength.Value = value; }
	public decimal StdMultiplier { get => _stdMultiplier.Value; set => _stdMultiplier.Value = value; }
	public int StAtrPeriod { get => _stAtrPeriod.Value; set => _stAtrPeriod.Value = value; }
	public decimal StMultiplier { get => _stMultiplier.Value; set => _stMultiplier.Value = value; }
public Sides? Direction { get => _direction.Value; set => _direction.Value = value; }

	public FlexiMaVarianceTrackerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_maLength = Param(nameof(MaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Length for base SMA", "FlexiMA");

		_stdLength = Param(nameof(StdLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Length", "Length for deviation StdDev", "Variance");

		_stdMultiplier = Param(nameof(StdMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Mult", "Deviation multiplier", "Variance");

		_stAtrPeriod = Param(nameof(StAtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for SuperTrend", "SuperTrend");

		_stMultiplier = Param(nameof(StMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Mult", "ATR multiplier for SuperTrend", "SuperTrend");

_direction = Param(nameof(Direction), (Sides?)null)
.SetDisplay("Trade Direction", "Allowed trading direction", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = new SimpleMovingAverage { Length = MaLength };
		_diffAvg = new SimpleMovingAverage { Length = StdLength };
		_stdDev = new StandardDeviation { Length = StdLength };
		_superTrend = new SuperTrend { Length = StAtrPeriod, Multiplier = StMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_ma, _superTrend, ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue stValue)
	{
		if (!maValue.IsFinal || !stValue.IsFinal || candle.State != CandleStates.Finished)
			return;

		var ma = maValue.GetValue<decimal>();
		var st = (SuperTrendIndicatorValue)stValue;

		var diff = candle.ClosePrice - ma;

		var diffAvg = _diffAvg.Process(diff).GetValue<decimal>();
		var std = _stdDev.Process(diff).GetValue<decimal>();
		var threshold = diffAvg + StdMultiplier * std;

var allowLong = Direction is null or Sides.Buy;
var allowShort = Direction is null or Sides.Sell;

		var longCond = st.IsUpTrend && diff > threshold;
		var shortCond = st.IsDownTrend && diff < -threshold;

		if (allowLong && longCond && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (allowShort && shortCond && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		if (Position > 0 && (st.IsDownTrend || diff < 0))
			SellMarket();

		if (Position < 0 && (st.IsUpTrend || diff > 0))
			BuyMarket();
	}
}
