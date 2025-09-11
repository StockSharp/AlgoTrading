using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR based trailing stop with optional Fibonacci levels.
/// </summary>
public class SwingArmAtrTrendIndicatorStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<bool> _modified;
	private readonly StrategyParam<bool> _showFib;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr = null!;
	private decimal _trendUp;
	private decimal _trendDown;
	private int _trend = 1;
	private decimal _ex;
	private DateTimeOffset _prevTime;
	private decimal _prevTrail;
	private decimal _prevF1;
	private decimal _prevF2;
	private decimal _prevF3;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR factor.
	/// </summary>
	public decimal AtrFactor
	{
		get => _atrFactor.Value;
		set => _atrFactor.Value = value;
	}

	/// <summary>
	/// Use modified true range.
	/// </summary>
	public bool Modified
	{
		get => _modified.Value;
		set => _modified.Value = value;
	}

	/// <summary>
	/// Show Fibonacci lines.
	/// </summary>
	public bool ShowFib
	{
		get => _showFib.Value;
		set => _showFib.Value = value;
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
	/// Initializes a new instance of <see cref="SwingArmAtrTrendIndicatorStrategy"/>.
	/// </summary>
	public SwingArmAtrTrendIndicatorStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 28)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR length", "ATR");

		_atrFactor = Param(nameof(AtrFactor), 5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Factor", "ATR multiplier", "ATR");

		_modified = Param(nameof(Modified), true)
			.SetDisplay("Modified", "Use modified range", "ATR");

		_showFib = Param(nameof(ShowFib), true)
			.SetDisplay("Show Fib", "Display Fibonacci levels", "Fibonacci");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
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
		_atr = null!;
		_trendUp = 0m;
		_trendDown = 0m;
		_trend = 1;
		_ex = 0m;
		_prevTime = default;
		_prevTrail = 0m;
		_prevF1 = 0m;
		_prevF2 = 0m;
		_prevF3 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var tr = GetTrueRange(candle);
		var atrValue = _atr.Process(tr).GetCurrentValue<decimal>();
		if (!_atr.IsFormed)
			return;

		var loss = AtrFactor * atrValue;
		var up = candle.ClosePrice - loss;
		var dn = candle.ClosePrice + loss;

		_trendUp = candle.ClosePrice > _trendUp ? Math.Max(up, _trendUp) : up;
		_trendDown = candle.ClosePrice < _trendDown ? Math.Min(dn, _trendDown) : dn;

		var prevTrend = _trend;
		if (candle.ClosePrice > _trendDown)
			_trend = 1;
		else if (candle.ClosePrice < _trendUp)
			_trend = -1;

		var trail = _trend == 1 ? _trendUp : _trendDown;

		if (prevTrend <= 0 && _trend > 0)
			_ex = candle.HighPrice;
		else if (prevTrend >= 0 && _trend < 0)
			_ex = candle.LowPrice;
		else
			_ex = _trend == 1 ? Math.Max(_ex, candle.HighPrice) : Math.Min(_ex, candle.LowPrice);

		DrawLine(_prevTime, _prevTrail, candle.OpenTime, trail);
		_prevTime = candle.OpenTime;
		_prevTrail = trail;

		if (ShowFib)
		{
			var f1 = _ex + (trail - _ex) * 0.618m;
			var f2 = _ex + (trail - _ex) * 0.786m;
			var f3 = _ex + (trail - _ex) * 0.886m;
			DrawLine(_prevTime, _prevF1, candle.OpenTime, f1);
			DrawLine(_prevTime, _prevF2, candle.OpenTime, f2);
			DrawLine(_prevTime, _prevF3, candle.OpenTime, f3);
			_prevF1 = f1;
			_prevF2 = f2;
			_prevF3 = f3;
		}
	}

	private decimal GetTrueRange(ICandleMessage candle)
	{
		if (!Modified)
			return candle.HighPrice - candle.LowPrice;

		var hiLo = Math.Min(candle.HighPrice - candle.LowPrice, 1.5m * candle.Volume);
		var hRef = candle.LowPrice <= candle.HighPrice ? candle.HighPrice - candle.ClosePrice : (candle.HighPrice - candle.ClosePrice) - 0.5m * (candle.LowPrice - candle.HighPrice);
		var lRef = candle.HighPrice >= candle.LowPrice ? candle.ClosePrice - candle.LowPrice : (candle.ClosePrice - candle.LowPrice) - 0.5m * (candle.LowPrice - candle.HighPrice);
		return Math.Max(Math.Max(hiLo, hRef), lRef);
	}
}
