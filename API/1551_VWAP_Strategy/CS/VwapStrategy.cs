using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VWAP strategy with entry bands, signal-based stops and multiple exit modes.
/// </summary>
public class VwapStrategy : Strategy
{
	public enum ExitMode
	{
		Vwap,
		Deviation,
		None
	}

	private readonly StrategyParam<decimal> _stopPoints;
	private readonly StrategyParam<ExitMode> _exitModeLong;
	private readonly StrategyParam<ExitMode> _exitModeShort;
	private readonly StrategyParam<decimal> _targetLongDeviation;
	private readonly StrategyParam<decimal> _targetShortDeviation;
	private readonly StrategyParam<bool> _enableSafetyExit;
	private readonly StrategyParam<int> _numOpposingBars;
	private readonly StrategyParam<bool> _allowLongs;
	private readonly StrategyParam<bool> _allowShorts;
	private readonly StrategyParam<decimal> _minStrength;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _sessionDate;
	private decimal _sumSrc;
	private decimal _sumVol;
	private decimal _sumSrcSqVol;
	private decimal? _signalLow;
	private decimal? _signalHigh;
	private int _bullCount;
	private int _bearCount;

	/// <summary>
	/// Stop buffer in price points.
	/// </summary>
	public decimal StopPoints { get => _stopPoints.Value; set => _stopPoints.Value = value; }

	/// <summary>
	/// Long exit mode.
	/// </summary>
	public ExitMode ExitModeLong { get => _exitModeLong.Value; set => _exitModeLong.Value = value; }

	/// <summary>
	/// Short exit mode.
	/// </summary>
	public ExitMode ExitModeShort { get => _exitModeShort.Value; set => _exitModeShort.Value = value; }

	/// <summary>
	/// Deviation multiplier for long targets.
	/// </summary>
	public decimal TargetLongDeviation { get => _targetLongDeviation.Value; set => _targetLongDeviation.Value = value; }

	/// <summary>
	/// Deviation multiplier for short targets.
	/// </summary>
	public decimal TargetShortDeviation { get => _targetShortDeviation.Value; set => _targetShortDeviation.Value = value; }

	/// <summary>
	/// Enable safety exit.
	/// </summary>
	public bool EnableSafetyExit { get => _enableSafetyExit.Value; set => _enableSafetyExit.Value = value; }

	/// <summary>
	/// Number of opposing bars for safety exit.
	/// </summary>
	public int NumOpposingBars { get => _numOpposingBars.Value; set => _numOpposingBars.Value = value; }

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool AllowLongs { get => _allowLongs.Value; set => _allowLongs.Value = value; }

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool AllowShorts { get => _allowShorts.Value; set => _allowShorts.Value = value; }

