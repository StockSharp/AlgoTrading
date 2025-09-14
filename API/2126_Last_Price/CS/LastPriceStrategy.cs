using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that places limit orders when last trade price moves away
/// from best bid or ask by a configurable interval.
/// </summary>
public class LastPriceStrategy : Strategy
{
	private readonly StrategyParam<decimal> _interval;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _spread;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPips;

	private decimal _bestAsk;
	private decimal _bestBid;
	private decimal _lastPrice;
	private decimal _lastVolume;
	private decimal _tradePrice;
	private decimal _intervalPrice;
	private decimal _stopLossPrice;

	public decimal Interval { get => _interval.Value; set => _interval.Value = value; }
	public decimal MinVolume { get => _minVolume.Value; set => _minVolume.Value = value; }
	public decimal MaxVolume { get => _maxVolume.Value; set => _maxVolume.Value = value; }
	public decimal Spread { get => _spread.Value; set => _spread.Value = value; }
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	public decimal StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }

	public LastPriceStrategy()
	{
		_interval = Param(nameof(Interval), 400m)
			.SetGreaterThanZero()
			.SetDisplay("Interval", "Distance from bid/ask to trigger order in points", "General")
			.SetCanOptimize(true);

		_minVolume = Param(nameof(MinVolume), 1m)
			.SetDisplay("Min Volume", "Minimum trade volume", "General")
			.SetCanOptimize(true);

		_maxVolume = Param(nameof(MaxVolume), 900000m)
			.SetDisplay("Max Volume", "Maximum trade volume", "General")
			.SetCanOptimize(true);

		_spread = Param(nameof(Spread), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Spread", "Maximum allowed spread", "General")
			.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 400m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Protection");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, DataType.Level1),
			(Security, DataType.Ticks)
		];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_bestAsk = _bestBid = _lastPrice = _lastVolume = _tradePrice = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var step = Security.PriceStep ?? 1m;
		_intervalPrice = Interval * step;
		_stopLossPrice = StopLossPips * step;

		StartProtection(stopLoss: new Unit(_stopLossPrice, UnitTypes.Absolute));

		SubscribeLevel1().Bind(ProcessLevel1).Start();
		SubscribeTrades().Bind(ProcessTrade).Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_bestAsk = (decimal)ask;

		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_bestBid = (decimal)bid;
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		_lastPrice = trade.TradePrice ?? 0m;
		_lastVolume = trade.Volume ?? 0m;

		Evaluate(trade.ServerTime);
	}

	private void Evaluate(DateTimeOffset time)
	{
		if (!CheckTradingTime(time))
		{
			if (Position > 0)
				SellMarket(Volume);
			else if (Position < 0)
				BuyMarket(Volume);

			return;
		}

		if (Position == 0)
		{
			var spread = _bestAsk - _bestBid;
			if (_lastVolume >= MinVolume && _lastVolume <= MaxVolume && spread <= Spread)
			{
				if (_lastPrice >= _bestAsk + _intervalPrice)
				{
					BuyLimit(_bestAsk, Volume);
					_tradePrice = _bestAsk;
				}
				else if (_lastPrice <= _bestBid - _intervalPrice)
				{
					SellLimit(_bestBid, Volume);
					_tradePrice = _bestBid;
				}
			}
		}
		else if (Position > 0)
		{
			if (_lastPrice <= _bestBid)
				SellMarket(Volume);
		}
		else
		{
			if (_lastPrice >= _bestAsk)
				BuyMarket(Volume);
		}
	}

	private static bool InRange(int value, int start, int end) => value >= start && value < end;

	private bool CheckTradingTime(DateTimeOffset time)
	{
		var day = time.DayOfWeek;
		if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
			return false;

		var seconds = time.Hour * 3600 + time.Minute * 60 + time.Second;
		if (seconds < 10 * 3600)
			return false;

		return
			InRange(seconds, 10 * 3600 + 5 * 60 + 40, 13 * 3600 + 54 * 60 + 30) ||
			InRange(seconds, 14 * 3600 + 8 * 60 + 30, 15 * 3600 + 44 * 60 + 30) ||
			InRange(seconds, 16 * 3600 + 5 * 60 + 30, 18 * 3600 + 39 * 60 + 30) ||
			InRange(seconds, 19 * 3600 + 15 * 60 + 10, 23 * 3600 + 44 * 60 + 30);
	}
}
