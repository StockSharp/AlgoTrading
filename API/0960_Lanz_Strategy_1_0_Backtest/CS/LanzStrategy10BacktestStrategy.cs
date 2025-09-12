using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// LANZ Strategy 1.0 backtest: places a limit order based on 08:00-18:00 price direction.
/// </summary>
public class LanzStrategy10BacktestStrategy : Strategy
{
	private readonly StrategyParam<decimal> _accountSizeUsd;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _epOffsetFraction;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<int> _manualCloseHour;
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _openAt0800;
	private int _prevPriceDirection;
	private int _todayPriceDirection;
	private int _finalSignalDirection;
	private decimal _baseLevel;
	private bool _orderSent;
	private Order _entryOrder;
	private decimal _pipSize;

	private readonly TimeZoneInfo _nyZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

	public decimal AccountSizeUsd { get => _accountSizeUsd.Value; set => _accountSizeUsd.Value = value; }
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }
	public decimal EpOffsetFraction { get => _epOffsetFraction.Value; set => _epOffsetFraction.Value = value; }
	public decimal StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public decimal TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public int ManualCloseHour { get => _manualCloseHour.Value; set => _manualCloseHour.Value = value; }
	public bool EnableBuy { get => _enableBuy.Value; set => _enableBuy.Value = value; }
	public bool EnableSell { get => _enableSell.Value; set => _enableSell.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LanzStrategy10BacktestStrategy()
	{
		_accountSizeUsd = Param(nameof(AccountSizeUsd), 100000m)
			.SetDisplay("Account Size USD", "Account capital", "Money");

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetDisplay("Risk %", "Risk percent per trade", "Money")
			.SetCanOptimize(true);

		_epOffsetFraction = Param(nameof(EpOffsetFraction), 0m)
			.SetDisplay("EP Offset", "Entry price offset fraction", "Risk")
			.SetRange(-5m, 5m);

		_stopLossPips = Param(nameof(StopLossPips), 18m)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance", "Risk")
			.SetGreaterThanZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 54m)
			.SetDisplay("Take Profit (pips)", "Take profit distance", "Risk")
			.SetGreaterThanZero();

		_manualCloseHour = Param(nameof(ManualCloseHour), 9)
			.SetDisplay("Manual Close Hour", "Hour to close open position NY time", "Time");

		_enableBuy = Param(nameof(EnableBuy), true)
			.SetDisplay("Enable Buy", "Allow long entries", "Signals");

		_enableSell = Param(nameof(EnableSell), true)
			.SetDisplay("Enable Sell", "Allow short entries", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();

		_openAt0800 = 0m;
		_prevPriceDirection = 0;
		_todayPriceDirection = 0;
		_finalSignalDirection = 0;
		_baseLevel = 0m;
		_orderSent = false;
		_entryOrder = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = (Security?.PriceStep ?? 1m) * 10m;

		StartProtection(
			stopLoss: new Unit(StopLossPips * _pipSize, UnitTypes.Absolute),
			takeProfit: new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute),
			useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var nyOpen = TimeZoneInfo.ConvertTime(candle.OpenTime.UtcDateTime, _nyZone);
		var nyClose = TimeZoneInfo.ConvertTime(candle.CloseTime.UtcDateTime, _nyZone);

		var is0800 = nyOpen.Hour == 8 && nyOpen.Minute == 0;
		if (is0800)
			_openAt0800 = candle.OpenPrice;

		var is1800 = nyClose.Hour == 18 && nyClose.Minute == 0;
		if (is1800)
		{
			var priceDirection = candle.ClosePrice > _openAt0800 ? 1 : candle.ClosePrice < _openAt0800 ? -1 : 0;
			_prevPriceDirection = _todayPriceDirection;
			_todayPriceDirection = priceDirection;
			var coinciden = priceDirection == _prevPriceDirection && _prevPriceDirection != 0;
			_finalSignalDirection = coinciden ? priceDirection : -1 * priceDirection;

			var fibRange = candle.HighPrice - candle.LowPrice;
			_baseLevel = _finalSignalDirection == -1
				? (EpOffsetFraction == 0m ? candle.LowPrice : candle.LowPrice + fibRange * -EpOffsetFraction)
				: (EpOffsetFraction == 0m ? candle.HighPrice : candle.HighPrice - fibRange * -EpOffsetFraction);

			_orderSent = false;
			_entryOrder = null;
		}

		if (nyClose.Hour == ManualCloseHour && nyClose.Minute == 0 && Position != 0)
		{
			CloseAll();
			_orderSent = false;
			_entryOrder = null;
			return;
		}

		if (nyClose.Hour == 8 && nyClose.Minute == 0 && Position == 0 && _orderSent && _entryOrder is not null)
		{
			CancelOrder(_entryOrder);
			_orderSent = false;
			_entryOrder = null;
		}

		var entryWindow = nyOpen.Hour >= 18 || nyOpen.Hour < 8;
		var canPlaceOrder = !_orderSent && Position == 0 && entryWindow;

		if (canPlaceOrder)
		{
			var riskUsd = AccountSizeUsd * (RiskPercent / 100m);
			var qty = StopLossPips > 0m ? riskUsd / (StopLossPips * 10m) : 0m;

			if (qty <= 0m)
				return;

			var isLong = _finalSignalDirection == -1;

			if (isLong && EnableBuy)
			{
				_entryOrder = BuyLimit(_baseLevel, qty);
				_orderSent = true;
			}
			else if (!isLong && EnableSell)
			{
				_entryOrder = SellLimit(_baseLevel, qty);
				_orderSent = true;
			}
		}
	}
}