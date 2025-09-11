using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Opening Range Breakout strategy with EMA, VWAP and ATR filters.
/// </summary>
public class TotOrbTitanStrategy : Strategy
{
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<int> _sessionEndHour;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _tpMultiplier;
	private readonly StrategyParam<int> _emaFastLength;
	private readonly StrategyParam<int> _emaSlowLength;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _emaFast;
	private EMA _emaSlow;
	private ATR _atr;
	private VWAP _vwap;

	private decimal? _orbHigh;
	private decimal? _orbLow;
	private bool _orbSet;
	private int _tradeCount;
	private DateTime _currentDate;
	private decimal? _stop;
	private decimal? _take;

	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	public int StartMinute { get => _startMinute.Value; set => _startMinute.Value = value; }
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }
	public int EndMinute { get => _endMinute.Value; set => _endMinute.Value = value; }
	public int SessionEndHour { get => _sessionEndHour.Value; set => _sessionEndHour.Value = value; }
	public int MaxTradesPerDay { get => _maxTrades.Value; set => _maxTrades.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal TpMultiplier { get => _tpMultiplier.Value; set => _tpMultiplier.Value = value; }
	public int EmaFastLength { get => _emaFastLength.Value; set => _emaFastLength.Value = value; }
	public int EmaSlowLength { get => _emaSlowLength.Value; set => _emaSlowLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TotOrbTitanStrategy()
	{
		_startHour = Param(nameof(StartHour), 8);
		_startMinute = Param(nameof(StartMinute), 30);
		_endHour = Param(nameof(EndHour), 8);
		_endMinute = Param(nameof(EndMinute), 45);
		_sessionEndHour = Param(nameof(SessionEndHour), 15);
		_maxTrades = Param(nameof(MaxTradesPerDay), 2);
		_atrLength = Param(nameof(AtrLength), 14);
		_tpMultiplier = Param(nameof(TpMultiplier), 3m);
		_emaFastLength = Param(nameof(EmaFastLength), 9);
		_emaSlowLength = Param(nameof(EmaSlowLength), 21);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
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
		_orbHigh = null;
		_orbLow = null;
		_orbSet = false;
		_tradeCount = 0;
		_stop = null;
		_take = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaFast = new EMA { Length = EmaFastLength };
		_emaSlow = new EMA { Length = EmaSlowLength };
		_atr = new ATR { Length = AtrLength };
		_vwap = new VWAP();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_emaFast, _emaSlow, _atr, _vwap, Process).Start();
	}

	private void Process(ICandleMessage candle, decimal emaFast, decimal emaSlow, decimal atr, decimal vwap)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime.Date;
		if (date != _currentDate)
		{
			_currentDate = date;
			_orbHigh = null;
			_orbLow = null;
			_orbSet = false;
			_tradeCount = 0;
			_stop = null;
			_take = null;
			_vwap.Reset();
		}

		if (!_emaFast.IsFormed || !_emaSlow.IsFormed || !_atr.IsFormed || !_vwap.IsFormed)
			return;

		var start = new TimeSpan(StartHour, StartMinute, 0);
		var end = new TimeSpan(EndHour, EndMinute, 0);
		var sessionEnd = new TimeSpan(SessionEndHour, 0, 0);
		var time = candle.OpenTime.TimeOfDay;

		if (time >= start && time < end)
		{
			_orbHigh = _orbHigh is null ? candle.HighPrice : Math.Max(_orbHigh.Value, candle.HighPrice);
			_orbLow = _orbLow is null ? candle.LowPrice : Math.Min(_orbLow.Value, candle.LowPrice);
		}

		if (time >= end && !_orbSet && _orbHigh.HasValue && _orbLow.HasValue)
			_orbSet = true;

		var inRth = time >= start && time < sessionEnd;
		if (!inRth || !_orbSet)
			return;

		var opensInside = candle.OpenPrice <= _orbHigh && candle.OpenPrice >= _orbLow;
		var closesAbove = candle.ClosePrice > _orbHigh;
		var closesBelow = candle.ClosePrice < _orbLow;

		var longCond = opensInside && closesAbove && vwap < emaSlow && emaSlow < emaFast;
		var shortCond = opensInside && closesBelow && vwap > emaSlow && emaSlow > emaFast;

		if (longCond && Position == 0 && _tradeCount < MaxTradesPerDay)
		{
			BuyMarket(Volume);
			_stop = candle.ClosePrice - atr;
			_take = candle.ClosePrice + atr * TpMultiplier;
			_tradeCount++;
		}
		else if (shortCond && Position == 0 && _tradeCount < MaxTradesPerDay)
		{
			SellMarket(Volume);
			_stop = candle.ClosePrice + atr;
			_take = candle.ClosePrice - atr * TpMultiplier;
			_tradeCount++;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stop || candle.HighPrice >= _take)
			{
				SellMarket(Math.Abs(Position));
				_stop = null;
				_take = null;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stop || candle.LowPrice <= _take)
			{
				BuyMarket(Math.Abs(Position));
				_stop = null;
				_take = null;
			}
		}
	}
}

