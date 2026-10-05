using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long EMA advanced exit strategy.
/// Long only: enters when the short MA crosses above (or, with the Above condition, is above) the medium MA while price is above the long MA.
/// Exits when the MACD on the higher MacdCandleType timeframe crosses below its signal line, when price closes below the MaCloseExitPeriod MA,
/// when the short MA crosses below the medium MA, when a candle range exceeds AtrMultiplier ATRs, or on a percent trailing stop; each exit can be switched off.
/// </summary>
public class LongEmaAdvancedExitStrategy : Strategy
{
	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MaTypes
	{
		/// <summary>
		/// Exponential moving average.
		/// </summary>
		EMA,

		/// <summary>
		/// Simple moving average.
		/// </summary>
		SMA,
	}

	/// <summary>
	/// Entry condition types.
	/// </summary>
	public enum EntryConditions
	{
		/// <summary>
		/// Short MA crosses above the medium MA.
		/// </summary>
		Crossover,

		/// <summary>
		/// Short MA is above the medium MA.
		/// </summary>
		Above,
	}

	private readonly StrategyParam<MaTypes> _maType;
	private readonly StrategyParam<EntryConditions> _entryConditionType;
	private readonly StrategyParam<int> _longTermPeriod;
	private readonly StrategyParam<int> _shortTermPeriod;
	private readonly StrategyParam<int> _midTermPeriod;
	private readonly StrategyParam<bool> _enableMacdExit;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPercent;
	private readonly StrategyParam<bool> _useMaCloseExit;
	private readonly StrategyParam<int> _maCloseExitPeriod;
	private readonly StrategyParam<bool> _useMaCrossExit;
	private readonly StrategyParam<bool> _useVolatilityFilter;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevShort;
	private decimal? _prevMid;
	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private MovingAverageConvergenceDivergenceSignal _macd;

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MaTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Entry condition type.
	/// </summary>
	public EntryConditions EntryConditionType
	{
		get => _entryConditionType.Value;
		set => _entryConditionType.Value = value;
	}

	/// <summary>
	/// Long-term MA period.
	/// </summary>
	public int LongTermPeriod
	{
		get => _longTermPeriod.Value;
		set => _longTermPeriod.Value = value;
	}

	/// <summary>
	/// Short-term MA period.
	/// </summary>
	public int ShortTermPeriod
	{
		get => _shortTermPeriod.Value;
		set => _shortTermPeriod.Value = value;
	}

	/// <summary>
	/// Medium-term MA period.
	/// </summary>
	public int MidTermPeriod
	{
		get => _midTermPeriod.Value;
		set => _midTermPeriod.Value = value;
	}

	/// <summary>
	/// Exit on a MACD cross down.
	/// </summary>
	public bool EnableMacdExit
	{
		get => _enableMacdExit.Value;
		set => _enableMacdExit.Value = value;
	}

	/// <summary>
	/// Candle type of the MACD exit.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// MACD fast length.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// MACD slow length.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// MACD signal length.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Use the percent trailing stop.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop percent.
	/// </summary>
	public decimal TrailingStopPercent
	{
		get => _trailingStopPercent.Value;
		set => _trailingStopPercent.Value = value;
	}

	/// <summary>
	/// Exit when price closes below the exit MA.
	/// </summary>
	public bool UseMaCloseExit
	{
		get => _useMaCloseExit.Value;
		set => _useMaCloseExit.Value = value;
	}

	/// <summary>
	/// Exit MA period.
	/// </summary>
	public int MaCloseExitPeriod
	{
		get => _maCloseExitPeriod.Value;
		set => _maCloseExitPeriod.Value = value;
	}

	/// <summary>
	/// Exit when the short MA crosses below the medium MA.
	/// </summary>
	public bool UseMaCrossExit
	{
		get => _useMaCrossExit.Value;
		set => _useMaCrossExit.Value = value;
	}

