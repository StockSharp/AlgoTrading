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

public class OpeningAndClosingOnTimeV2Strategy : Strategy
{
	public enum TradeModeses
	{
		Buy,
		Sell,
		BuyAndSell
	}

	private readonly StrategyParam<int> _openHour;
	private readonly StrategyParam<int> _closeHour;
	private readonly StrategyParam<TradeModeses> _tradeMode;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSlow;
	private decimal _prevFast;
	private bool _opened;

	public int OpenHour
	{
		get => _openHour.Value;
		set => _openHour.Value = value;
	}

	public int CloseHour
	{
		get => _closeHour.Value;
		set => _closeHour.Value = value;
	}

	public TradeModeses TradeMode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public OpeningAndClosingOnTimeV2Strategy()
	{
		_openHour = Param(nameof(OpenHour), 2)
			.SetDisplay("Open Hour", "Hour of day to open trades (UTC)", "General");

		_closeHour = Param(nameof(CloseHour), 14)
			.SetDisplay("Close Hour", "Hour of day to close trades (UTC)", "General");

		_tradeMode = Param(nameof(TradeMode), TradeModeses.BuyAndSell)
			.SetDisplay("Trade Mode", "Allowed trade directions", "General");

		_slowPeriod = Param(nameof(SlowPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow EMA period", "Indicators");

		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast EMA period", "Indicators");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Protection");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Take profit in price units", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSlow = 0m;
		_prevFast = 0m;
		_opened = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var slowMa = new ExponentialMovingAverage { Length = SlowPeriod };
		var fastMa = new ExponentialMovingAverage { Length = FastPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(slowMa, fastMa, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Absolute),
			new Unit(StopLoss, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, slowMa);
			DrawIndicator(area, fastMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal slow, decimal fast)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.OpenTime.Hour;

		if (!_opened && hour >= OpenHour && hour < CloseHour)
		{
			if (_prevSlow != 0m && _prevFast != 0m)
			{
				var buySignal = _prevFast <= _prevSlow && fast > slow;
				var sellSignal = _prevFast >= _prevSlow && fast < slow;

				if (buySignal && (TradeMode == TradeModeses.Buy || TradeMode == TradeModeses.BuyAndSell) && Position <= 0)
				{
					BuyMarket();
					_opened = true;
				}
				else if (sellSignal && (TradeMode == TradeModeses.Sell || TradeMode == TradeModeses.BuyAndSell) && Position >= 0)
				{
					SellMarket();
					_opened = true;
				}
			}
		}
		else if (_opened && hour >= CloseHour)
		{
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();
			_opened = false;
		}

		_prevSlow = slow;
		_prevFast = fast;
	}
}
