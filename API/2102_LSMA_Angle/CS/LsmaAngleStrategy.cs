using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// LSMA Angle based strategy.
/// Opens long when the LSMA angle rises above a threshold and short when it falls below a negative threshold.
/// Positions are closed when the angle returns to the neutral zone.
/// </summary>
public class LsmaAngleStrategy : Strategy
{
	private readonly StrategyParam<int> _lsmaPeriod;
	private readonly StrategyParam<decimal> _angleThreshold;
	private readonly StrategyParam<int> _startShift;
	private readonly StrategyParam<int> _endShift;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevAngle;
	private decimal _multiplier;
	private Shift _shift;

	public int LsmaPeriod
	{
	get => _lsmaPeriod.Value;
	set => _lsmaPeriod.Value = value;
	}

	public decimal AngleThreshold
	{
	get => _angleThreshold.Value;
	set => _angleThreshold.Value = value;
	}

	public int StartShift
	{
	get => _startShift.Value;
	set => _startShift.Value = value;
	}

	public int EndShift
	{
	get => _endShift.Value;
	set => _endShift.Value = value;
	}

	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	public LsmaAngleStrategy()
	{
	_lsmaPeriod = Param(nameof(LsmaPeriod), 25)
		.SetGreaterThanZero()
		.SetDisplay("LSMA Period", "LSMA calculation length", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);

	_angleThreshold = Param(nameof(AngleThreshold), 15m)
		.SetGreaterThanZero()
		.SetDisplay("Angle Threshold", "Threshold for LSMA angle", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5m, 30m, 5m);

	_startShift = Param(nameof(StartShift), 4)
		.SetDisplay("Start Shift", "Bar shift for angle start", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(2, 8, 1);

	_endShift = Param(nameof(EndShift), 0)
		.SetDisplay("End Shift", "Bar shift for angle end", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(0, 4, 1);

	_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
	base.OnReseted();
	_prevAngle = 0m;
	_shift = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	if (StartShift <= EndShift)
		throw new InvalidOperationException("StartShift must be greater than EndShift.");

	var shiftLength = StartShift - EndShift;

	var lsma = new LinearRegression { Length = LsmaPeriod };
	_shift = new Shift { Length = shiftLength };

	_multiplier = Security.Code?.Contains("JPY") == true ? 1000m : 100000m;

	var subscription = SubscribeCandles(CandleType);

	subscription
		.Bind(lsma, (candle, lsmaValue) =>
		{
		if (candle.State != CandleStates.Finished)
			return;

		var shifted = _shift.Process(lsmaValue);

		if (!shifted.IsFinal || shifted is not DecimalIndicatorValue dv)
			return;

		var pastLsma = dv.Value;
		var angle = ((lsmaValue - pastLsma) * _multiplier) / shiftLength;

		var wasUp = _prevAngle > AngleThreshold;
		var wasDown = _prevAngle < -AngleThreshold;

		if (!wasUp && angle > AngleThreshold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (wasUp && angle <= AngleThreshold && Position > 0)
		{
			SellMarket(Position);
		}

		if (!wasDown && angle < -AngleThreshold && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (wasDown && angle >= -AngleThreshold && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		_prevAngle = angle;
		})
		.Start();

	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		DrawIndicator(area, lsma);
		DrawOwnTrades(area);
	}
	}
}
