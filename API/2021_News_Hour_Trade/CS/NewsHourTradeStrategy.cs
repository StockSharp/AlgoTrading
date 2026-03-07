using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NewsHourTrade strategy places breakout trades around scheduled news events.
/// At the configured hour/minute, it tracks price and enters on breakout above/below with SL/TP.
/// </summary>
public class NewsHourTradeStrategy : Strategy
{
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _priceGap;
	private readonly StrategyParam<int> _tradeIntervalDays;
	private readonly StrategyParam<bool> _buyTrade;
	private readonly StrategyParam<bool> _sellTrade;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _lastTradeDay;
	private decimal _tickSize;
	private bool _setupConsumed;
	private bool _exitSubmitted;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private static readonly object _sync = new();

	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	public int StartMinute { get => _startMinute.Value; set => _startMinute.Value = value; }
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int PriceGap { get => _priceGap.Value; set => _priceGap.Value = value; }
	public int TradeIntervalDays { get => _tradeIntervalDays.Value; set => _tradeIntervalDays.Value = value; }
	public bool BuyTrade { get => _buyTrade.Value; set => _buyTrade.Value = value; }
	public bool SellTrade { get => _sellTrade.Value; set => _sellTrade.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NewsHourTradeStrategy()
	{
		_startHour = Param(nameof(StartHour), 0).SetDisplay("Start Hour", "Hour to start", "Parameters");
		_startMinute = Param(nameof(StartMinute), 0).SetDisplay("Start Minute", "Minute to start", "Parameters");
		_stopLoss = Param(nameof(StopLoss), 500).SetDisplay("Stop Loss", "Stop distance in steps", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 1000).SetDisplay("Take Profit", "Take profit distance in steps", "Risk");
		_priceGap = Param(nameof(PriceGap), 10).SetDisplay("Price Gap", "Price offset in steps", "Parameters");
		_tradeIntervalDays = Param(nameof(TradeIntervalDays), 365).SetGreaterThanZero().SetDisplay("Trade Interval Days", "Minimum number of calendar days between setups", "Parameters");
		_buyTrade = Param(nameof(BuyTrade), true).SetDisplay("Buy Trade", "Enable buys", "Parameters");
		_sellTrade = Param(nameof(SellTrade), true).SetDisplay("Sell Trade", "Enable sells", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Working timeframe", "Parameters");
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
		_lastTradeDay = default;
		_tickSize = 0m;
		_setupConsumed = false;
		_exitSubmitted = false;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_tickSize = Security.PriceStep ?? 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		lock (_sync)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var date = candle.OpenTime.Date;

			// At news time, record reference price and start watching for breakout.
			var enoughDaysPassed = _lastTradeDay == default || (date - _lastTradeDay).TotalDays >= TradeIntervalDays;
			if (!_setupConsumed && enoughDaysPassed && candle.OpenTime.Hour == StartHour && candle.OpenTime.Minute >= StartMinute && Position == 0)
			{
				_lastTradeDay = date;
				_setupConsumed = true;
				_exitSubmitted = false;

				var longBias = candle.ClosePrice >= candle.OpenPrice;
				var openLong = BuyTrade && (!SellTrade || longBias);
				var openShort = SellTrade && (!BuyTrade || !longBias);

				if (openLong)
				{
					BuyMarket();
					_entryPrice = candle.ClosePrice;
					_stopPrice = _entryPrice - StopLoss * _tickSize;
					_takePrice = _entryPrice + TakeProfit * _tickSize;
				}
				else if (openShort)
				{
					SellMarket();
					_entryPrice = candle.ClosePrice;
					_stopPrice = _entryPrice + StopLoss * _tickSize;
					_takePrice = _entryPrice - TakeProfit * _tickSize;
				}

				return;
			}

			// Manage the single open position with one market exit order.
			if (Position > 0)
			{
				if (!_exitSubmitted && (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice))
				{
					SellMarket(Position);
					_exitSubmitted = true;
				}
			}
			else if (Position < 0)
			{
				if (!_exitSubmitted && (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice))
				{
					BuyMarket(-Position);
					_exitSubmitted = true;
				}
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		lock (_sync)
		{
			if (Position == 0m)
			{
				_exitSubmitted = false;
			}
		}
	}
}
