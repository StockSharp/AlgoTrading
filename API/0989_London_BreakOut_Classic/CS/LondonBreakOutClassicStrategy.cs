using System;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// London session breakout strategy based on Asian range.
/// </summary>
public class LondonBreakOutClassicStrategy : Strategy
{
	private readonly StrategyParam<decimal> _crv;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _boxStart;
	private readonly StrategyParam<TimeSpan> _boxEnd;
	private readonly StrategyParam<TimeSpan> _tradeStart;
	private readonly StrategyParam<TimeSpan> _tradeEnd;

	private decimal _sessionHigh;
	private decimal _sessionLow;
	private DateTime _currentDay;
	private bool _tradeOpened;
	private decimal _stopPrice;
	private decimal _takePrice;

	public decimal Crv { get => _crv.Value; set => _crv.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public TimeSpan BoxStart { get => _boxStart.Value; set => _boxStart.Value = value; }
	public TimeSpan BoxEnd { get => _boxEnd.Value; set => _boxEnd.Value = value; }
	public TimeSpan TradeStart { get => _tradeStart.Value; set => _tradeStart.Value = value; }
	public TimeSpan TradeEnd { get => _tradeEnd.Value; set => _tradeEnd.Value = value; }

	public LondonBreakOutClassicStrategy()
	{
		_crv = Param(nameof(Crv), 1m)
			.SetGreaterThanZero()
			.SetDisplay("CRV", "Risk reward factor", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
		_boxStart = Param(nameof(BoxStart), TimeSpan.Zero)
			.SetDisplay("Box Start", "Asian session start", "Session");
		_boxEnd = Param(nameof(BoxEnd), TimeSpan.FromHours(7))
			.SetDisplay("Box End", "Asian session end", "Session");
		_tradeStart = Param(nameof(TradeStart), TimeSpan.FromHours(7))
			.SetDisplay("Trade Start", "Breakout start", "Session");
		_tradeEnd = Param(nameof(TradeEnd), TimeSpan.FromHours(16))
			.SetDisplay("Trade End", "Breakout end", "Session");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sessionHigh = 0m;
		_sessionLow = decimal.MaxValue;
		_currentDay = default;
		_tradeOpened = false;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(OnProcess).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;
		if (day != _currentDay)
		{
			_currentDay = day;
			_sessionHigh = 0m;
			_sessionLow = decimal.MaxValue;
			_tradeOpened = false;

			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));
		}

		var tod = candle.OpenTime.TimeOfDay;

		// Accumulate Asian session range
		if (tod >= BoxStart && tod < BoxEnd)
		{
			_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
			_sessionLow = Math.Min(_sessionLow, candle.LowPrice);
			return;
		}

		// Trading window
		if (tod >= TradeStart && tod < TradeEnd && !_tradeOpened && _sessionHigh > 0 && _sessionLow < decimal.MaxValue)
		{
			var mid = (_sessionHigh + _sessionLow) / 2m;

			if (candle.ClosePrice > _sessionHigh && Position <= 0)
			{
				_stopPrice = mid;
				var range = candle.ClosePrice - _stopPrice;
				_takePrice = candle.ClosePrice + range * Crv;
				BuyMarket(Volume + Math.Abs(Position));
				_tradeOpened = true;
				return;
			}

			if (candle.ClosePrice < _sessionLow && Position >= 0)
			{
				_stopPrice = mid;
				var range = _stopPrice - candle.ClosePrice;
				_takePrice = candle.ClosePrice - range * Crv;
				SellMarket(Volume + Math.Abs(Position));
				_tradeOpened = true;
				return;
			}
		}

		// Exit logic
		if (Position > 0)
		{
			if (candle.ClosePrice <= _stopPrice || candle.ClosePrice >= _takePrice || tod >= TradeEnd)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice >= _stopPrice || candle.ClosePrice <= _takePrice || tod >= TradeEnd)
				BuyMarket(Math.Abs(Position));
		}
	}
}
