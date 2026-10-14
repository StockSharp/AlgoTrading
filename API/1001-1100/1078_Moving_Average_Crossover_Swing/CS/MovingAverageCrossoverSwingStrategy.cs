using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover swing strategy.
/// A fast EMA crossing above the medium EMA goes long and crossing below goes short (each side can be disabled).
/// Entries can require the close to be on the trade side of the slow EMA and the MACD histogram to be on the same side of zero.
/// Stop loss and take profit are ATR multiples fixed at entry, and a cross of the exit EMA pair against the position can also close it.
/// </summary>
public class MovingAverageCrossoverSwingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _mediumPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _fastExitPeriod;
	private readonly StrategyParam<int> _mediumExitPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrStopMultiplier;
	private readonly StrategyParam<decimal> _atrTakeMultiplier;
	private readonly StrategyParam<bool> _enableSlow;
	private readonly StrategyParam<bool> _enableMacd;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _enableCrossExit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevMedium;
	private decimal? _prevFastExit;
	private decimal? _prevMediumExit;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Fast EMA period for entries.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Medium EMA period for entries.
	/// </summary>
	public int MediumPeriod
	{
		get => _mediumPeriod.Value;
		set => _mediumPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for the trend filter.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period for the exit cross.
	/// </summary>
	public int FastExitPeriod
	{
		get => _fastExitPeriod.Value;
		set => _fastExitPeriod.Value = value;
	}

	/// <summary>
	/// Medium EMA period for the exit cross.
	/// </summary>
	public int MediumExitPeriod
	{
		get => _mediumExitPeriod.Value;
		set => _mediumExitPeriod.Value = value;
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
	/// Stop loss distance in ATR multiples.
	/// </summary>
	public decimal AtrStopMultiplier
	{
		get => _atrStopMultiplier.Value;
		set => _atrStopMultiplier.Value = value;
	}

	/// <summary>
	/// Take profit distance in ATR multiples.
	/// </summary>
	public decimal AtrTakeMultiplier
	{
		get => _atrTakeMultiplier.Value;
		set => _atrTakeMultiplier.Value = value;
	}

	/// <summary>
	/// Require the close to be on the trade side of the slow EMA.
	/// </summary>
	public bool EnableSlow
	{
		get => _enableSlow.Value;
		set => _enableSlow.Value = value;
	}

	/// <summary>
	/// Require the MACD histogram to be on the trade side of zero.
	/// </summary>
	public bool EnableMacd
	{
		get => _enableMacd.Value;
		set => _enableMacd.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Close the position when the exit EMA pair crosses against it.
	/// </summary>
	public bool EnableCrossExit
	{
		get => _enableCrossExit.Value;
		set => _enableCrossExit.Value = value;
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
	public MovingAverageCrossoverSwingStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast EMA period for entries", "Indicators");

		_mediumPeriod = Param(nameof(MediumPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Medium Period", "Medium EMA period for entries", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow EMA period for the trend filter", "Indicators");

		_fastExitPeriod = Param(nameof(FastExitPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Exit Period", "Fast EMA period for the exit cross", "Exit");

		_mediumExitPeriod = Param(nameof(MediumExitPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Medium Exit Period", "Medium EMA period for the exit cross", "Exit");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Risk");

		_atrStopMultiplier = Param(nameof(AtrStopMultiplier), 1.4m)
			.SetNotNegative()
			.SetDisplay("ATR Stop Multiplier", "Stop loss distance in ATR multiples", "Risk");

		_atrTakeMultiplier = Param(nameof(AtrTakeMultiplier), 3.2m)
			.SetNotNegative()
			.SetDisplay("ATR Take Multiplier", "Take profit distance in ATR multiples", "Risk");

		_enableSlow = Param(nameof(EnableSlow), true)
			.SetDisplay("Enable Slow EMA", "Require the close on the trade side of the slow EMA", "Filters");

		_enableMacd = Param(nameof(EnableMacd), true)
			.SetDisplay("Enable MACD", "Require the MACD histogram on the trade side of zero", "Filters");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long trades", "Trading");

		_enableShort = Param(nameof(EnableShort), false)
			.SetDisplay("Enable Short", "Allow short trades", "Trading");

		_enableCrossExit = Param(nameof(EnableCrossExit), true)
			.SetDisplay("Enable Cross Exit", "Close when the exit EMA pair crosses against the position", "Exit");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_prevFast = null;
		_prevMedium = null;
		_prevFastExit = null;
		_prevMediumExit = null;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var medium = new ExponentialMovingAverage { Length = MediumPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };
		var fastExit = new ExponentialMovingAverage { Length = FastExitPeriod };
		var mediumExit = new ExponentialMovingAverage { Length = MediumExitPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fast, medium, slow, fastExit, mediumExit, atr, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, medium);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue mediumValue, IIndicatorValue slowValue,
		IIndicatorValue fastExitValue, IIndicatorValue mediumExitValue, IIndicatorValue atrValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFormed || !mediumValue.IsFormed || !slowValue.IsFormed || !fastExitValue.IsFormed
			|| !mediumExitValue.IsFormed || !atrValue.IsFormed || !macdValue.IsFormed)
			return;

		if (macdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macdLine, Signal: decimal signalLine })
			return;

		var fast = fastValue.GetValue<decimal>();
		var medium = mediumValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();
		var fastExit = fastExitValue.GetValue<decimal>();
		var mediumExit = mediumExitValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var histogram = macdLine - signalLine;

		var prevFast = _prevFast;
		var prevMedium = _prevMedium;
		var prevFastExit = _prevFastExit;
		var prevMediumExit = _prevMediumExit;
		_prevFast = fast;
		_prevMedium = medium;
		_prevFastExit = fastExit;
		_prevMediumExit = mediumExit;

		if (prevFast is not decimal pf || prevMedium is not decimal pm || prevFastExit is not decimal pfe || prevMediumExit is not decimal pme)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			var stopHit = _stopPrice is decimal stop && candle.LowPrice <= stop;
			var takeHit = _takePrice is decimal take && candle.HighPrice >= take;
			var crossExit = EnableCrossExit && pfe >= pme && fastExit < mediumExit;

			if (stopHit || takeHit || crossExit)
			{
				SellMarket(Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}
		}
		else if (Position < 0)
		{
			var stopHit = _stopPrice is decimal stop && candle.HighPrice >= stop;
			var takeHit = _takePrice is decimal take && candle.LowPrice <= take;
			var crossExit = EnableCrossExit && pfe <= pme && fastExit > mediumExit;

			if (stopHit || takeHit || crossExit)
			{
				BuyMarket(-Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}
		}

		var longSignal = EnableLong && pf <= pm && fast > medium
			&& (!EnableSlow || close > slow)
			&& (!EnableMacd || histogram > 0m);

		var shortSignal = EnableShort && pf >= pm && fast < medium
			&& (!EnableSlow || close < slow)
			&& (!EnableMacd || histogram < 0m);

		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = AtrStopMultiplier > 0m ? close - atr * AtrStopMultiplier : null;
			_takePrice = AtrTakeMultiplier > 0m ? close + atr * AtrTakeMultiplier : null;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = AtrStopMultiplier > 0m ? close + atr * AtrStopMultiplier : null;
			_takePrice = AtrTakeMultiplier > 0m ? close - atr * AtrTakeMultiplier : null;
		}
	}
}
