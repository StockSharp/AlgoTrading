using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only strategy using configurable moving averages with multiple exit options.
/// </summary>
public class LongEmaAdvancedExitStrategy : Strategy
{
	private readonly StrategyParam<string> _maType;
	private readonly StrategyParam<string> _entryType;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _midPeriod;
	private readonly StrategyParam<bool> _enableMacdExit;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<decimal> _trailingPercent;
	private readonly StrategyParam<bool> _useMaCloseExit;
	private readonly StrategyParam<int> _maClosePeriod;
	private readonly StrategyParam<bool> _useMaCrossExit;
	private readonly StrategyParam<bool> _useVolatilityFilter;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _atrSma;
	private MovingAverage _shortMa;
	private MovingAverage _midMa;
	private decimal _prevShort;
	private decimal _prevMid;
	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _maxPrice;
	private decimal _trailStop;

	/// <summary>
	/// Moving average type.
	/// </summary>
	public string MaType { get => _maType.Value; set => _maType.Value = value; }

	/// <summary>
	/// Entry condition type.
	/// </summary>
	public string EntryConditionType { get => _entryType.Value; set => _entryType.Value = value; }

	/// <summary>
	/// Long moving average period.
	/// </summary>
	public int LongTermPeriod { get => _longPeriod.Value; set => _longPeriod.Value = value; }

	/// <summary>
	/// Short moving average period.
	/// </summary>
	public int ShortTermPeriod { get => _shortPeriod.Value; set => _shortPeriod.Value = value; }

	/// <summary>
	/// Medium moving average period.
	/// </summary>
	public int MidTermPeriod { get => _midPeriod.Value; set => _midPeriod.Value = value; }

	/// <summary>
	/// Use MACD cross down exit.
	/// </summary>
	public bool EnableMacdExit { get => _enableMacdExit.Value; set => _enableMacdExit.Value = value; }

	/// <summary>
	/// Timeframe for MACD calculation.
	/// </summary>
	public DataType MacdCandleType { get => _macdCandleType.Value; set => _macdCandleType.Value = value; }

