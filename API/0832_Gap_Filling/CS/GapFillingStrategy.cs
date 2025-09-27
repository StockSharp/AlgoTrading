using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades price gaps between sessions.
/// </summary>
public class GapFillingStrategy : Strategy
{
	public enum CloseWhenOption
	{
		NewSession,
		NewGap,
		ReversePosition
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _invert;
	private readonly StrategyParam<CloseWhenOption> _closeWhen;

	private decimal _prevOpen;
	private decimal _prevClose;
	private decimal _prevHigh;
	private decimal _prevLow;
	private DateTime _prevDate;
	private bool _hasPrev;
	private decimal _limitPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public bool Invert { get => _invert.Value; set => _invert.Value = value; }
	public CloseWhenOption CloseWhen { get => _closeWhen.Value; set => _closeWhen.Value = value; }

	public GapFillingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for analysis", "General");

		_invert = Param(nameof(Invert), false)
			.SetDisplay("Invert", "Trade with gap direction", "Strategy");

		_closeWhen = Param(nameof(CloseWhen), CloseWhenOption.NewSession)
			.SetDisplay("Close when", "Condition to close open positions", "Strategy");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOpen = _prevClose = _prevHigh = _prevLow = _limitPrice = 0m;
		_prevDate = default;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType).Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var date = candle.OpenTime.Date;

		var isNewSession = _hasPrev && date != _prevDate;

		var upGap = false;
		var dnGap = false;

		if (_hasPrev)
		{
			upGap = open > _prevHigh && Math.Min(close, open) > Math.Max(_prevClose, _prevOpen);
			dnGap = open < _prevLow && Math.Min(_prevClose, _prevOpen) > Math.Max(close, open);
		}

		if (isNewSession && (upGap || dnGap))
			_limitPrice = upGap ? Math.Max(_prevClose, _prevOpen) : Math.Min(_prevClose, _prevOpen);

		if (CloseWhen == CloseWhenOption.NewSession && isNewSession ||
			CloseWhen == CloseWhenOption.NewGap && isNewSession && (upGap || dnGap))
		{
			CloseAll();
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdatePrev(open, close, high, low, date);
			return;
		}

		if (isNewSession)
		{
			if (Invert)
			{
				if (upGap && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
				else if (dnGap && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			}
			else
			{
				if (dnGap && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
				else if (upGap && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		if (Position > 0)
		{
			if (Invert)
			{
				if (low <= _limitPrice)
				SellMarket(Position);
			}
			else
			{
				if (high >= _limitPrice)
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			if (Invert)
			{
				if (high >= _limitPrice)
				BuyMarket(-Position);
			}
			else
			{
				if (low <= _limitPrice)
				BuyMarket(-Position);
			}
		}

		UpdatePrev(open, close, high, low, date);
	}

	private void UpdatePrev(decimal open, decimal close, decimal high, decimal low, DateTime date)
	{
		_prevOpen = open;
		_prevClose = close;
		_prevHigh = high;
		_prevLow = low;
		_prevDate = date;
		_hasPrev = true;
	}

	private void CloseAll()
	{
		CancelActiveOrders();
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}
}
