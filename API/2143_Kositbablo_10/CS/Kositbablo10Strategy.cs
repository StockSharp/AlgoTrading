using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe strategy based on RSI and EMA signals.
/// </summary>
public class Kositbablo10Strategy : Strategy
{
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<bool> _turbo;
	private readonly StrategyParam<DataType> _hourlyCandleType;
	private readonly StrategyParam<DataType> _dailyCandleType;

	private decimal _rsiDailyBuy;
	private decimal _rsiDailySell;
	private decimal _rsiHourlyBuy;
	private decimal _rsiHourlySell;
	private decimal _emaBuyLong;
	private decimal _emaBuyShort;
	private decimal _emaSellLong;
	private decimal _emaSellShort;

	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public bool Turbo { get => _turbo.Value; set => _turbo.Value = value; }
	public DataType HourlyCandleType { get => _hourlyCandleType.Value; set => _hourlyCandleType.Value = value; }
	public DataType DailyCandleType { get => _dailyCandleType.Value; set => _dailyCandleType.Value = value; }

	public Kositbablo10Strategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 1400)
		.SetDisplay("Take Profit", "Take profit in points", "General");
		_stopLoss = Param(nameof(StopLoss), 500)
		.SetDisplay("Stop Loss", "Stop loss in points", "General");
		_turbo = Param(nameof(Turbo), false)
		.SetDisplay("Turbo Mode", "Allow trading even if position exists", "General");
		_hourlyCandleType = Param(nameof(HourlyCandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Hourly Candle Type", "Type of hourly candles", "General");
		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Daily Candle Type", "Type of daily candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, HourlyCandleType), (Security, DailyCandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_rsiDailyBuy = _rsiDailySell = _rsiHourlyBuy = _rsiHourlySell = 0m;
		_emaBuyLong = _emaBuyShort = _emaSellLong = _emaSellShort = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsiDailyBuy = new RelativeStrengthIndex { Length = 11 };
		var rsiDailySell = new RelativeStrengthIndex { Length = 22 };

		var dailySubscription = SubscribeCandles(DailyCandleType);
		dailySubscription.Bind(rsiDailyBuy, rsiDailySell, OnDailyProcess).Start();

		var rsiHourlyBuy = new RelativeStrengthIndex { Length = 5 };
		var rsiHourlySell = new RelativeStrengthIndex { Length = 20 };
		var emaBuyLong = new ExponentialMovingAverage { Length = 20 };
		var emaBuyShort = new ExponentialMovingAverage { Length = 2 };
		var emaSellLong = new ExponentialMovingAverage { Length = 23 };
		var emaSellShort = new ExponentialMovingAverage { Length = 12 };

		var hourlySubscription = SubscribeCandles(HourlyCandleType);
		hourlySubscription.Bind(rsiHourlyBuy, rsiHourlySell, emaBuyLong, emaBuyShort, emaSellLong, emaSellShort, OnHourlyProcess).Start();

		StartProtection(new Unit(TakeProfit, UnitTypes.Point), new Unit(StopLoss, UnitTypes.Point));
	}

	private void OnDailyProcess(ICandleMessage candle, decimal rsiBuy, decimal rsiSell)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_rsiDailyBuy = rsiBuy;
		_rsiDailySell = rsiSell;
	}

	private void OnHourlyProcess(ICandleMessage candle, decimal rsiBuy, decimal rsiSell, decimal emaBuyLong, decimal emaBuyShort, decimal emaSellLong, decimal emaSellShort)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_rsiHourlyBuy = rsiBuy;
		_rsiHourlySell = rsiSell;
		_emaBuyLong = emaBuyLong;
		_emaBuyShort = emaBuyShort;
		_emaSellLong = emaSellLong;
		_emaSellShort = emaSellShort;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!Turbo && Position != 0)
		return;

		var buyCond = _rsiDailyBuy < 60 && _rsiHourlyBuy < 48 && _emaBuyLong > _emaBuyShort;
		var sellCond = _rsiDailySell > 38 && _rsiHourlySell > 60 && _emaSellLong > _emaSellShort;

		if (buyCond && Position <= 0)
		BuyMarket();
		else if (sellCond && Position >= 0)
		SellMarket();
	}
}
