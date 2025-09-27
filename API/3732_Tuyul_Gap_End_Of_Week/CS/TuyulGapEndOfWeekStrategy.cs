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
/// Port of the MetaTrader 5 expert advisor TuyulGAP.
/// Places weekly breakout stop orders around the recent high/low range and closes positions once secure profit is reached.
/// </summary>
public class TuyulGapEndOfWeekStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _lookbackBars;
	private readonly StrategyParam<int> _setupDayOfWeek;
	private readonly StrategyParam<int> _setupHour;
	private readonly StrategyParam<int> _setupMinuteWindow;
	private readonly StrategyParam<decimal> _secureProfitTarget;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highestHigh = null!;
	private Lowest _lowestLow = null!;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _protectiveStopOrder;

	private bool _ordersPlacedForSession;
	private DateTime? _lastPlacementDate;

	private decimal _tickSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="TuyulGapEndOfWeekStrategy"/> class.
	/// </summary>
	public TuyulGapEndOfWeekStrategy()
	{

		_stopLossPoints = Param(nameof(StopLossPoints), 60)
		.SetRange(0, 5000)
		.SetDisplay("Stop Loss (points)", "Distance from entry used for protective stops", "Risk");

		_lookbackBars = Param(nameof(LookbackBars), 12)
		.SetRange(2, 500)
		.SetDisplay("Lookback Bars", "Number of finished candles inspected for highs/lows", "Setup");

		_setupDayOfWeek = Param(nameof(SetupDayOfWeek), 5)
		.SetRange(0, 6)
		.SetDisplay("Setup Day Of Week", "Day index (0=Sunday) that stages the weekly orders", "Setup");

		_setupHour = Param(nameof(SetupHour), 23)
		.SetRange(0, 23)
		.SetDisplay("Setup Hour", "Exchange hour when the weekly setup is evaluated", "Setup");

		_setupMinuteWindow = Param(nameof(SetupMinuteWindow), 15)
		.SetRange(0, 59)
		.SetDisplay("Setup Minute Window", "Minutes after the setup hour when staging is allowed", "Setup");

		_secureProfitTarget = Param(nameof(SecureProfitTarget), 5m)
		.SetRange(0m, 100000m)
		.SetDisplay("Secure Profit Target", "Unrealized profit per position that triggers an immediate exit", "Risk");

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(1)))
		.SetDisplay("Candle Type", "Timeframe used for the high/low scan and monitoring", "Data");
	}


	/// <summary>
	/// Distance from entry used for protective stops, measured in instrument points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Number of finished candles inspected for highs and lows.
	/// </summary>
	public int LookbackBars
	{
		get => _lookbackBars.Value;
		set => _lookbackBars.Value = value;
	}

	/// <summary>
	/// Day index (0=Sunday … 6=Saturday) that stages the weekly setup.
	/// </summary>
	public int SetupDayOfWeek
	{
		get => _setupDayOfWeek.Value;
		set => _setupDayOfWeek.Value = value;
	}

	/// <summary>
	/// Exchange hour when the weekly setup is evaluated.
	/// </summary>
	public int SetupHour
	{
		get => _setupHour.Value;
		set => _setupHour.Value = value;
	}

	/// <summary>
	/// Minutes after the setup hour when staging is allowed.
	/// </summary>
	public int SetupMinuteWindow
	{
		get => _setupMinuteWindow.Value;
		set => _setupMinuteWindow.Value = value;
	}

	/// <summary>
	/// Unrealized profit per position that triggers an immediate exit.
	/// </summary>
	public decimal SecureProfitTarget
	{
		get => _secureProfitTarget.Value;
		set => _secureProfitTarget.Value = value;
	}

	/// <summary>
	/// Timeframe used for the high/low scan and monitoring.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buyStopOrder = null;
		_sellStopOrder = null;
		_protectiveStopOrder = null;
		_ordersPlacedForSession = false;
		_lastPlacementDate = null;
		_tickSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security?.PriceStep ?? 0m;

		_highestHigh = new Highest
		{
			Length = Math.Max(2, LookbackBars),
			CandlePrice = CandlePrice.High,
		};

		_lowestLow = new Lowest
		{
			Length = Math.Max(2, LookbackBars),
			CandlePrice = CandlePrice.Low,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_highestHigh, _lowestLow, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		ResetWeeklyState(candle.CloseTime);
		CloseProfitablePositions();
		TryPlaceWeeklyOrders(candle.CloseTime, highestValue, lowestValue);
	}

	private void ResetWeeklyState(DateTimeOffset time)
	{
		if (time.DayOfWeek == DayOfWeek.Monday)
		{
			CancelPendingOrders();
			_ordersPlacedForSession = false;
			_lastPlacementDate = time.Date;
			return;
		}

		if (_lastPlacementDate != time.Date && time.DayOfWeek == GetSetupDay())
		{
			_ordersPlacedForSession = false;
			_lastPlacementDate = time.Date;
		}
	}

	private void CloseProfitablePositions()
	{
		if (SecureProfitTarget <= 0m)
		return;

		var portfolio = Portfolio;
		var security = Security;
		if (portfolio == null || security == null)
		return;

		foreach (var position in portfolio.Positions.ToArray())
		{
			if (position.Security != security)
			continue;

			var volume = position.CurrentValue ?? 0m;
			if (volume == 0m)
			continue;

			var profit = position.PnL ?? 0m;
			if (profit < SecureProfitTarget)
			continue;

			if (volume > 0m)
			SellMarket(volume);
			else
			BuyMarket(-volume);

			CancelIfActive(ref _protectiveStopOrder);
		}
	}

	private void TryPlaceWeeklyOrders(DateTimeOffset time, decimal highestValue, decimal lowestValue)
	{
		if (time.DayOfWeek != GetSetupDay())
		return;

		if (time.Hour != SetupHour)
		return;

		if (time.Minute > SetupMinuteWindow)
		return;

		if (_lastPlacementDate != time.Date)
		{
			_ordersPlacedForSession = false;
			_lastPlacementDate = time.Date;
		}

		if (_ordersPlacedForSession)
		return;

		if (!_highestHigh.IsFormed || !_lowestLow.IsFormed)
		return;

		var tick = _tickSize > 0m ? _tickSize : Security?.PriceStep ?? 0m;
		if (tick <= 0m)
		return;

		var volume = NormalizeVolume(Volume);
		if (volume <= 0m)
		return;

		var buyPrice = NormalizePrice(highestValue + tick);
		var sellPrice = NormalizePrice(lowestValue - tick);

		if (buyPrice <= 0m || sellPrice <= 0m)
		return;

		if (!IsActive(_buyStopOrder))
		_buyStopOrder = BuyStop(volume, buyPrice);

		if (!IsActive(_sellStopOrder))
		_sellStopOrder = SellStop(volume, sellPrice);

		_ordersPlacedForSession = true;
	}

	private void CancelPendingOrders()
	{
		CancelIfActive(ref _buyStopOrder);
		CancelIfActive(ref _sellStopOrder);
	}

	private void CancelIfActive(ref Order order)
	{
		if (order == null)
		return;

		if (order.State == OrderStates.Active)
		CancelOrder(order);

		order = null;
	}

	private bool IsActive(Order order)
	{
		return order != null && order.State == OrderStates.Active;
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order.Security != Security)
		return;

		if (order == _buyStopOrder && order.State != OrderStates.Active)
		_buyStopOrder = null;
		else if (order == _sellStopOrder && order.State != OrderStates.Active)
		_sellStopOrder = null;
		else if (order == _protectiveStopOrder && order.State != OrderStates.Active)
		_protectiveStopOrder = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			CancelIfActive(ref _protectiveStopOrder);
		}
		else
		{
			UpdateProtectiveStop();
		}
	}

	private void UpdateProtectiveStop()
	{
		var stopDistance = GetStopLossDistance();
		if (stopDistance <= 0m)
		{
			CancelIfActive(ref _protectiveStopOrder);
			return;
		}

		var portfolio = Portfolio;
		var security = Security;
		if (portfolio == null || security == null)
		return;

		var position = portfolio.Positions.FirstOrDefault(p => p.Security == security);
		if (position == null)
		{
			CancelIfActive(ref _protectiveStopOrder);
			return;
		}

		var volume = position.CurrentValue ?? 0m;
		if (volume == 0m)
		{
			CancelIfActive(ref _protectiveStopOrder);
			return;
		}

		var averagePrice = position.AveragePrice ?? 0m;
		if (averagePrice <= 0m)
		{
			CancelIfActive(ref _protectiveStopOrder);
			return;
		}

		var stopPrice = volume > 0m
		? NormalizePrice(averagePrice - stopDistance)
		: NormalizePrice(averagePrice + stopDistance);

		if (stopPrice <= 0m)
		{
			CancelIfActive(ref _protectiveStopOrder);
			return;
		}

		var desiredVolume = Math.Abs(volume);

		if (_protectiveStopOrder != null)
		{
			if (_protectiveStopOrder.State == OrderStates.Active && _protectiveStopOrder.Price == stopPrice && _protectiveStopOrder.Volume == desiredVolume)
			return;

			CancelIfActive(ref _protectiveStopOrder);
		}

		_protectiveStopOrder = volume > 0m
		? SellStop(desiredVolume, stopPrice)
		: BuyStop(desiredVolume, stopPrice);
	}

	private decimal GetStopLossDistance()
	{
		var tick = _tickSize > 0m ? _tickSize : Security?.PriceStep ?? 0m;
		if (tick <= 0m)
		return 0m;

		return StopLossPoints > 0 ? StopLossPoints * tick : 0m;
	}

	private decimal NormalizePrice(decimal price)
	{
		var tick = _tickSize > 0m ? _tickSize : Security?.PriceStep ?? 0m;
		if (tick <= 0m)
		return price;

		return Math.Round(price / tick, MidpointRounding.AwayFromZero) * tick;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		if (security.VolumeStep is { } volumeStep && volumeStep > 0m)
		volume = Math.Round(volume / volumeStep, MidpointRounding.AwayFromZero) * volumeStep;

		if (security.MinVolume is { } minVolume && minVolume > 0m && volume < minVolume)
		volume = minVolume;

		if (security.MaxVolume is { } maxVolume && maxVolume > 0m && volume > maxVolume)
		volume = maxVolume;

		return volume;
	}

	private DayOfWeek GetSetupDay()
	{
		var day = SetupDayOfWeek % 7;
		if (day < 0)
		day += 7;

		return (DayOfWeek)day;
	}
}

