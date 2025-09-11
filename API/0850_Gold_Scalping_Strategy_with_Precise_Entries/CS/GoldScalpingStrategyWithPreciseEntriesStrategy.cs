using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gold scalping strategy with precise entries.
/// </summary>
public class GoldScalpingStrategyWithPreciseEntriesStrategy : Strategy
{
	private readonly StrategyParam<int> _emaFastPeriod;
	private readonly StrategyParam<int> _emaSlowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _rsiLower;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _pipTarget;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prevCandle;
	private decimal _longStop;
	private decimal _longTarget;
	private decimal _shortStop;
	private decimal _shortTarget;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int EmaFastPeriod
	{
		get => _emaFastPeriod.Value;
		set => _emaFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int EmaSlowPeriod
	{
		get => _emaSlowPeriod.Value;
		set => _emaSlowPeriod.Value = value;
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
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Lower RSI bound.
	/// </summary>
	public decimal RsiLower
	{
		get => _rsiLower.Value;
		set => _rsiLower.Value = value;
	}

	/// <summary>
	/// Upper RSI bound.
	/// </summary>
	public decimal RsiUpper
	{
		get => _rsiUpper.Value;
		set => _rsiUpper.Value = value;
	}

	/// <summary>
	/// Profit target in price points.
	/// </summary>
	public decimal PipTarget
	{
		get => _pipTarget.Value;
		set => _pipTarget.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="GoldScalpingStrategyWithPreciseEntriesStrategy"/>.
	/// </summary>
	public GoldScalpingStrategyWithPreciseEntriesStrategy()
	{
		_emaFastPeriod = Param(nameof(EmaFastPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Fast", "Fast EMA period", "Indicators")
			.SetCanOptimize(true);

		_emaSlowPeriod = Param(nameof(EmaSlowPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Slow", "Slow EMA period", "Indicators")
			.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators")
			.SetCanOptimize(true);

		_rsiLower = Param(nameof(RsiLower), 45m)
			.SetDisplay("RSI Lower", "Lower RSI bound", "Indicators");

		_rsiUpper = Param(nameof(RsiUpper), 55m)
			.SetDisplay("RSI Upper", "Upper RSI bound", "Indicators");

		_pipTarget = Param(nameof(PipTarget), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target", "Profit target in price points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevCandle = null;
		_longStop = 0m;
		_longTarget = 0m;
		_shortStop = 0m;
		_shortTarget = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var emaFast = new ExponentialMovingAverage { Length = EmaFastPeriod };
		var emaSlow = new ExponentialMovingAverage { Length = EmaSlowPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaFast, emaSlow, atr, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow, decimal atr, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevCandle = candle;
			return;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _longStop || candle.HighPrice >= _longTarget)
			{
				SellMarket(Position);
				_longStop = 0m;
				_longTarget = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTarget)
			{
				BuyMarket(-Position);
				_shortStop = 0m;
				_shortTarget = 0m;
			}
		}

		var bullishEngulfing = _prevCandle is not null &&
			candle.ClosePrice > candle.OpenPrice &&
			_prevCandle.ClosePrice < _prevCandle.OpenPrice &&
			candle.ClosePrice > _prevCandle.ClosePrice &&
			candle.OpenPrice < _prevCandle.ClosePrice;

		var bearishEngulfing = _prevCandle is not null &&
			candle.ClosePrice < candle.OpenPrice &&
			_prevCandle.ClosePrice > _prevCandle.OpenPrice &&
			candle.ClosePrice < _prevCandle.ClosePrice &&
			candle.OpenPrice > _prevCandle.ClosePrice;

		var longCondition = emaFast > emaSlow && rsi >= RsiLower && rsi <= RsiUpper && bullishEngulfing && candle.ClosePrice > emaFast;
		var shortCondition = emaFast < emaSlow && rsi >= RsiLower && rsi <= RsiUpper && bearishEngulfing && candle.ClosePrice < emaFast;

		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_longStop = candle.ClosePrice - atr;
			_longTarget = candle.ClosePrice + PipTarget;
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_shortStop = candle.ClosePrice + atr;
			_shortTarget = candle.ClosePrice - PipTarget;
		}

		_prevCandle = candle;
	}
}
