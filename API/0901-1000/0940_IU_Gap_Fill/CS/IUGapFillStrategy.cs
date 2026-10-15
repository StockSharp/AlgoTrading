using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades when a session gap of a given size is filled.
/// </summary>
public class IUGapFillStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gapPercent;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<int> _cooldownDays;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private decimal _lastSessionClose;
	private decimal _prevDayClose;
	private bool _gapUp;
	private bool _gapDown;
	private bool _validGap;
	private bool _isFirstBar;
	private DateTime _entryDay;
	private DateTime _nextEntryDate;

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
	/// Minimum number of days between entries.
	/// </summary>
	public int CooldownDays
	{
		get => _cooldownDays.Value;
		set => _cooldownDays.Value = value;
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
		_gapPercent = Param(nameof(GapPercent), 0.01m)
			.SetDisplay("Gap %", "Minimum percentage gap.", "General");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR calculation period.", "ATR");

		_atrFactor = Param(nameof(AtrFactor), 2m)
			.SetDisplay("ATR Factor", "ATR multiplier.", "ATR");

		_cooldownDays = Param(nameof(CooldownDays), 1)
			.SetDisplay("Cooldown Days", "Minimum days between entries.", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use.", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_currentDay = default;
		_lastSessionClose = 0m;
		_prevDayClose = 0m;
		_gapUp = false;
		_gapDown = false;
		_validGap = false;
		_isFirstBar = false;
		_entryDay = default;
		_nextEntryDate = DateTime.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_currentDay = default;
		_lastSessionClose = 0m;
		_prevDayClose = 0m;
		_gapUp = false;
		_gapDown = false;
		_validGap = false;
		_isFirstBar = false;
		_entryDay = default;
		_nextEntryDate = DateTime.MinValue;

		var dummyEma1 = new ExponentialMovingAverage { Length = 10 };
		var dummyEma2 = new ExponentialMovingAverage { Length = 20 };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(dummyEma1, dummyEma2, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal d1, decimal d2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;

		if (_currentDay != day)
		{
			_prevDayClose = _lastSessionClose;
			_currentDay = day;

			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();

			if (_prevDayClose > 0)
			{
				_gapUp = candle.OpenPrice > _prevDayClose;
				_gapDown = candle.OpenPrice < _prevDayClose;
				_validGap = Math.Abs(_prevDayClose - candle.OpenPrice) >= candle.OpenPrice * GapPercent / 100m;
			}
			_isFirstBar = true;
		}

		_lastSessionClose = candle.ClosePrice;

		if (_isFirstBar)
		{
			_isFirstBar = false;
		}
		else if (_validGap && Position == 0 && day >= _nextEntryDate)
		{
			// Gap fill logic: price returns to previous close level
			if (_gapUp && candle.ClosePrice <= _prevDayClose)
			{
				BuyMarket();
				_entryDay = day;
				_nextEntryDate = day.AddDays(CooldownDays);
			}
			else if (_gapDown && candle.ClosePrice >= _prevDayClose)
			{
				SellMarket();
				_entryDay = day;
				_nextEntryDate = day.AddDays(CooldownDays);
			}
		}
	}
}
