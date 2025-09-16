using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining EMA crossover, Parabolic SAR, and Bulls/Bears Power indicators.
/// Trades during specified hours when all signals align.
/// </summary>
public class EmaSarBullsBearsStrategy : Strategy
{
	private readonly StrategyParam<int> _shortEmaPeriod;
	private readonly StrategyParam<int> _longEmaPeriod;
	private readonly StrategyParam<int> _bearsBullsPeriod;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaxStep;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Unit> _takeValue;
	private readonly StrategyParam<Unit> _stopValue;

	private ExponentialMovingAverage _shortEma;
	private ExponentialMovingAverage _longEma;
	private ParabolicSar _sar;
	private BearsPower _bearsPower;
	private BullsPower _bullsPower;

	private decimal? _prevBearsPower;
	private decimal? _prevBullsPower;

	/// <summary>
	/// Short EMA period.
	/// </summary>
	public int ShortEmaPeriod
	{
		get => _shortEmaPeriod.Value;
		set => _shortEmaPeriod.Value = value;
	}

/// <summary>
/// Long EMA period.
/// </summary>
public int LongEmaPeriod
{
	get => _longEmaPeriod.Value;
	set => _longEmaPeriod.Value = value;
}

/// <summary>
/// Bulls and Bears Power period.
/// </summary>
public int BearsBullsPeriod
{
get => _bearsBullsPeriod.Value;
set => _bearsBullsPeriod.Value = value;
}

/// <summary>
/// SAR acceleration step.
/// </summary>
public decimal SarStep
{
get => _sarStep.Value;
set => _sarStep.Value = value;
}

/// <summary>
/// SAR maximum acceleration.
/// </summary>
public decimal SarMaxStep
{
get => _sarMaxStep.Value;
set => _sarMaxStep.Value = value;
}

/// <summary>
/// Trading session start hour.
/// </summary>
public int StartHour
{
get => _startHour.Value;
set => _startHour.Value = value;
}

/// <summary>
/// Trading session end hour.
/// </summary>
public int EndHour
{
get => _endHour.Value;
set => _endHour.Value = value;
}

/// <summary>
/// Candle type parameter.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Take profit value.
/// </summary>
public Unit TakeValue
{
get => _takeValue.Value;
set => _takeValue.Value = value;
}

/// <summary>
/// Stop loss value.
/// </summary>
public Unit StopValue
{
get => _stopValue.Value;
set => _stopValue.Value = value;
}

/// <summary>
/// Initializes a new instance of the <see cref="EmaSarBullsBearsStrategy"/> class.
/// </summary>
public EmaSarBullsBearsStrategy()
{
_shortEmaPeriod = Param(nameof(ShortEmaPeriod), 3)
.SetGreaterThanZero()
.SetDisplay("Short EMA", "Short EMA period", "Indicators");

_longEmaPeriod = Param(nameof(LongEmaPeriod), 34)
.SetGreaterThanZero()
.SetDisplay("Long EMA", "Long EMA period", "Indicators");

_bearsBullsPeriod = Param(nameof(BearsBullsPeriod), 13)
.SetGreaterThanZero()
.SetDisplay("Bulls/Bears Period", "Period for Bulls and Bears Power", "Indicators");

_sarStep = Param(nameof(SarStep), 0.02m)
.SetGreaterThanZero()
.SetDisplay("SAR Step", "Acceleration factor for Parabolic SAR", "Indicators");

_sarMaxStep = Param(nameof(SarMaxStep), 0.2m)
.SetGreaterThanZero()
.SetDisplay("SAR Max Step", "Maximum acceleration for Parabolic SAR", "Indicators");

_startHour = Param(nameof(StartHour), 8)
.SetDisplay("Start Hour", "Trading window start hour", "General");

_endHour = Param(nameof(EndHour), 17)
.SetDisplay("End Hour", "Trading window end hour", "General");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
.SetDisplay("Candle Type", "Candle series type", "General");

_takeValue = Param(nameof(TakeValue), new Unit(400, UnitTypes.Absolute))
.SetDisplay("Take Profit", "Take profit value", "Protection");

_stopValue = Param(nameof(StopValue), new Unit(2000, UnitTypes.Absolute))
.SetDisplay("Stop Loss", "Stop loss value", "Protection");
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

_shortEma = null;
_longEma = null;
_sar = null;
_bearsPower = null;
_bullsPower = null;
_prevBearsPower = null;
_prevBullsPower = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_shortEma = new() { Length = ShortEmaPeriod };
_longEma = new() { Length = LongEmaPeriod };
_sar = new ParabolicSar
{
	AccelerationStep = SarStep,
	AccelerationMax = SarMaxStep
};
_bearsPower = new() { Length = BearsBullsPeriod };
_bullsPower = new() { Length = BearsBullsPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_shortEma, _longEma, _sar, _bearsPower, _bullsPower, ProcessCandle)
			.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _shortEma);
			DrawIndicator(area, _longEma);
			DrawIndicator(area, _sar);
			DrawIndicator(area, _bearsPower);
			DrawIndicator(area, _bullsPower);
			DrawOwnTrades(area);
		}

		StartProtection(TakeValue, StopValue);
}

	private void ProcessCandle(ICandleMessage candle, decimal shortEma, decimal longEma, decimal sarValue, decimal bearsPower, decimal bullsPower)
		{
		if (candle.State != CandleStates.Finished)
		return;
		
		var hour = candle.OpenTime.Hour;
			if (hour <= StartHour || hour >= EndHour)
			{
				_prevBearsPower = bearsPower;
				_prevBullsPower = bullsPower;
				return;
		}
		
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		bool shortSignal = shortEma < longEma && sarValue > candle.HighPrice && bearsPower < 0m &&
		_prevBearsPower is decimal prevBears && bearsPower > prevBears;
		
		bool longSignal = shortEma > longEma && sarValue < candle.LowPrice && bullsPower > 0m &&
		_prevBullsPower is decimal prevBulls && bullsPower < prevBulls;
		
		if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			LogInfo($"Short Entry: EMA {shortEma:F2} < {longEma:F2}, SAR {sarValue:F2} > High {candle.HighPrice:F2}, Bears {bearsPower:F2} rising");
		}
		else if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo($"Long Entry: EMA {shortEma:F2} > {longEma:F2}, SAR {sarValue:F2} < Low {candle.LowPrice:F2}, Bulls {bullsPower:F2} falling");
		}
		
		_prevBearsPower = bearsPower;
		_prevBullsPower = bullsPower;
		}
		}