	/// <summary>
	/// Minimum signal strength.
	/// </summary>
	public decimal MinStrength { get => _minStrength.Value; set => _minStrength.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VwapStrategy()
	{
		_stopPoints = Param(nameof(StopPoints), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Points", "Stop buffer from signal bar", "Parameters");

		_exitModeLong = Param(nameof(ExitModeLong), ExitMode.Vwap)
			.SetDisplay("Long Exit Mode", string.Empty, "Parameters");

		_exitModeShort = Param(nameof(ExitModeShort), ExitMode.Vwap)
			.SetDisplay("Short Exit Mode", string.Empty, "Parameters");

		_targetLongDeviation = Param(nameof(TargetLongDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Long Target Deviation", string.Empty, "Parameters");

		_targetShortDeviation = Param(nameof(TargetShortDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Short Target Deviation", string.Empty, "Parameters");

		_enableSafetyExit = Param(nameof(EnableSafetyExit), true)
			.SetDisplay("Enable Safety Exit", string.Empty, "Parameters");

		_numOpposingBars = Param(nameof(NumOpposingBars), 3)
			.SetGreaterThanZero()
			.SetDisplay("Opposing Bars", string.Empty, "Parameters");

		_allowLongs = Param(nameof(AllowLongs), true)
			.SetDisplay("Allow Longs", string.Empty, "Parameters");

		_allowShorts = Param(nameof(AllowShorts), true)
			.SetDisplay("Allow Shorts", string.Empty, "Parameters");

		_minStrength = Param(nameof(MinStrength), 0.7m)
			.SetGreaterThanZero()
			.SetDisplay("Min Strength", string.Empty, "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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

		_sessionDate = default;
		_sumSrc = 0m;
		_sumVol = 0m;
		_sumSrcSqVol = 0m;
		_signalLow = null;
		_signalHigh = null;
		_bullCount = 0;
		_bearCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime.UtcDateTime.Date;
		var vol = candle.TotalVolume ?? 0m;
		var src = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		if (date != _sessionDate)
		{
			_sessionDate = date;
			_sumSrc = src * vol;
			_sumVol = vol;
			_sumSrcSqVol = src * src * vol;
		}
		else
		{
			_sumSrc += src * vol;
			_sumVol += vol;
			_sumSrcSqVol += src * src * vol;
		}

		if (_sumVol == 0m)
			return;

		var vwap = _sumSrc / _sumVol;
		var variance = _sumSrcSqVol / _sumVol - vwap * vwap;
		var stdev = (decimal)Math.Sqrt((double)Math.Max(variance, 0m));

		var entryUpper = vwap + stdev * 2m;
		var entryLower = vwap - stdev * 2m;
		var targetUpperLong = vwap + stdev * TargetLongDeviation;
		var targetLowerShort = vwap - stdev * TargetShortDeviation;

		var barRange = candle.HighPrice - candle.LowPrice;
		var bullStrength = barRange > 0m ? (candle.ClosePrice - candle.LowPrice) / barRange : 0m;
		var bearStrength = barRange > 0m ? (candle.HighPrice - candle.ClosePrice) / barRange : 0m;

		if (candle.ClosePrice > candle.OpenPrice)
		{
			_bullCount++;
			_bearCount = 0;
		}
		else if (candle.ClosePrice < candle.OpenPrice)
		{
			_bearCount++;
			_bullCount = 0;
		}
		else
		{
			_bullCount = 0;
			_bearCount = 0;
		}

		var longCondition = AllowLongs && candle.OpenPrice < entryLower && candle.ClosePrice > entryLower && bullStrength >= MinStrength && Position == 0;
		var shortCondition = AllowShorts && candle.OpenPrice > entryUpper && candle.ClosePrice < entryUpper && bearStrength >= MinStrength && Position == 0;

		if (longCondition)
		{
			BuyMarket();
			_signalLow = candle.LowPrice;
			_signalHigh = null;
		}
		else if (shortCondition)
		{
			SellMarket();
			_signalHigh = candle.HighPrice;
			_signalLow = null;
		}

		if (Position == 0)
		{
			_signalLow = null;
			_signalHigh = null;
		}

		if (Position > 0 && _signalLow.HasValue)
		{
			var stop = _signalLow.Value - StopPoints;
			var exitVwap = ExitModeLong == ExitMode.Vwap && candle.HighPrice >= vwap;
			var exitDev = ExitModeLong == ExitMode.Deviation && candle.HighPrice >= targetUpperLong;

			if (candle.LowPrice <= stop || (ExitModeLong != ExitMode.None && (exitVwap || exitDev)))
				SellMarket();
			else if (EnableSafetyExit && _bearCount >= NumOpposingBars)
				SellMarket();
		}
		else if (Position < 0 && _signalHigh.HasValue)
		{
			var stop = _signalHigh.Value + StopPoints;
			var exitVwap = ExitModeShort == ExitMode.Vwap && candle.LowPrice <= vwap;
			var exitDev = ExitModeShort == ExitMode.Deviation && candle.LowPrice <= targetLowerShort;

			if (candle.HighPrice >= stop || (ExitModeShort != ExitMode.None && (exitVwap || exitDev)))
				BuyMarket();
			else if (EnableSafetyExit && _bullCount >= NumOpposingBars)
				BuyMarket();
		}
	}
}

