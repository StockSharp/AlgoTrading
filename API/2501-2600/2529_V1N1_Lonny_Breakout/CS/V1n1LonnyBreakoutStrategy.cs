using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that mirrors the original "V1N1 LONNY" MQL expert advisor.
/// The strategy forms an opening range from early candles and
/// enters when a candle closes outside that range while trend and momentum filters agree.
/// </summary>
public class V1n1LonnyBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rangeBars;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private RelativeStrengthIndex _rsi;

	private decimal _prevEma;
	private decimal _prevPrevEma;
	private bool _hasPrevEma;
	private bool _hasPrevPrevEma;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private bool _rangeReady;
	private decimal _rangeHigh;
	private decimal _rangeLow;
	private bool _breakoutUpSeen;
	private bool _breakoutDownSeen;

	/// <summary>
	/// EMA period for the trend filter.
	/// </summary>
	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
	}

	/// <summary>
	/// RSI period for momentum filter.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Number of initial bars to build the opening range.
	/// </summary>
	public int RangeBars
	{
		get => _rangeBars.Value;
		set => _rangeBars.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="V1n1LonnyBreakoutStrategy"/> class.
	/// </summary>
	public V1n1LonnyBreakoutStrategy()
	{
		_trendPeriod = Param(nameof(TrendPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Trend EMA", "EMA period for trend filter", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period for momentum filter", "Indicators");

		_rangeBars = Param(nameof(RangeBars), 5)
			.SetGreaterThanZero()
			.SetDisplay("Range Bars", "Bars used to build the opening range", "Breakout");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type and timeframe", "General");
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

		_ema = null;
		_rsi = null;
		_prevEma = 0m;
		_prevPrevEma = 0m;
		_hasPrevEma = false;
		_hasPrevPrevEma = false;

		_highs.Clear();
		_lows.Clear();
		_rangeReady = false;
		_rangeHigh = 0m;
		_rangeLow = 0m;
		_breakoutUpSeen = false;
		_breakoutDownSeen = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = TrendPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		Indicators.Add(_ema);
		Indicators.Add(_rsi);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rsiResult = _rsi.Process(new DecimalIndicatorValue(_rsi, candle.ClosePrice, candle.OpenTime) { IsFinal = true });

		if (!_rsi.IsFormed || !_ema.IsFormed)
		{
			ShiftEma(emaValue);
			return;
		}

		var rsiValue = rsiResult.ToDecimal();

		// Build the opening range from the first N bars
		if (!_rangeReady)
		{
			_highs.Add(candle.HighPrice);
			_lows.Add(candle.LowPrice);

			if (_highs.Count >= RangeBars)
			{
				_rangeHigh = decimal.MinValue;
				_rangeLow = decimal.MaxValue;

				for (var i = 0; i < _highs.Count; i++)
				{
					if (_highs[i] > _rangeHigh) _rangeHigh = _highs[i];
					if (_lows[i] < _rangeLow) _rangeLow = _lows[i];
				}

				_rangeReady = true;
			}

			ShiftEma(emaValue);
			return;
		}

		if (Position != 0 || !_hasPrevEma || !_hasPrevPrevEma)
		{
			ShiftEma(emaValue);
			return;
		}

		// Trend rising: EMA going up
		var trendUp = _prevEma > _prevPrevEma;
		var trendDown = _prevEma < _prevPrevEma;

		// Long: close above range high + trend up + RSI not overbought
		if (!_breakoutUpSeen && trendUp && rsiValue < 70 && candle.ClosePrice > _rangeHigh)
		{
			BuyMarket();
			_breakoutUpSeen = true;
		}
		// Short: close below range low + trend down + RSI not oversold
		else if (!_breakoutDownSeen && trendDown && rsiValue > 30 && candle.ClosePrice < _rangeLow)
		{
			SellMarket();
			_breakoutDownSeen = true;
		}

		ShiftEma(emaValue);
	}

	private void ShiftEma(decimal emaValue)
	{
		if (_hasPrevEma)
		{
			_prevPrevEma = _prevEma;
			_hasPrevPrevEma = true;
		}

		_prevEma = emaValue;
		_hasPrevEma = true;
	}
}
