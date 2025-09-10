using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy with optional time and MA filters and flexible stop calculation.
/// </summary>
public class BreakoutsWithTimeFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useMaFilter;
	private readonly StrategyParam<MaTypeEnum> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<StopLossType> _slType;
	private readonly StrategyParam<int> _slLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _pointsStop;
	private readonly StrategyParam<decimal> _riskReward;

	private decimal _stopLevel;
	private decimal _targetLevel;
	private readonly List<ICandleMessage> _history = new();

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public bool UseMaFilter { get => _useMaFilter.Value; set => _useMaFilter.Value = value; }
	public MaTypeEnum MaType { get => _maType.Value; set => _maType.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public bool UseTimeFilter { get => _useTimeFilter.Value; set => _useTimeFilter.Value = value; }
	public TimeSpan StartTime { get => _startTime.Value; set => _startTime.Value = value; }
	public TimeSpan EndTime { get => _endTime.Value; set => _endTime.Value = value; }
	public StopLossType SlType { get => _slType.Value; set => _slType.Value = value; }
	public int SlLength { get => _slLength.Value; set => _slLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal PointsStop { get => _pointsStop.Value; set => _pointsStop.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }

	public BreakoutsWithTimeFilterStrategy()
	{
		_length = Param(nameof(Length), 5)
			.SetDisplay("Length", "Lookback period for breakout levels", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_useMaFilter = Param(nameof(UseMaFilter), false)
			.SetDisplay("Use MA Filter", "Enable moving average filter", "MA Filter");

		_maType = Param(nameof(MaType), MaTypeEnum.Hull)
			.SetDisplay("MA Type", "Moving average type", "MA Filter");

		_maLength = Param(nameof(MaLength), 99)
			.SetDisplay("MA Length", "Moving average period", "MA Filter")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Time Filter", "Enable trading time window", "Time Filter");

		_startTime = Param(nameof(StartTime), new TimeSpan(14, 30, 0))
			.SetDisplay("Start Time", "Start of trading window", "Time Filter");

		_endTime = Param(nameof(EndTime), new TimeSpan(15, 0, 0))
			.SetDisplay("End Time", "End of trading window", "Time Filter");

		_slType = Param(nameof(SlType), StopLossType.Atr)
			.SetDisplay("Stop Loss Type", "Stop loss calculation", "Risk Management");

		_slLength = Param(nameof(SlLength), 0)
			.SetDisplay("Candle SL Length", "Bars back for candle stop", "Risk Management");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR calculation period", "Risk Management");

		_atrMultiplier = Param(nameof(AtrMultiplier), 0.5m)
			.SetDisplay("ATR Multiplier", "Multiplier for ATR stop", "Risk Management");

		_pointsStop = Param(nameof(PointsStop), 50m)
			.SetDisplay("Fixed Points Stop", "Fixed points stop size", "Risk Management");

		_riskReward = Param(nameof(RiskReward), 3m)
			.SetDisplay("Risk Reward", "Risk to reward ratio", "Risk Management");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_stopLevel = 0m;
		_targetLevel = 0m;
		_history.Clear();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var highest = new Highest { Length = Length };
		var lowest = new Lowest { Length = Length };
		var atr = new AverageTrueRange { Length = AtrLength };
		var ma = MaType switch
		{
			MaTypeEnum.Sma => new SimpleMovingAverage { Length = MaLength },
			MaTypeEnum.Ema => new ExponentialMovingAverage { Length = MaLength },
			MaTypeEnum.Wma => new WeightedMovingAverage { Length = MaLength },
			MaTypeEnum.Vwma => new VolumeWeightedMovingAverage { Length = MaLength },
			_ => new HullMovingAverage { Length = MaLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, atr, ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue, decimal atrValue, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_history.Add(candle);
		if (_history.Count > Math.Max(SlLength + 10, Length + 10))
			_history.RemoveAt(0);

		var inTime = !UseTimeFilter || InTimeRange(candle.CloseTime);
		var maLong = !UseMaFilter || candle.ClosePrice > maValue;
		var maShort = !UseMaFilter || candle.ClosePrice < maValue;

		if (Position == 0 && inTime)
		{
			if (candle.ClosePrice > highestValue && maLong)
				EnterLong(candle, atrValue);
			else if (candle.ClosePrice < lowestValue && maShort)
				EnterShort(candle, atrValue);
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopLevel || candle.HighPrice >= _targetLevel)
				ClosePosition();
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopLevel || candle.LowPrice <= _targetLevel)
				ClosePosition();
		}
	}

	private void EnterLong(ICandleMessage candle, decimal atrValue)
	{
		_stopLevel = SlType switch
		{
			StopLossType.Atr => candle.ClosePrice - atrValue * AtrMultiplier,
			StopLossType.Candle => GetCandleStop(false),
			StopLossType.Points => candle.ClosePrice - PointsStop,
			_ => 0m
		};

		var stopDistance = candle.ClosePrice - _stopLevel;
		_targetLevel = candle.ClosePrice + RiskReward * stopDistance;

		BuyMarket();
	}

	private void EnterShort(ICandleMessage candle, decimal atrValue)
	{
		_stopLevel = SlType switch
		{
			StopLossType.Atr => candle.ClosePrice + atrValue * AtrMultiplier,
			StopLossType.Candle => GetCandleStop(true),
			StopLossType.Points => candle.ClosePrice + PointsStop,
			_ => 0m
		};

		var stopDistance = _stopLevel - candle.ClosePrice;
		_targetLevel = candle.ClosePrice - RiskReward * stopDistance;

		SellMarket();
	}

	private decimal GetCandleStop(bool isShort)
	{
		if (_history.Count <= SlLength)
			return isShort ? _history[^1].HighPrice : _history[^1].LowPrice;

		var index = _history.Count - 1 - SlLength;
		var c = _history[index];
		return isShort ? c.HighPrice : c.LowPrice;
	}

	private bool InTimeRange(DateTimeOffset time)
	{
		var t = time.TimeOfDay;
		return StartTime <= EndTime ? t >= StartTime && t <= EndTime : t >= StartTime || t <= EndTime;
	}

	public enum MaTypeEnum
	{
		Sma,
		Ema,
		Wma,
		Vwma,
		Hull
	}

	public enum StopLossType
	{
		Atr,
		Candle,
		Points
	}
}
