namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Flex ATR strategy using EMA crossover with RSI filter and ATR-based stops.
/// </summary>
public class FlexAtrStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<decimal> _atrStopMult;
	private readonly StrategyParam<decimal> _atrProfitMult;
	private readonly StrategyParam<bool> _enableTrailingStop;
	private readonly StrategyParam<decimal> _atrTrailMult;

	private readonly Dictionary<double, (decimal fastDays, decimal slowDays, decimal rsiDays, decimal atrDays)> _paramSets = new()
	{
		[1440] = (8m, 21m, 14m, 14m),
		[10080] = (40m, 105m, 14m, 14m),
		[30] = (0.35m, 0.9m, 0.45m, 0.45m),
		[60] = (0.6m, 1.6m, 0.6m, 0.6m),
		[240] = (1.3m, 3.5m, 1.3m, 1.3m),
		[5] = (0.15m, 0.45m, 0.15m, 0.15m)
	};

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal _longStop;
	private decimal _longTarget;
	private decimal _shortStop;
	private decimal _shortTarget;
	private decimal _trailingStop;

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Start date for trading.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// ATR stop multiplier.
	/// </summary>
	public decimal AtrStopMult
	{
		get => _atrStopMult.Value;
		set => _atrStopMult.Value = value;
	}

	/// <summary>
	/// ATR profit target multiplier.
	/// </summary>
	public decimal AtrProfitMult
	{
		get => _atrProfitMult.Value;
		set => _atrProfitMult.Value = value;
	}

	/// <summary>
	/// Enable dynamic trailing stop.
	/// </summary>
	public bool EnableTrailingStop
	{
		get => _enableTrailingStop.Value;
		set => _enableTrailingStop.Value = value;
	}

	/// <summary>
	/// ATR trailing stop multiplier.
	/// </summary>
	public decimal AtrTrailMult
	{
		get => _atrTrailMult.Value;
		set => _atrTrailMult.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="FlexAtrStrategy"/>.
	/// </summary>
	public FlexAtrStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Trading starts from", "General");

		_atrStopMult = Param(nameof(AtrStopMult), 3m)
			.SetDisplay("ATR Stop Mult", "ATR multiplier for stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_atrProfitMult = Param(nameof(AtrProfitMult), 1.5m)
			.SetDisplay("ATR Profit Mult", "ATR multiplier for target", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_enableTrailingStop = Param(nameof(EnableTrailingStop), true)
			.SetDisplay("Enable Trailing", "Use trailing stop", "Risk");

		_atrTrailMult = Param(nameof(AtrTrailMult), 1m)
			.SetDisplay("ATR Trail Mult", "ATR multiplier for trailing", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);
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

		_prevFast = null;
		_prevSlow = null;
		_longStop = 0m;
		_longTarget = 0m;
		_shortStop = 0m;
		_shortTarget = 0m;
		_trailingStop = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var tf = (TimeSpan)CandleType.Arg;
		var minutes = tf.TotalMinutes;
		var intraday = minutes < 1440;
		var barsPerDay = intraday ? (decimal)(1440 / minutes) : 1m;

		(var fastDays, var slowDays, var rsiDays, var atrDays) = _paramSets.TryGetValue(minutes, out var s) ? s : _paramSets[1440];

		var fastPeriod = (int)Math.Round(intraday ? fastDays * barsPerDay : fastDays);
		var slowPeriod = (int)Math.Round(intraday ? slowDays * barsPerDay : slowDays);
		var rsiPeriod = (int)Math.Round(intraday ? rsiDays * barsPerDay : rsiDays);
		var atrPeriod = (int)Math.Round(intraday ? atrDays * barsPerDay : atrDays);

		_emaFast = new ExponentialMovingAverage { Length = fastPeriod };
		_emaSlow = new ExponentialMovingAverage { Length = slowPeriod };
		_rsi = new RelativeStrengthIndex { Length = rsiPeriod };
		_atr = new AverageTrueRange { Length = atrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaFast, _emaSlow, _rsi, _atr, ProcessCandle)
			.Start();

		StartProtection(new Unit(3m, UnitTypes.Percent), new Unit(3m, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.OpenTime < StartDate)
			return;

		if (!_emaFast.IsFormed || !_emaSlow.IsFormed || !_rsi.IsFormed || !_atr.IsFormed)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow && rsi > 50m;
		var crossDown = _prevFast >= _prevSlow && fast < slow && rsi < 50m;

		if (crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_longStop = candle.ClosePrice - atr * AtrStopMult;
			_longTarget = candle.ClosePrice + atr * AtrProfitMult;
			_trailingStop = candle.ClosePrice - atr * AtrTrailMult;
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_shortStop = candle.ClosePrice + atr * AtrStopMult;
			_shortTarget = candle.ClosePrice - atr * AtrProfitMult;
			_trailingStop = candle.ClosePrice + atr * AtrTrailMult;
		}

		if (Position > 0)
		{
			if (EnableTrailingStop)
			{
				var newStop = candle.ClosePrice - atr * AtrTrailMult;
				if (newStop > _trailingStop)
					_trailingStop = newStop;

				if (candle.ClosePrice <= _trailingStop)
					SellMarket(Position);
				else if (candle.ClosePrice >= _longTarget)
					SellMarket(Position);
			}
			else
			{
				if (candle.ClosePrice <= _longStop || candle.ClosePrice >= _longTarget)
					SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			var absPos = Math.Abs(Position);
			if (EnableTrailingStop)
			{
				var newStop = candle.ClosePrice + atr * AtrTrailMult;
				if (newStop < _trailingStop)
					_trailingStop = newStop;

				if (candle.ClosePrice >= _trailingStop)
					BuyMarket(absPos);
				else if (candle.ClosePrice <= _shortTarget)
					BuyMarket(absPos);
			}
			else
			{
				if (candle.ClosePrice >= _shortStop || candle.ClosePrice <= _shortTarget)
					BuyMarket(absPos);
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}