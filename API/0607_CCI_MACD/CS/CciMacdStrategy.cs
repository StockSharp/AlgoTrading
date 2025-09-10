using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI with MACD filter strategy using EMA and ATR bands.
/// </summary>
public class CciMacdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<bool> _useMacdFilter;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<int> _ema125Period;
	private readonly StrategyParam<int> _ema750Period;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<bool> _enableTimeFilter;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;

	private CommodityChannelIndex _cci = null!;
	private MovingAverageConvergenceDivergence _macd = null!;
	private ExponentialMovingAverage _ema125 = null!;
	private ExponentialMovingAverage _ema750 = null!;
	private AverageTrueRange _atr = null!;
	private decimal _prevCci;
	private bool _initialized;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// CCI length.
	/// </summary>
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

	/// <summary>
	/// Enable MACD filter.
	/// </summary>
	public bool UseMacdFilter { get => _useMacdFilter.Value; set => _useMacdFilter.Value = value; }

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFastLength { get => _macdFastLength.Value; set => _macdFastLength.Value = value; }

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlowLength { get => _macdSlowLength.Value; set => _macdSlowLength.Value = value; }

	/// <summary>
	/// MACD signal length.
	/// </summary>
	public int MacdSignalLength { get => _macdSignalLength.Value; set => _macdSignalLength.Value = value; }

	/// <summary>
	/// EMA 125 period.
	/// </summary>
	public int Ema125Period { get => _ema125Period.Value; set => _ema125Period.Value = value; }

	/// <summary>
	/// EMA 750 period.
	/// </summary>
	public int Ema750Period { get => _ema750Period.Value; set => _ema750Period.Value = value; }

	/// <summary>
	/// ATR band multiplier.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Enable time filter.
	/// </summary>
	public bool EnableTimeFilter { get => _enableTimeFilter.Value; set => _enableTimeFilter.Value = value; }

	/// <summary>
	/// Start time of trading.
	/// </summary>
	public TimeSpan StartTime { get => _startTime.Value; set => _startTime.Value = value; }

	/// <summary>
	/// End time of trading.
	/// </summary>
	public TimeSpan EndTime { get => _endTime.Value; set => _endTime.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="CciMacdStrategy"/> class.
	/// </summary>
	public CciMacdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI calculation length", "Indicators");

		_useMacdFilter = Param(nameof(UseMacdFilter), true)
			.SetDisplay("Use MACD Filter", "Enable MACD trend filter", "Indicators");

		_macdFastLength = Param(nameof(MacdFastLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast EMA length", "Indicators");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow EMA length", "Indicators");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal length", "Indicators");

		_ema125Period = Param(nameof(Ema125Period), 125)
			.SetGreaterThanZero()
			.SetDisplay("EMA125 Period", "EMA 125 length", "Indicators");

		_ema750Period = Param(nameof(Ema750Period), 750)
			.SetGreaterThanZero()
			.SetDisplay("EMA750 Period", "EMA 750 length", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 18m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR band multiplier", "Indicators");

		_enableTimeFilter = Param(nameof(EnableTimeFilter), false)
			.SetDisplay("Use Time Filter", "Enable trading only in time range", "General");

		_startTime = Param(nameof(StartTime), new TimeSpan(7, 30, 0))
			.SetDisplay("Start Time", "Start of trading", "General");

		_endTime = Param(nameof(EndTime), new TimeSpan(8, 45, 0))
			.SetDisplay("End Time", "End of trading", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_macd = new MovingAverageConvergenceDivergence
		{
			Fast = MacdFastLength,
			Slow = MacdSlowLength,
			Signal = MacdSignalLength
		};
		_ema125 = new ExponentialMovingAverage { Length = Ema125Period };
		_ema750 = new ExponentialMovingAverage { Length = Ema750Period };
		_atr = new AverageTrueRange { Length = Ema750Period, Type = MovingAverageType.Exponential };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _ema125, _ema750, _atr, _cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private bool IsInTimeRange(DateTimeOffset time)
	{
		if (!EnableTimeFilter)
			return true;

		var t = time.TimeOfDay;
		return StartTime <= EndTime ? t >= StartTime && t <= EndTime : t >= StartTime || t <= EndTime;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, decimal ema125Value, decimal ema750Value, decimal atrValue, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed || !_ema125.IsFormed || !_ema750.IsFormed || !_atr.IsFormed || !_cci.IsFormed)
			return;

		var macd = (MovingAverageConvergenceDivergenceValue)macdValue;
		var macdLine = macd.Macd;

		var cciCrossAbove = _initialized && _prevCci <= 0m && cciValue > 0m;
		var cciCrossBelow = _initialized && _prevCci >= 0m && cciValue < 0m;
		_prevCci = cciValue;
		_initialized = true;

		var macdBullish = macdLine > 0m;
		var macdBearish = macdLine < 0m;

		var cciSignalUp = cciCrossAbove && (!UseMacdFilter || macdBullish);
		var cciSignalDown = cciCrossBelow && (!UseMacdFilter || macdBearish);

		var upperBand = ema750Value + AtrMultiplier * atrValue;
		var lowerBand = ema750Value - AtrMultiplier * atrValue;

		string? currentSignalColor = null;
		if (candle.ClosePrice > ema125Value && candle.ClosePrice > ema750Value && candle.ClosePrice < upperBand && cciSignalUp)
		{
			currentSignalColor = "aqua";
		}
		else if (candle.ClosePrice < ema125Value && candle.ClosePrice < ema750Value && candle.ClosePrice > lowerBand && cciSignalDown)
		{
			currentSignalColor = "red";
		}

		var timeOk = IsInTimeRange(candle.OpenTime);
		var longCondition = cciSignalUp && currentSignalColor == "aqua" && timeOk;
		var shortCondition = cciSignalDown && currentSignalColor == "red" && timeOk;

		if (longCondition && Position <= 0)
			BuyMarket();
		else if (shortCondition && Position >= 0)
			SellMarket();
	}
}
