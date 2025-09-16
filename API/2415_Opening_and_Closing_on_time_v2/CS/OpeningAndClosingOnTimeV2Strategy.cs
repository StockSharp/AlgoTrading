using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum TradeMode
{
	Buy,
	Sell,
	BuyAndSell
}

public class OpeningAndClosingOnTimeV2Strategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _openTime;
	private readonly StrategyParam<TimeSpan> _closeTime;
	private readonly StrategyParam<TradeMode> _tradeMode;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _slowMa;
	private ExponentialMovingAverage _fastMa;

	private decimal _prevSlow;
	private decimal _prevFast;
	private bool _hasPrev;
	private bool _opened;

	public TimeSpan OpenTime
	{
		get => _openTime.Value;
		set => _openTime.Value = value;
	}

	public TimeSpan CloseTime
	{
		get => _closeTime.Value;
		set => _closeTime.Value = value;
	}

	public TradeMode TradeMode
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

	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public OpeningAndClosingOnTimeV2Strategy()
	{
		_openTime = Param(nameof(OpenTime), new TimeSpan(5, 0, 0))
			.SetDisplay("Open Time", "Time of day to open trades", "General");

		_closeTime = Param(nameof(CloseTime), new TimeSpan(21, 1, 0))
			.SetDisplay("Close Time", "Time of day to close trades", "General");

		_tradeMode = Param(nameof(TradeMode), TradeMode.BuyAndSell)
			.SetDisplay("Trade Mode", "Allowed trade directions", "General");

		_slowPeriod = Param(nameof(SlowPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow EMA period", "Indicators");

		_fastPeriod = Param(nameof(FastPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast EMA period", "Indicators");

		_stopLossTicks = Param(nameof(StopLossTicks), 30)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss in ticks", "Protection");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 50)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit in ticks", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_hasPrev = false;
		_opened = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_slowMa = new ExponentialMovingAverage { Length = SlowPeriod };
		_fastMa = new ExponentialMovingAverage { Length = FastPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_slowMa, _fastMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _fastMa);
			DrawOwnTrades(area);
		}

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitTicks * step, UnitTypes.Point),
			stopLoss: new Unit(StopLossTicks * step, UnitTypes.Point),
			useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle, decimal slow, decimal fast)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime.TimeOfDay;

		if (!_opened && time == OpenTime)
		{
			if (_hasPrev)
			{
				if ((TradeMode == TradeMode.Buy || TradeMode == TradeMode.BuyAndSell) && _prevFast > _prevSlow && Position <= 0)
					BuyMarket();
				if ((TradeMode == TradeMode.Sell || TradeMode == TradeMode.BuyAndSell) && _prevFast < _prevSlow && Position >= 0)
					SellMarket();
			}
			_opened = true;
		}
		else if (_opened && time == CloseTime)
		{
			if (Position != 0)
				ClosePosition();
			_opened = false;
		}

		_prevSlow = slow;
		_prevFast = fast;
		_hasPrev = true;
	}
}
