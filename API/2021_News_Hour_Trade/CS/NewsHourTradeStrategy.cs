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
	private readonly StrategyParam<bool> _buyTrade;
	private readonly StrategyParam<bool> _sellTrade;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _lastTradeDay;
	private decimal _tickSize;
	private bool _waitingForBreakout;
	private decimal _referencePrice;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	public int StartMinute { get => _startMinute.Value; set => _startMinute.Value = value; }
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int PriceGap { get => _priceGap.Value; set => _priceGap.Value = value; }
	public bool BuyTrade { get => _buyTrade.Value; set => _buyTrade.Value = value; }
	public bool SellTrade { get => _sellTrade.Value; set => _sellTrade.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NewsHourTradeStrategy()
	{
		_startHour = Param(nameof(StartHour), 0).SetDisplay("Start Hour", "Hour to start", "Parameters");
		_startMinute = Param(nameof(StartMinute), 0).SetDisplay("Start Minute", "Minute to start", "Parameters");
		_stopLoss = Param(nameof(StopLoss), 500).SetDisplay("Stop Loss", "Stop distance in steps", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 1000).SetDisplay("Take Profit", "Take profit distance in steps", "Risk");
		_priceGap = Param(nameof(PriceGap), 100).SetDisplay("Price Gap", "Price offset in steps", "Parameters");
		_buyTrade = Param(nameof(BuyTrade), true).SetDisplay("Buy Trade", "Enable buys", "Parameters");
		_sellTrade = Param(nameof(SellTrade), true).SetDisplay("Sell Trade", "Enable sells", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Working timeframe", "Parameters");
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
		_waitingForBreakout = false;
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
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime.Date;

		// At news time, record reference price and start watching for breakout
		if (date != _lastTradeDay && candle.OpenTime.Hour == StartHour && candle.OpenTime.Minute >= StartMinute && Position == 0)
		{
			_lastTradeDay = date;
			_referencePrice = candle.ClosePrice;
			_waitingForBreakout = true;
			return;
		}

		// Check for breakout entry
		if (_waitingForBreakout && Position == 0)
		{
			var offset = PriceGap * _tickSize;

			if (BuyTrade && candle.ClosePrice > _referencePrice + offset)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLoss * _tickSize;
				_takePrice = _entryPrice + TakeProfit * _tickSize;
				_waitingForBreakout = false;
			}
			else if (SellTrade && candle.ClosePrice < _referencePrice - offset)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLoss * _tickSize;
				_takePrice = _entryPrice - TakeProfit * _tickSize;
				_waitingForBreakout = false;
			}
		}

		// Manage open position with SL/TP
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				BuyMarket();
		}
	}
}
