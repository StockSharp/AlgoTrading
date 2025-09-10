using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Aftershock earnings drift strategy.
/// </summary>
public class AftershockPlaybookStrategy : Strategy
{
	private readonly StrategyParam<decimal> _posSurprise;
	private readonly StrategyParam<decimal> _negSurprise;
	private readonly StrategyParam<decimal> _atrMult;
	private readonly StrategyParam<int> _atrLen;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<bool> _timeExit;
	private readonly StrategyParam<int> _holdDays;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entry;
	private DateTime _entryTime;
	private decimal _atr;
	private bool _waitNext;
	private bool _reenter;
	private int _lastDir;

	public decimal PositiveSurprise { get => _posSurprise.Value; set => _posSurprise.Value = value; }
	public decimal NegativeSurprise { get => _negSurprise.Value; set => _negSurprise.Value = value; }
	public decimal AtrMultiplier { get => _atrMult.Value; set => _atrMult.Value = value; }
	public int AtrLength { get => _atrLen.Value; set => _atrLen.Value = value; }
	public bool ReverseSignals { get => _reverse.Value; set => _reverse.Value = value; }
	public bool UseTimeExit { get => _timeExit.Value; set => _timeExit.Value = value; }
	public int HoldDays { get => _holdDays.Value; set => _holdDays.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AftershockPlaybookStrategy()
	{
		_posSurprise = Param(nameof(PositiveSurprise), 0m).SetDisplay("Positive surprise ≥ (%)", "Minimum EPS surprise for long", "General");
		_negSurprise = Param(nameof(NegativeSurprise), 0m).SetDisplay("Negative surprise ≤ (%)", "Maximum negative EPS surprise for short", "General");
		_atrMult = Param(nameof(AtrMultiplier), 2m).SetDisplay("ATR stop ×", "ATR multiplier for short stop", "Risk");
		_atrLen = Param(nameof(AtrLength), 14).SetDisplay("ATR length", "ATR lookback period", "Risk");
		_reverse = Param(nameof(ReverseSignals), false).SetDisplay("Reverse signals", "Flip long/short polarity", "General");
		_timeExit = Param(nameof(UseTimeExit), false).SetDisplay("Time exit", "Use time based exit", "Risk");
		_holdDays = Param(nameof(HoldDays), 45).SetDisplay("Hold Days", "Calendar days to hold", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame()).SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_entry = 0m;
		_entryTime = DateTime.MinValue;
		_atr = 0m;
		_waitNext = false;
		_reenter = false;
		_lastDir = 0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var atr = new Atr { Length = AtrLength };
		SubscribeCandles(CandleType).Bind(atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_atr = atr;

		if (TryGetEarningsSurprise(candle.OpenTime, out var surprise))
		{
			_waitNext = false;
			_reenter = false;

			if (Position != 0)
			{
				Exit(candle.ClosePrice, true);
				return;
			}

			var longOk = surprise >= PositiveSurprise;
			var shortOk = surprise <= NegativeSurprise;

			if (ReverseSignals)
				(longOk, shortOk) = (shortOk, longOk);

			if (longOk && !_waitNext)
			{
				BuyMarket();
				_entry = candle.ClosePrice;
				_entryTime = candle.OpenTime.DateTime;
				_lastDir = 1;
			}
			else if (shortOk && !_waitNext)
			{
				SellMarket();
				_entry = candle.ClosePrice;
				_entryTime = candle.OpenTime.DateTime;
				_lastDir = -1;
			}
		}

		if (Position < 0)
		{
			var stop = _entry + AtrMultiplier * _atr;
			if (candle.HighPrice >= stop)
			{
				Exit(stop);
				return;
			}
		}

		if (UseTimeExit && Position != 0)
		{
			var days = (candle.CloseTime.Date - _entryTime.Date).TotalDays;
			if (days >= HoldDays)
			{
				Exit(candle.ClosePrice);
				return;
			}
		}

		if (Position == 0 && _reenter && _lastDir != 0)
		{
			if (_lastDir > 0)
				BuyMarket();
			else
				SellMarket();

			_entry = candle.ClosePrice;
			_entryTime = candle.OpenTime.DateTime;
			_reenter = false;
		}
	}

	private void Exit(decimal price, bool ignore = false)
	{
		if (Position > 0)
			RegisterSell(Position);
		else if (Position < 0)
			RegisterBuy(-Position);

		if (!ignore)
		{
			var pnl = (price - _entry) * _lastDir;
			if (pnl > 0)
				_reenter = true;
			else
				_waitNext = true;
		}

		_entry = 0m;
		_entryTime = DateTime.MinValue;
	}

	private bool TryGetEarningsSurprise(DateTimeOffset date, out decimal surprise)
	{
		surprise = 0m;
		return false; // TODO
	}
}
