using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reversal Catcher strategy.
/// Uses Bollinger Bands, RSI and EMA trend filter to catch reversals.
/// </summary>
public class ReversalCatcherStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<int> _endOfDay;
	private readonly StrategyParam<bool> _mktAlwaysOn;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private RelativeStrengthIndex _rsi;
	private BollingerBands _bollinger;

	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevRsi;
	private decimal? _prevBbUpper;
	private decimal? _prevBbLower;
	private decimal? _stop;
	private decimal? _target;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Overbought level for RSI.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// Oversold level for RSI.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// End of day in HHMM format.
	/// </summary>
	public int EndOfDay
	{
		get => _endOfDay.Value;
		set => _endOfDay.Value = value;
	}

	/// <summary>
	/// Markets that are always open.
	/// </summary>
	public bool MktAlwaysOn
	{
		get => _mktAlwaysOn.Value;
		set => _mktAlwaysOn.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ReversalCatcherStrategy"/>.
	/// </summary>
	public ReversalCatcherStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")
			.SetCanOptimize(true);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 1.5m)
			.SetRange(1m, 4m)
			.SetDisplay("Bollinger Deviation", "Standard deviation for Bollinger Bands", "Indicators")
			.SetCanOptimize(true);

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Trends")
			.SetCanOptimize(true);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Trends")
			.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "Momentum")
			.SetCanOptimize(true);

		_overbought = Param(nameof(Overbought), 70m)
			.SetRange(50m, 90m)
			.SetDisplay("Overbought", "RSI overbought level", "Momentum")
			.SetCanOptimize(true);

		_oversold = Param(nameof(Oversold), 30m)
			.SetRange(10m, 50m)
			.SetDisplay("Oversold", "RSI oversold level", "Momentum")
			.SetCanOptimize(true);

		_endOfDay = Param(nameof(EndOfDay), 1500)
			.SetDisplay("End Of Day", "Close all positions after HHMM", "Trade settings");

		_mktAlwaysOn = Param(nameof(MktAlwaysOn), false)
			.SetDisplay("Market Always On", "Skip end-of-day exit", "Trade settings");

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

		_emaFast = default;
		_emaSlow = default;
		_rsi = default;
		_bollinger = default;
		_prevHigh = default;
		_prevLow = default;
		_prevRsi = default;
		_prevBbUpper = default;
		_prevBbLower = default;
		_stop = default;
		_target = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaFast = new ExponentialMovingAverage { Length = FastEmaPeriod };
		_emaSlow = new ExponentialMovingAverage { Length = SlowEmaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_emaFast, _emaSlow, _rsi, _bollinger, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow, decimal rsi, decimal bbMiddle, decimal bbUpper, decimal bbLower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var boardTz = Security.Board?.BoardTimeZone ?? TimeZoneInfo.Utc;
		var localTime = TimeZoneInfo.ConvertTime(candle.CloseTime, boardTz);
		var hourVal = localTime.Hour * 100 + localTime.Minute;

		if (!MktAlwaysOn && hourVal >= EndOfDay)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));

			_stop = null;
			_target = null;

			_prevRsi = rsi;
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevBbUpper = bbUpper;
			_prevBbLower = bbLower;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevRsi = rsi;
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevBbUpper = bbUpper;
			_prevBbLower = bbLower;
			return;
		}

		var upTrend = emaFast > emaSlow;
		var downTrend = emaFast < emaSlow;

		var hhLLong = _prevLow is decimal pl && _prevHigh is decimal ph &&
			candle.LowPrice > pl && candle.HighPrice > ph && candle.ClosePrice > ph;
		var hhLLShort = _prevLow is decimal pl2 && _prevHigh is decimal ph2 &&
			candle.LowPrice < pl2 && candle.HighPrice < ph2 && candle.ClosePrice < pl2;

		var rsiCrossOver = _prevRsi is decimal pr1 && pr1 < Oversold && rsi >= Oversold;
		var rsiCrossUnder = _prevRsi is decimal pr2 && pr2 > Overbought && rsi <= Overbought;

		var longCond = _prevHigh is decimal pHigh && _prevBbLower is decimal pLower &&
			pHigh < pLower &&
			candle.ClosePrice > bbLower && candle.ClosePrice < bbUpper &&
			hhLLong && rsiCrossOver && downTrend;

		var shortCond = _prevLow is decimal pLow && _prevBbUpper is decimal pUpper &&
			pLow > pUpper &&
			candle.ClosePrice < bbUpper && candle.ClosePrice > bbLower &&
			hhLLShort && rsiCrossUnder && upTrend;

		if (longCond && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
			_stop = _prevLow;
			_target = candle.HighPrice >= bbMiddle ? bbUpper : bbMiddle;
		}
		else if (shortCond && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
			_stop = _prevHigh;
			_target = candle.LowPrice <= bbMiddle ? bbLower : bbMiddle;
		}

		if (Position > 0 && _stop is decimal sl && _target is decimal tl)
		{
			if (candle.ClosePrice <= sl || candle.ClosePrice >= tl)
			{
				SellMarket(Math.Abs(Position));
				_stop = null;
				_target = null;
			}
		}
		else if (Position < 0 && _stop is decimal ss && _target is decimal ts)
		{
			if (candle.ClosePrice >= ss || candle.ClosePrice <= ts)
			{
				BuyMarket(Math.Abs(Position));
				_stop = null;
				_target = null;
			}
		}

		_prevRsi = rsi;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevBbUpper = bbUpper;
		_prevBbLower = bbLower;
	}
}