	/// <summary>
	/// Exit when a candle range exceeds the ATR threshold.
	/// </summary>
	public bool UseVolatilityFilter
	{
		get => _useVolatilityFilter.Value;
		set => _useVolatilityFilter.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the volatility exit.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public LongEmaAdvancedExitStrategy()
	{
		_maType = Param(nameof(MaType), MaTypes.EMA)
			.SetDisplay("MA Type", "Moving average type", "Indicators");

		_entryConditionType = Param(nameof(EntryConditionType), EntryConditions.Crossover)
			.SetDisplay("Entry Condition", "Crossover or Above", "Entry");

		_longTermPeriod = Param(nameof(LongTermPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Long MA", "Long-term MA period", "Indicators");

		_shortTermPeriod = Param(nameof(ShortTermPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short MA", "Short-term MA period", "Indicators");

		_midTermPeriod = Param(nameof(MidTermPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Medium MA", "Medium-term MA period", "Indicators");

		_enableMacdExit = Param(nameof(EnableMacdExit), true)
			.SetDisplay("MACD Exit", "Exit on a MACD cross down", "Exit");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(7).TimeFrame())
			.SetDisplay("MACD Candle Type", "Candle type of the MACD exit", "Exit");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast length", "Exit");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow length", "Exit");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal length", "Exit");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing Stop", "Use the percent trailing stop", "Risk");

		_trailingStopPercent = Param(nameof(TrailingStopPercent), 15m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop %", "Trailing stop percent", "Risk");

		_useMaCloseExit = Param(nameof(UseMaCloseExit), false)
			.SetDisplay("MA Close Exit", "Exit when price closes below the exit MA", "Exit");

		_maCloseExitPeriod = Param(nameof(MaCloseExitPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Exit MA Period", "Exit MA period", "Exit");

		_useMaCrossExit = Param(nameof(UseMaCrossExit), true)
			.SetDisplay("MA Cross Exit", "Exit when the short MA crosses below the medium MA", "Exit");

		_useVolatilityFilter = Param(nameof(UseVolatilityFilter), false)
			.SetDisplay("Volatility Exit", "Exit when a candle range exceeds the ATR threshold", "Exit");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Exit");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "ATR multiplier of the volatility exit", "Exit");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, MacdCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevShort = null;
		_prevMid = null;
		_prevMacd = null;
		_prevSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevShort = null;
		_prevMid = null;
		_prevMacd = null;
		_prevSignal = null;

		var shortMa = CreateMa(ShortTermPeriod);
		var midMa = CreateMa(MidTermPeriod);
		var longMa = CreateMa(LongTermPeriod);
		var exitMa = CreateMa(MaCloseExitPeriod);
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(shortMa, midMa, longMa, exitMa, atr, ProcessCandle)
			.Start();

		_macd = new MovingAverageConvergenceDivergenceSignal();
		_macd.Macd.ShortMa.Length = MacdFastLength;
		_macd.Macd.LongMa.Length = MacdSlowLength;
		_macd.SignalMa.Length = MacdSignalLength;

		// The weekly MACD needs months of history, so it is processed manually to keep it from blocking entries until it forms.
		SubscribeCandles(MacdCandleType)
			.Bind(ProcessMacd)
			.Start();

		if (UseTrailingStop && TrailingStopPercent > 0)
			StartProtection(new Unit(), new Unit(TrailingStopPercent, UnitTypes.Percent), isStopTrailing: true, useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortMa);
			DrawIndicator(area, midMa);
			DrawIndicator(area, longMa);
			DrawOwnTrades(area);
		}
	}

	private IIndicator CreateMa(int length)
	{
		return MaType == MaTypes.SMA
			? new SimpleMovingAverage { Length = length }
			: new ExponentialMovingAverage { Length = length };
	}

	private void ProcessMacd(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdValue = _macd.Process(candle);

		if (!macdValue.IsFormed || macdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macd, Signal: decimal signal })
			return;

		var crossDown = _prevMacd is decimal prevMacd && _prevSignal is decimal prevSignal && prevMacd >= prevSignal && macd < signal;
		_prevMacd = macd;
		_prevSignal = signal;

		if (crossDown && EnableMacdExit && Position > 0 && IsFormedAndOnlineAndAllowTrading())
			SellMarket(Position);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue shortValue, IIndicatorValue midValue, IIndicatorValue longValue, IIndicatorValue exitValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!shortValue.IsFormed || !midValue.IsFormed)
			return;

		var shortMa = shortValue.GetValue<decimal>();
		var midMa = midValue.GetValue<decimal>();
		var prevShort = _prevShort;
		var prevMid = _prevMid;
		_prevShort = shortMa;
		_prevMid = midMa;

		if (prevShort is not decimal ps || prevMid is not decimal pm)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var crossUp = ps <= pm && shortMa > midMa;
		var crossDown = ps >= pm && shortMa < midMa;

		if (Position > 0)
		{
			var exit = (UseMaCrossExit && crossDown)
				|| (UseMaCloseExit && exitValue.IsFormed && close < exitValue.GetValue<decimal>())
				|| (UseVolatilityFilter && atrValue.IsFormed && candle.HighPrice - candle.LowPrice > atrValue.GetValue<decimal>() * AtrMultiplier);

			if (exit)
				SellMarket(Position);

			return;
		}

		if (Position < 0 || !longValue.IsFormed)
			return;

		var entry = EntryConditionType == EntryConditions.Crossover ? crossUp : shortMa > midMa;

		if (entry && close > longValue.GetValue<decimal>())
			BuyMarket(Volume);
	}
}