	/// <summary>
	/// MACD fast length.
	/// </summary>
	public int MacdFastLength { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// <summary>
	/// MACD slow length.
	/// </summary>
	public int MacdSlowLength { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// <summary>
	/// MACD signal length.
	/// </summary>
	public int MacdSignalLength { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// <summary>
	/// Use trailing stop.
	/// </summary>
	public bool UseTrailingStop { get => _useTrailing.Value; set => _useTrailing.Value = value; }

	/// <summary>
	/// Trailing stop percent.
	/// </summary>
	public decimal TrailingStopPercent { get => _trailingPercent.Value; set => _trailingPercent.Value = value; }

	/// <summary>
	/// Close position if price closes below selected MA.
	/// </summary>
	public bool UseMaCloseExit { get => _useMaCloseExit.Value; set => _useMaCloseExit.Value = value; }

	/// <summary>
	/// Selected MA period for close exit.
	/// </summary>
	public int MaCloseExitPeriod { get => _maClosePeriod.Value; set => _maClosePeriod.Value = value; }

	/// <summary>
	/// Close position on short MA cross below medium MA.
	/// </summary>
	public bool UseMaCrossExit { get => _useMaCrossExit.Value; set => _useMaCrossExit.Value = value; }

	/// <summary>
	/// Apply volatility filter.
	/// </summary>
	public bool UseVolatilityFilter { get => _useVolatilityFilter.Value; set => _useVolatilityFilter.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier for volatility filter.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="LongEmaAdvancedExitStrategy"/>.
	/// </summary>
	public LongEmaAdvancedExitStrategy()
	{
		_maType = Param(nameof(MaType), "EMA").SetDisplay("MA Type", "Moving average type", "General");
		_entryType = Param(nameof(EntryConditionType), "Crossover").SetDisplay("Entry Type", "Entry condition type", "Signals");
		_longPeriod = Param(nameof(LongTermPeriod), 200).SetDisplay("Long MA", "Long MA period", "Indicators");
		_shortPeriod = Param(nameof(ShortTermPeriod), 5).SetDisplay("Short MA", "Short MA period", "Indicators");
		_midPeriod = Param(nameof(MidTermPeriod), 10).SetDisplay("Mid MA", "Mid MA period", "Indicators");
		_enableMacdExit = Param(nameof(EnableMacdExit), true).SetDisplay("MACD Exit", "Enable MACD exit", "Exit");
		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(7).TimeFrame()).SetDisplay("MACD TF", "MACD timeframe", "Exit");
		_macdFast = Param(nameof(MacdFastLength), 12).SetDisplay("MACD Fast", "MACD fast length", "Exit");
		_macdSlow = Param(nameof(MacdSlowLength), 26).SetDisplay("MACD Slow", "MACD slow length", "Exit");
		_macdSignal = Param(nameof(MacdSignalLength), 9).SetDisplay("MACD Signal", "MACD signal smoothing", "Exit");
		_useTrailing = Param(nameof(UseTrailingStop), false).SetDisplay("Trailing", "Use trailing stop", "Exit");
		_trailingPercent = Param(nameof(TrailingStopPercent), 15m).SetDisplay("Trail %", "Trailing stop percent", "Exit").SetGreaterThanZero();
		_useMaCloseExit = Param(nameof(UseMaCloseExit), false).SetDisplay("MA Close Exit", "Close if price below MA", "Exit");
		_maClosePeriod = Param(nameof(MaCloseExitPeriod), 50).SetDisplay("MA Close Period", "MA period for close exit", "Exit");
		_useMaCrossExit = Param(nameof(UseMaCrossExit), true).SetDisplay("MA Cross Exit", "Use MA cross exit", "Exit");
		_useVolatilityFilter = Param(nameof(UseVolatilityFilter), false).SetDisplay("Volatility Filter", "Use ATR filter", "Filters");
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetDisplay("ATR Period", "ATR period", "Filters");
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m).SetDisplay("ATR Mult", "ATR multiplier", "Filters").SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (EnableMacdExit)
			return [(Security, CandleType), (Security, MacdCandleType)];
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_atrSma = null;
		_shortMa = null;
		_midMa = null;
		_prevShort = 0m;
		_prevMid = 0m;
		_prevMacd = 0m;
		_prevSignal = 0m;
		_maxPrice = 0m;
		_trailStop = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var longMa = CreateMa(MaType, LongTermPeriod);
		_shortMa = CreateMa(MaType, ShortTermPeriod);
		_midMa = CreateMa(MaType, MidTermPeriod);
		var selectedMa = CreateMa(MaType, MaCloseExitPeriod);
		var atr = new AverageTrueRange { Length = AtrPeriod };
		_atrSma = new SimpleMovingAverage { Length = AtrPeriod };
		var macd = new MovingAverageConvergenceDivergence
		{
			FastLength = MacdFastLength,
			SlowLength = MacdSlowLength,
			SignalLength = MacdSignalLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(longMa, _shortMa, _midMa, selectedMa, atr, ProcessCandle)
			.Start();

		if (EnableMacdExit)
		{
			var macdSubscription = SubscribeCandles(MacdCandleType);
			macdSubscription
				.Bind(macd, ProcessMacd)
				.Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, longMa);
			DrawIndicator(area, _shortMa);
			DrawIndicator(area, _midMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private MovingAverage CreateMa(string type, int length)
	{
		return type switch
		{
			"SMA" => new SimpleMovingAverage { Length = length },
			"WMA" => new WeightedMovingAverage { Length = length },
			"HMA" => new HullMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	private void ProcessMacd(ICandleMessage candle, decimal macdValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var crossDown = _prevMacd > _prevSignal && macdValue < signalValue;
		if (EnableMacdExit && crossDown && Position > 0)
			SellMarket(Math.Abs(Position));

		_prevMacd = macdValue;
		_prevSignal = signalValue;
	}

	private void ProcessCandle(ICandleMessage candle, decimal longValue, decimal shortValue, decimal midValue, decimal selectedMaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atrAvg = _atrSma.Process(atrValue, candle.ServerTime, true).ToDecimal();

		var priceAboveLong = candle.ClosePrice > longValue;
		var crossOver = _prevShort <= _prevMid && shortValue > midValue;
		var aboveMa = shortValue > midValue;
		var enter = EntryConditionType == "Crossover" ? crossOver && priceAboveLong : aboveMa && priceAboveLong;

		if (UseVolatilityFilter)
			enter = enter && atrValue > atrAvg * AtrMultiplier;

		if (enter && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (UseMaCloseExit && candle.ClosePrice < selectedMaValue && Position > 0)
			SellMarket(Math.Abs(Position));

		if (UseMaCrossExit && _prevShort >= _prevMid && shortValue < midValue && Position > 0)
			SellMarket(Math.Abs(Position));

		if (UseTrailingStop)
		{
			if (Position > 0)
			{
				_maxPrice = Math.Max(_maxPrice, candle.HighPrice);
				var stopLevel = _maxPrice * (1 - TrailingStopPercent / 100m);
				if (stopLevel > _trailStop)
					_trailStop = stopLevel;
				if (candle.ClosePrice <= _trailStop)
				{
					SellMarket(Math.Abs(Position));
					_maxPrice = 0m;
					_trailStop = 0m;
				}
			}
			else
			{
				_maxPrice = 0m;
				_trailStop = 0m;
			}
		}

		_prevShort = shortValue;
		_prevMid = midValue;
	}
}
