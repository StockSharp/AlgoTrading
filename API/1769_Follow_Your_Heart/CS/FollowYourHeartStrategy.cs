using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy summing relative changes of open, close, high and low prices.
/// </summary>
public class FollowYourHeartStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bars;
	private readonly StrategyParam<decimal> _level;
	private readonly StrategyParam<decimal> _profitBuy;
	private readonly StrategyParam<decimal> _profitSell;
	private readonly StrategyParam<decimal> _lossBuy;
	private readonly StrategyParam<decimal> _lossSell;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<bool> _tradingHoursOn;
	private readonly StrategyParam<int> _openHourBuy;
	private readonly StrategyParam<int> _closeHourBuy;
	private readonly StrategyParam<int> _openHourSell;
	private readonly StrategyParam<int> _closeHourSell;

	private SimpleMovingAverage _openSma = null!;
	private SimpleMovingAverage _closeSma = null!;
	private SimpleMovingAverage _highSma = null!;
	private SimpleMovingAverage _lowSma = null!;

	private decimal _prevOpen;
	private decimal _prevClose;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _isFirst = true;
	private decimal _entryPrice;

	public FollowYourHeartStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_bars = Param(nameof(Bars), 6)
			.SetGreaterThanZero()
			.SetDisplay("Bars", "Number of bars to sum", "Parameters");

		_level = Param(nameof(Level), 2.3m)
			.SetDisplay("Level", "Threshold for changes", "Parameters");

		_profitBuy = Param(nameof(ProfitBuy), 75m)
			.SetDisplay("Profit Buy", "Money profit target", "Risk");
		_profitSell = Param(nameof(ProfitSell), 56m)
			.SetDisplay("Profit Sell", "Money profit target", "Risk");
		_lossBuy = Param(nameof(LossBuy), -54m)
			.SetDisplay("Loss Buy", "Money loss limit", "Risk");
		_lossSell = Param(nameof(LossSell), -51m)
			.SetDisplay("Loss Sell", "Money loss limit", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 550)
			.SetGreaterThanZero()
			.SetDisplay("TP", "Take profit in points", "Risk");
		_stopLoss = Param(nameof(StopLoss), 550)
			.SetGreaterThanZero()
			.SetDisplay("SL", "Stop loss in points", "Risk");

		_tradingHoursOn = Param(nameof(TradingHoursOn), true)
			.SetDisplay("Trading Hours On", "Enable session filter", "Time");
		_openHourBuy = Param(nameof(OpenHourBuy), 6)
			.SetDisplay("Open Hour Buy", "Start hour for buys", "Time");
		_closeHourBuy = Param(nameof(CloseHourBuy), 12)
			.SetDisplay("Close Hour Buy", "End hour for buys", "Time");
		_openHourSell = Param(nameof(OpenHourSell), 4)
			.SetDisplay("Open Hour Sell", "Start hour for sells", "Time");
		_closeHourSell = Param(nameof(CloseHourSell), 10)
			.SetDisplay("Close Hour Sell", "End hour for sells", "Time");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Bars { get => _bars.Value; set => _bars.Value = value; }
	public decimal Level { get => _level.Value; set => _level.Value = value; }
	public decimal ProfitBuy { get => _profitBuy.Value; set => _profitBuy.Value = value; }
	public decimal ProfitSell { get => _profitSell.Value; set => _profitSell.Value = value; }
	public decimal LossBuy { get => _lossBuy.Value; set => _lossBuy.Value = value; }
	public decimal LossSell { get => _lossSell.Value; set => _lossSell.Value = value; }
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public bool TradingHoursOn { get => _tradingHoursOn.Value; set => _tradingHoursOn.Value = value; }
	public int OpenHourBuy { get => _openHourBuy.Value; set => _openHourBuy.Value = value; }
	public int CloseHourBuy { get => _closeHourBuy.Value; set => _closeHourBuy.Value = value; }
	public int OpenHourSell { get => _openHourSell.Value; set => _openHourSell.Value = value; }
	public int CloseHourSell { get => _closeHourSell.Value; set => _closeHourSell.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_openSma = new SimpleMovingAverage { Length = Bars };
		_closeSma = new SimpleMovingAverage { Length = Bars };
		_highSma = new SimpleMovingAverage { Length = Bars };
		_lowSma = new SimpleMovingAverage { Length = Bars };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirst)
		{
			_prevOpen = candle.OpenPrice;
			_prevClose = candle.ClosePrice;
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_isFirst = false;
			return;
		}

		var step = Security.PriceStep ?? 1m;
		var stepPrice = Security.StepPrice ?? step;

		var deltaOpen = _prevOpen == 0m ? 0m : (candle.OpenPrice - _prevOpen) / _prevOpen / step;
		var deltaClose = _prevClose == 0m ? 0m : (candle.ClosePrice - _prevClose) / _prevClose / step;
		var deltaHigh = _prevHigh == 0m ? 0m : (candle.HighPrice - _prevHigh) / _prevHigh / step;
		var deltaLow = _prevLow == 0m ? 0m : (candle.LowPrice - _prevLow) / _prevLow / step;

		_openSma.Process(deltaOpen);
		_closeSma.Process(deltaClose);
		_highSma.Process(deltaHigh);
		_lowSma.Process(deltaLow);

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			var profitMoney = (candle.ClosePrice - _entryPrice) / step * stepPrice * Position;

			if (Position > 0)
			{
				var profitPoints = (candle.ClosePrice - _entryPrice) / step;
				if (profitMoney > ProfitBuy || profitMoney < LossBuy || profitPoints >= TakeProfit || profitPoints <= -StopLoss)
				{
					SellMarket(Position);
					_entryPrice = 0m;
				}
			}
			else
			{
				var profitPoints = (_entryPrice - candle.ClosePrice) / step;
				if (profitMoney > ProfitSell || profitMoney < LossSell || profitPoints >= TakeProfit || profitPoints <= -StopLoss)
				{
					BuyMarket(-Position);
					_entryPrice = 0m;
				}
			}

			return;
		}

		if (!_openSma.IsFormed || !_closeSma.IsFormed || !_highSma.IsFormed || !_lowSma.IsFormed)
			return;

		var o = _openSma.GetCurrentValue() * Bars;
		var c = _closeSma.GetCurrentValue() * Bars;
		var h = _highSma.GetCurrentValue() * Bars;
		var l = _lowSma.GetCurrentValue() * Bars;
		var sum = (o + c + h + l) / 4m;

		var time = candle.OpenTime;

		if (TradingHoursOn)
		{
			if (sum > 0)
			{
				if (time.Hour < OpenHourBuy || time.Hour > CloseHourBuy)
					return;
			}
			else if (sum < 0)
			{
				if (time.Hour < OpenHourSell || time.Hour > CloseHourSell)
					return;
			}
		}

		if (sum > 0 && o > Level && c > Level && c > o)
		{
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
		}
		else if (sum < 0 && o < -Level && c < -Level && c < o)
		{
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
		}
	}
}
