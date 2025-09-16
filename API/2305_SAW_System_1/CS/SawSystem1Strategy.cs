using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy placing stop orders at the start of each trading day.
/// Calculates daily range and sets stop-loss and take-profit as percentages of that range.
/// </summary>
public class SawSystem1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _volatilityDays;
	private readonly StrategyParam<int> _openHour;
	private readonly StrategyParam<int> _closeHour;
	private readonly StrategyParam<int> _stopLossRate;
	private readonly StrategyParam<int> _takeProfitRate;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private ATR _atr;
	private decimal _avgRange;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _offsetDistance;
	private DateTime _currentDate;
	private bool _ordersPlaced;
	private bool _modOrder;
	private bool _protectionPlaced;

	private Order _buyEntryOrder;
	private Order _sellEntryOrder;
	private decimal _buyEntryPrice;
	private decimal _sellEntryPrice;
	private decimal _entryPrice;
	private decimal _prevPosition;

	/// <summary>
	/// Order volume.
	/// </summary>
	public new decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Number of days for average range calculation.
	/// </summary>
	public int VolatilityDays
	{
		get => _volatilityDays.Value;
		set => _volatilityDays.Value = value;
	}

	/// <summary>
	/// Hour to place entry orders.
	/// </summary>
	public int OpenHour
	{
		get => _openHour.Value;
		set => _openHour.Value = value;
	}

	/// <summary>
	/// Hour to cancel remaining orders.
	/// </summary>
	public int CloseHour
	{
		get => _closeHour.Value;
		set => _closeHour.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage of average range.
	/// </summary>
	public int StopLossRate
	{
		get => _stopLossRate.Value;
		set => _stopLossRate.Value = value;
	}

	/// <summary>
	/// Take-profit percentage of average range.
	/// </summary>
	public int TakeProfitRate
	{
		get => _takeProfitRate.Value;
		set => _takeProfitRate.Value = value;
	}

	/// <summary>
	/// Keep opposite order to reverse position.
	/// </summary>
	public bool Reverse
	{
		get => _reverse.Value;
		set => _reverse.Value = value;
	}

	/// <summary>
	/// Increase volume for opposite order when reversing.
	/// </summary>
	public bool UseMartingale
	{
		get => _useMartingale.Value;
		set => _useMartingale.Value = value;
	}

	/// <summary>
	/// Volume multiplier for martingale.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type for time tracking.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public SawSystem1Strategy()
	{
		_volume = Param(nameof(Volume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Lot", "Order volume", "General");

		_volatilityDays = Param(nameof(VolatilityDays), 5)
			.SetGreaterThanZero()
			.SetDisplay("Volatility Days", "Days for average range", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_openHour = Param(nameof(OpenHour), 7)
			.SetDisplay("Open Hour", "Hour to place orders", "Time");

		_closeHour = Param(nameof(CloseHour), 10)
			.SetDisplay("Close Hour", "Hour to cancel orders", "Time");

		_stopLossRate = Param(nameof(StopLossRate), 15)
			.SetDisplay("Stop Loss %", "Stop-loss percent of range", "Risk");

		_takeProfitRate = Param(nameof(TakeProfitRate), 30)
			.SetDisplay("Take Profit %", "Take-profit percent of range", "Risk");

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse", "Keep opposite order after fill", "Logic");

		_useMartingale = Param(nameof(UseMartingale), false)
			.SetDisplay("Use Martingale", "Increase volume on reversal", "Logic");

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 2m)
			.SetDisplay("Martingale Multiplier", "Volume multiplier", "Logic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for time tracking", "General");
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
		_avgRange = 0m;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_offsetDistance = 0m;
		_ordersPlaced = false;
		_modOrder = false;
		_protectionPlaced = false;
		_buyEntryOrder = null;
		_sellEntryOrder = null;
		_prevPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new ATR { Length = VolatilityDays };

		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		dailySubscription
			.Bind(_atr, ProcessDaily)
			.Start();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDaily(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished || !_atr.IsFormed)
			return;

		_avgRange = atrValue;
		_stopLossDistance = _avgRange * StopLossRate / 100m;
		_takeProfitDistance = _avgRange * TakeProfitRate / 100m;
		_offsetDistance = _stopLossDistance / 2m;

		_currentDate = candle.OpenTime.Date;
		_ordersPlaced = false;
		_modOrder = false;

		LogInfo($"Daily range updated: {_avgRange}");
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;

		if (_currentDate != time.Date)
		{
			_currentDate = time.Date;
			_ordersPlaced = false;
			_modOrder = false;
		}

		if (!_ordersPlaced && _atr.IsFormed && time.Hour == OpenHour)
			PlaceEntryOrders(candle.ClosePrice);

		if (_ordersPlaced)
		{
			if (!_modOrder && time.Hour >= CloseHour && Position == 0)
			{
				CancelEntryOrders();
				_modOrder = true;
			}
			else if (_modOrder && time.Hour >= CloseHour)
			{
				CancelEntryOrders();
			}
		}

		if (Position == 0)
			_protectionPlaced = false;
	}

	private void PlaceEntryOrders(decimal price)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var buyPrice = price + _offsetDistance;
		var sellPrice = price - _offsetDistance;
		var volume = Volume;

		_buyEntryPrice = buyPrice;
		_sellEntryPrice = sellPrice;

		_buyEntryOrder = BuyStop(price: buyPrice, volume: volume);
		_sellEntryOrder = SellStop(price: sellPrice, volume: volume);

		_ordersPlaced = true;

		LogInfo($"Placed stop orders. BuyStop={buyPrice}, SellStop={sellPrice}");
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var currentPos = Position;

		if (_prevPosition == 0m && currentPos != 0m)
		{
			_entryPrice = trade.Trade.Price;

			if (!_modOrder)
			{
				if (!Reverse)
				{
					CancelOppositeEntry(currentPos);
				}
				else if (UseMartingale)
				{
					ReRegisterOppositeEntry(currentPos);
				}

				_modOrder = true;
			}

			PlaceProtectionOrders(currentPos);
		}
		else if (_prevPosition != 0m && currentPos == 0m)
		{
			CancelEntryOrders();
			_protectionPlaced = false;
		}

		_prevPosition = currentPos;
	}

	private void CancelOppositeEntry(decimal position)
	{
		if (position > 0)
		{
			if (_sellEntryOrder != null && _sellEntryOrder.State == OrderStates.Active)
				CancelOrder(_sellEntryOrder);
		}
		else if (position < 0)
		{
			if (_buyEntryOrder != null && _buyEntryOrder.State == OrderStates.Active)
				CancelOrder(_buyEntryOrder);
		}
	}

	private void ReRegisterOppositeEntry(decimal position)
	{
		var volume = Volume * MartingaleMultiplier;

		if (position > 0)
		{
			if (_sellEntryOrder != null && _sellEntryOrder.State == OrderStates.Active)
				CancelOrder(_sellEntryOrder);
			_sellEntryOrder = SellStop(price: _sellEntryPrice, volume: volume);
		}
		else if (position < 0)
		{
			if (_buyEntryOrder != null && _buyEntryOrder.State == OrderStates.Active)
				CancelOrder(_buyEntryOrder);
			_buyEntryOrder = BuyStop(price: _buyEntryPrice, volume: volume);
		}
	}

	private void PlaceProtectionOrders(decimal position)
	{
		if (_protectionPlaced)
			return;

		var volume = Math.Abs(position);

		if (position > 0)
		{
			var sl = _entryPrice - _stopLossDistance;
			var tp = _entryPrice + _takeProfitDistance;
			SellStop(price: sl, volume: volume);
			SellLimit(price: tp, volume: volume);
		}
		else if (position < 0)
		{
			var sl = _entryPrice + _stopLossDistance;
			var tp = _entryPrice - _takeProfitDistance;
			BuyStop(price: sl, volume: volume);
			BuyLimit(price: tp, volume: volume);
		}

		_protectionPlaced = true;
	}

	private void CancelEntryOrders()
	{
		if (_buyEntryOrder != null && _buyEntryOrder.State == OrderStates.Active)
			CancelOrder(_buyEntryOrder);

		if (_sellEntryOrder != null && _sellEntryOrder.State == OrderStates.Active)
			CancelOrder(_sellEntryOrder);
	}
}
