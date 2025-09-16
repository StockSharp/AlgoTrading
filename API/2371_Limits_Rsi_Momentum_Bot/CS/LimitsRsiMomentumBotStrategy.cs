using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that places limit orders based on RSI and Momentum values.
/// A buy limit is set below the candle open when both indicators signal oversold.
/// A sell limit is set above the open when indicators show overbought conditions.
/// Opposite pending order is cancelled once a position is opened.
/// Stop-loss and take-profit are managed via StartProtection.
/// </summary>
public class LimitsRsiMomentumBotStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _limitOrderDistance;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyRestrict;
	private readonly StrategyParam<decimal> _rsiSellRestrict;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyRestrict;
	private readonly StrategyParam<decimal> _momentumSellRestrict;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	private Order _buyOrder;
	private Order _sellOrder;

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Distance from candle open to place limit orders in price steps.
	/// </summary>
	public int LimitOrderDistance
	{
		get => _limitOrderDistance.Value;
		set => _limitOrderDistance.Value = value;
	}

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI threshold for long entries.
	/// </summary>
	public decimal RsiBuyRestrict
	{
		get => _rsiBuyRestrict.Value;
		set => _rsiBuyRestrict.Value = value;
	}

	/// <summary>
	/// RSI threshold for short entries.
	/// </summary>
	public decimal RsiSellRestrict
	{
		get => _rsiSellRestrict.Value;
		set => _rsiSellRestrict.Value = value;
	}

	/// <summary>
	/// Momentum period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Momentum threshold for long entries.
	/// </summary>
	public decimal MomentumBuyRestrict
	{
		get => _momentumBuyRestrict.Value;
		set => _momentumBuyRestrict.Value = value;
	}

	/// <summary>
	/// Momentum threshold for short entries.
	/// </summary>
	public decimal MomentumSellRestrict
	{
		get => _momentumSellRestrict.Value;
		set => _momentumSellRestrict.Value = value;
	}

	/// <summary>
	/// Trading start time.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Trading end time.
	/// </summary>
	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="LimitsRsiMomentumBotStrategy"/>.
	/// </summary>
	public LimitsRsiMomentumBotStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_limitOrderDistance = Param(nameof(LimitOrderDistance), 5)
			.SetGreaterThanZero()
			.SetDisplay("Limit Order Distance", "Distance from candle open in price steps", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_takeProfit = Param(nameof(TakeProfit), 35)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Profit target in price steps", "Protection")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 5);

		_stopLoss = Param(nameof(StopLoss), 8)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Loss limit in price steps", "Protection")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators");

		_rsiBuyRestrict = Param(nameof(RsiBuyRestrict), 30m)
			.SetDisplay("RSI Buy Threshold", "Max RSI value to allow buys", "Indicators");

		_rsiSellRestrict = Param(nameof(RsiSellRestrict), 70m)
			.SetDisplay("RSI Sell Threshold", "Min RSI value to allow sells", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Period for Momentum calculation", "Indicators");

		_momentumBuyRestrict = Param(nameof(MomentumBuyRestrict), 1m)
			.SetDisplay("Momentum Buy Threshold", "Max Momentum value to allow buys", "Indicators");

		_momentumSellRestrict = Param(nameof(MomentumSellRestrict), 1m)
			.SetDisplay("Momentum Sell Threshold", "Min Momentum value to allow sells", "Indicators");

		_startTime = Param(nameof(StartTime), TimeSpan.Zero)
			.SetDisplay("Start Time", "Trading start time", "Trading");

		_endTime = Param(nameof(EndTime), new TimeSpan(23, 59, 0))
			.SetDisplay("End Time", "Trading end time", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var momentum = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, momentum, ProcessCandle)
			.Start();

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfit * step, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss * step, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, momentum);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsTradingTime(candle.OpenTime))
			return;

		if (_buyOrder != null && _buyOrder.State == OrderStates.Active)
		{
			CancelOrder(_buyOrder);
			_buyOrder = null;
		}

		if (_sellOrder != null && _sellOrder.State == OrderStates.Active)
		{
			CancelOrder(_sellOrder);
			_sellOrder = null;
		}

		var step = Security.PriceStep ?? 1m;

		if (rsiValue < RsiBuyRestrict && momentumValue < MomentumBuyRestrict && Position <= 0)
		{
			var price = candle.OpenPrice - LimitOrderDistance * step;
			_buyOrder = BuyLimit(Volume + Math.Abs(Position), price);
		}

		if (rsiValue > RsiSellRestrict && momentumValue > MomentumSellRestrict && Position >= 0)
		{
			var price = candle.OpenPrice + LimitOrderDistance * step;
			_sellOrder = SellLimit(Volume + Math.Abs(Position), price);
		}
	}

	private bool IsTradingTime(DateTimeOffset time)
	{
		var t = time.LocalDateTime.TimeOfDay;
		return t >= StartTime && t <= EndTime;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
		{
			if (_sellOrder != null && _sellOrder.State == OrderStates.Active)
			{
				CancelOrder(_sellOrder);
				_sellOrder = null;
			}
		}
		else if (Position < 0)
		{
			if (_buyOrder != null && _buyOrder.State == OrderStates.Active)
			{
				CancelOrder(_buyOrder);
				_buyOrder = null;
			}
		}
		else
		{
			_buyOrder = null;
			_sellOrder = null;
		}
	}
}
