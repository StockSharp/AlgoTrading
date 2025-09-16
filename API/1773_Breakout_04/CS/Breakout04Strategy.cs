using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy trading previous day high/low.
/// </summary>
public class Breakout04Strategy : Strategy
{
	private readonly StrategyParam<int> _mondayHour;
	private readonly StrategyParam<int> _fridayHour;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<bool> _useMm;
	private readonly StrategyParam<decimal> _percentMm;
	private readonly StrategyParam<decimal> _volume;

	private decimal _prevHigh;
	private decimal _prevLow;
	private DateTimeOffset _lastDaily;
	private decimal _stopPrice;
	private decimal _takePrice;

	public int MondayHour { get => _mondayHour.Value; set => _mondayHour.Value = value; }
	public int FridayHour { get => _fridayHour.Value; set => _fridayHour.Value = value; }
	public int TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public bool UseMoneyManagement { get => _useMm.Value; set => _useMm.Value = value; }
	public decimal PercentMM { get => _percentMm.Value; set => _percentMm.Value = value; }
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	public Breakout04Strategy()
	{
		_mondayHour = Param(nameof(MondayHour), 18)
			.SetDisplay("Monday Hour", "Trading allowed after this hour on Monday", "General");
		_fridayHour = Param(nameof(FridayHour), 14)
			.SetDisplay("Friday Hour", "Trading stops after this hour on Friday", "General");
		_trailingStop = Param(nameof(TrailingStop), 21)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop in points", "Risk Management");
		_takeProfit = Param(nameof(TakeProfit), 550)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk Management");
		_stopLoss = Param(nameof(StopLoss), 124)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Initial stop loss in points", "Risk Management");
		_useMm = Param(nameof(UseMoneyManagement), false)
			.SetDisplay("Use MM", "Enable simple money management", "General");
		_percentMm = Param(nameof(PercentMM), 8m)
			.SetGreaterThanZero()
			.SetDisplay("Percent MM", "Risk percent of free capital", "General");
		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Ticks), (Security, TimeSpan.FromDays(1).TimeFrame())];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeTrades().Bind(ProcessTrade).Start();
		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame()).Bind(ProcessDaily).Start();

		StartProtection();
	}

	private decimal GetVolume()
	{
		if (!UseMoneyManagement)
			return Volume;

		var account = Portfolio?.CurrentValue ?? 0m;
		if (account <= 0)
			return 0m;

		var vol = PercentMM / 100m * account / 100000m;
		return vol > 0 ? vol : 0m;
	}

	private void ProcessDaily(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_lastDaily = candle.OpenTime;
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = trade.ServerTime.ToLocalTime();

		if (time.DayOfWeek == DayOfWeek.Friday && time.Hour > FridayHour)
			return;

		if (time.DayOfWeek == DayOfWeek.Monday && time.Hour < MondayHour)
			return;

		if (_prevHigh == 0m && _prevLow == 0m)
			return;

		var price = trade.TradePrice ?? 0m;
		var step = Security.PriceStep ?? 1m;
		var stopOffset = StopLoss * step;
		var takeOffset = TakeProfit * step;
		var trailOffset = TrailingStop * step;

		if (Position == 0)
		{
			var volume = GetVolume();
			if (volume <= 0)
				return;

			if (price > _prevHigh)
			{
				BuyMarket(volume);
				_stopPrice = price - stopOffset;
				_takePrice = price + takeOffset;
			}
			else if (price < _prevLow)
			{
				SellMarket(volume);
				_stopPrice = price + stopOffset;
				_takePrice = price - takeOffset;
			}
		}
		else if (Position > 0)
		{
			if (price - _stopPrice > trailOffset)
				_stopPrice = price - trailOffset;

			if (price <= _stopPrice || price >= _takePrice)
				SellMarket(Position);
		}
		else
		{
			if (_stopPrice - price > trailOffset)
				_stopPrice = price + trailOffset;

			if (price >= _stopPrice || price <= _takePrice)
				BuyMarket(-Position);
		}
	}
}
