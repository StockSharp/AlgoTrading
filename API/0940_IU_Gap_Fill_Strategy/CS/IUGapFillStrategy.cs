namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trades when a session gap of a given size is filled.
/// </summary>
public class IUGapFillStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gapPercent;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private decimal _lastSessionClose;
	private bool _gapUp;
	private bool _gapDown;
	private bool _validGap;
	private bool _isFirstBar;
	private decimal? _atrTsl;

	/// <summary>
	/// Percentage difference for a valid gap.
	/// </summary>
	public decimal GapPercent
	{
		get => _gapPercent.Value;
		set => _gapPercent.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for trailing stop.
	/// </summary>
	public decimal AtrFactor
	{
		get => _atrFactor.Value;
		set => _atrFactor.Value = value;
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
	/// Initializes a new instance of the <see cref="IUGapFillStrategy"/> class.
	/// </summary>
	public IUGapFillStrategy()
	{
		_gapPercent = Param(nameof(GapPercent), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Gap %", "Minimum percentage gap", "General");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation period", "ATR")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_atrFactor = Param(nameof(AtrFactor), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Factor", "ATR multiplier", "ATR")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

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
		_currentDay = default;
		_lastSessionClose = 0m;
		_gapUp = false;
		_gapDown = false;
		_validGap = false;
		_isFirstBar = false;
		_atrTsl = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var atr = new AverageTrueRange { Length = AtrLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.UtcDateTime.Date;

		if (_currentDay != day)
		{
			_currentDay = day;

			_gapUp = candle.OpenPrice > _lastSessionClose;
			_gapDown = candle.OpenPrice < _lastSessionClose;
			_validGap = Math.Abs(_lastSessionClose - candle.OpenPrice) >= candle.OpenPrice * GapPercent / 100m;
			_isFirstBar = true;
		}

		if (_isFirstBar)
		{
			_isFirstBar = false;
		}
		else if (_validGap && Position == 0)
		{
			if (_gapUp && candle.LowPrice < _lastSessionClose && candle.ClosePrice > _lastSessionClose)
				BuyMarket();
			else if (_gapDown && candle.HighPrice > _lastSessionClose && candle.ClosePrice < _lastSessionClose)
				SellMarket();
		}

		if (Position > 0)
		{
			if (_atrTsl is null)
				_atrTsl = Position.AveragePrice - atr * AtrFactor;
			else
				_atrTsl = Math.Max(_atrTsl.Value, candle.ClosePrice - atr * AtrFactor);

			if (_atrTsl.HasValue && candle.LowPrice <= _atrTsl)
			{
				SellMarket(Position);
				_atrTsl = null;
			}
		}
		else if (Position < 0)
		{
			if (_atrTsl is null)
				_atrTsl = Position.AveragePrice + atr * AtrFactor;
			else
				_atrTsl = Math.Min(_atrTsl.Value, candle.ClosePrice + atr * AtrFactor);

			if (_atrTsl.HasValue && candle.HighPrice >= _atrTsl)
			{
				BuyMarket(Math.Abs(Position));
				_atrTsl = null;
			}
		}
		else
		{
			_atrTsl = null;
		}

		_lastSessionClose = candle.ClosePrice;
	}
}
