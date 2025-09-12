using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend and CCI based scalping strategy.
/// </summary>
public class SupertrendCciScalpStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength1;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<int> _atrLength2;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<decimal> _cciLevel;
	private readonly StrategyParam<DataType> _candleType;

	private bool _isLong;
	private bool _isShort;

	/// <summary>
	/// ATR length for first Supertrend.
	/// </summary>
	public int AtrLength1 { get => _atrLength1.Value; set => _atrLength1.Value = value; }

	/// <summary>
	/// ATR multiplier for first Supertrend.
	/// </summary>
	public decimal Factor1 { get => _factor1.Value; set => _factor1.Value = value; }

	/// <summary>
	/// ATR length for second Supertrend.
	/// </summary>
	public int AtrLength2 { get => _atrLength2.Value; set => _atrLength2.Value = value; }

	/// <summary>
	/// ATR multiplier for second Supertrend.
	/// </summary>
	public decimal Factor2 { get => _factor2.Value; set => _factor2.Value = value; }

	/// <summary>
	/// CCI calculation length.
	/// </summary>
	public int CciLength { get => _cciLength.Value; set => _cciLength.Value = value; }

	/// <summary>
	/// Moving average length for smoothed CCI.
	/// </summary>
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }

	/// <summary>
	/// Moving average type for smoothed CCI.
	/// </summary>
	public MovingAverageTypeEnum MaType { get => _maType.Value; set => _maType.Value = value; }

	/// <summary>
	/// CCI level for signals.
	/// </summary>
	public decimal CciLevel { get => _cciLevel.Value; set => _cciLevel.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SupertrendCciScalpStrategy"/>.
	/// </summary>
	public SupertrendCciScalpStrategy()
	{
	_atrLength1 = Param(nameof(AtrLength1), 14)
	    .SetDisplay("ATR Length 1", "ATR length for first Supertrend", "Supertrend");

	_factor1 = Param(nameof(Factor1), 3m)
	    .SetDisplay("Factor 1", "ATR multiplier for first Supertrend", "Supertrend");

	_atrLength2 = Param(nameof(AtrLength2), 14)
	    .SetDisplay("ATR Length 2", "ATR length for second Supertrend", "Supertrend");

	_factor2 = Param(nameof(Factor2), 6m)
	    .SetDisplay("Factor 2", "ATR multiplier for second Supertrend", "Supertrend");

	_cciLength = Param(nameof(CciLength), 20)
	    .SetDisplay("CCI Length", "CCI calculation length", "CCI");

	_smoothingLength = Param(nameof(SmoothingLength), 5)
	    .SetDisplay("Smoothing Length", "Moving average length for CCI", "CCI");

	_maType = Param(nameof(MaType), MovingAverageTypeEnum.Simple)
	    .SetDisplay("MA Type", "Type of moving average", "CCI");

	_cciLevel = Param(nameof(CciLevel), 100m)
	    .SetDisplay("CCI Level", "Threshold for entries and exits", "CCI");

	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
	    .SetDisplay("Candle Type", "Type of candles to use", "General");
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
	_isLong = false;
	_isShort = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	var st1 = new SuperTrend { Length = AtrLength1, Multiplier = Factor1 };
	var st2 = new SuperTrend { Length = AtrLength2, Multiplier = Factor2 };
	var cci = new CommodityChannelIndex { Length = CciLength };
	var cciMa = CreateMa(MaType, SmoothingLength);
	cci.Bind(cciMa);

	var subscription = SubscribeCandles(CandleType);
	subscription
	    .BindEx(st1, st2, cci, cciMa, ProcessCandle)
	    .Start();

	var area = CreateChartArea();
	if (area != null)
	{
	    DrawCandles(area, subscription);
	    DrawIndicator(area, st1);
	    DrawIndicator(area, st2);
	    DrawIndicator(area, cci);
	    DrawIndicator(area, cciMa);
	    DrawOwnTrades(area);
	}

	StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue st1Value, IIndicatorValue st2Value, IIndicatorValue cciValue, IIndicatorValue maValue)
	{
	if (candle.State != CandleStates.Finished)
	    return;

	if (!IsFormedAndOnlineAndAllowTrading())
	    return;

	if (!st1Value.IsFinal || !st2Value.IsFinal || !cciValue.IsFinal || !maValue.IsFinal)
	    return;

	var st1 = ((SuperTrendIndicatorValue)st1Value).Value;
	var st2 = ((SuperTrendIndicatorValue)st2Value).Value;
	var cci = cciValue.ToDecimal();
	var smoothing = maValue.ToDecimal();

	var longCondition = st1 > candle.ClosePrice && st2 < candle.ClosePrice && smoothing < -CciLevel;
	var shortCondition = st1 < candle.ClosePrice && st2 > candle.ClosePrice && smoothing > CciLevel;

	if (longCondition && Position <= 0)
	{
	    BuyMarket(Volume + Math.Abs(Position));
	    _isLong = true;
	    _isShort = false;
	}
	else if (shortCondition && Position >= 0)
	{
	    SellMarket(Volume + Math.Abs(Position));
	    _isShort = true;
	    _isLong = false;
	}

	if (_isLong && (st1 < candle.ClosePrice || st2 > candle.ClosePrice || cci > CciLevel))
	{
	    SellMarket(Math.Abs(Position));
	    _isLong = false;
	}
	else if (_isShort && (st1 > candle.ClosePrice || st2 < candle.ClosePrice || cci < -CciLevel))
	{
	    BuyMarket(Math.Abs(Position));
	    _isShort = false;
	}
	}

	private static IIndicator CreateMa(MovingAverageTypeEnum type, int length)
	{
	return type switch
	{
	    MovingAverageTypeEnum.Simple => new SimpleMovingAverage { Length = length },
	    MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
	    MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage { Length = length },
	    MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
	    MovingAverageTypeEnum.VolumeWeighted => new VolumeWeightedMovingAverage { Length = length },
	    _ => new SimpleMovingAverage { Length = length },
	};
	}

	public enum MovingAverageTypeEnum
	{
	Simple,
	Exponential,
	Smoothed,
	Weighted,
	VolumeWeighted
	}
}

