using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NewsHourTrade strategy places pending buy and sell stop orders around scheduled news events.
/// Orders are placed at a price offset and managed with stop loss, take profit and optional trailing stop.
/// </summary>
public class NewsHourTradeStrategy : Strategy
{
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _delaySeconds;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _priceGap;
	private readonly StrategyParam<int> _expirationSeconds;
	private readonly StrategyParam<bool> _trailStop;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<int> _trailingGap;
	private readonly StrategyParam<bool> _buyTrade;
	private readonly StrategyParam<bool> _sellTrade;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _lastTradeDay;
	private decimal _tickSize;
	private bool _entryInitialized;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Trading start hour.
	/// </summary>
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }

	/// <summary>
	/// Trading start minute.
	/// </summary>
	public int StartMinute { get => _startMinute.Value; set => _startMinute.Value = value; }

	/// <summary>
	/// Delay before placing orders in seconds.
	/// </summary>
	public int DelaySeconds { get => _delaySeconds.Value; set => _delaySeconds.Value = value; }

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Entry price offset in steps.
	/// </summary>
	public int PriceGap { get => _priceGap.Value; set => _priceGap.Value = value; }

	/// <summary>
	/// Pending order expiration in seconds. Zero means no expiration.
	/// </summary>
	public int Expiration { get => _expirationSeconds.Value; set => _expirationSeconds.Value = value; }

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool TrailStop { get => _trailStop.Value; set => _trailStop.Value = value; }

	/// <summary>
	/// Trailing stop distance in steps.
	/// </summary>
	public int TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }

	/// <summary>
	/// Additional gap before updating trailing stop.
	/// </summary>
	public int TrailingGap { get => _trailingGap.Value; set => _trailingGap.Value = value; }

	/// <summary>
	/// Allow buy side trading.
	/// </summary>
	public bool BuyTrade { get => _buyTrade.Value; set => _buyTrade.Value = value; }

	/// <summary>
	/// Allow sell side trading.
	/// </summary>
	public bool SellTrade { get => _sellTrade.Value; set => _sellTrade.Value = value; }

	/// <summary>
	/// Candle type for time tracking.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="NewsHourTradeStrategy"/>.
	/// </summary>
	public NewsHourTradeStrategy()
	{
		_startHour = Param(nameof(StartHour), 1).SetDisplay("Start Hour", "Hour to start", "Parameters").SetCanOptimize(true);
		_startMinute = Param(nameof(StartMinute), 1).SetDisplay("Start Minute", "Minute to start", "Parameters").SetCanOptimize(true);
		_delaySeconds = Param(nameof(DelaySeconds), 5).SetDisplay("Delay Seconds", "Delay before entry", "Parameters").SetCanOptimize(true);
		_volume = Param(nameof(Volume), 0.1m).SetDisplay("Volume", "Order volume", "Risk").SetCanOptimize(true);
		_stopLoss = Param(nameof(StopLoss), 20).SetDisplay("Stop Loss", "Stop distance", "Risk").SetCanOptimize(true);
		_takeProfit = Param(nameof(TakeProfit), 50).SetDisplay("Take Profit", "Take profit distance", "Risk").SetCanOptimize(true);
		_priceGap = Param(nameof(PriceGap), 10).SetDisplay("Price Gap", "Price offset", "Parameters").SetCanOptimize(true);
		_expirationSeconds = Param(nameof(Expiration), 60).SetDisplay("Expiration", "Order expiration", "Parameters").SetCanOptimize(true);
		_trailStop = Param(nameof(TrailStop), false).SetDisplay("Use Trailing", "Enable trailing stop", "Risk").SetCanOptimize(true);
		_trailingStop = Param(nameof(TrailingStop), 20).SetDisplay("Trailing Stop", "Trailing distance", "Risk").SetCanOptimize(true);
		_trailingGap = Param(nameof(TrailingGap), 10).SetDisplay("Trailing Gap", "Additional trailing gap", "Risk").SetCanOptimize(true);
		_buyTrade = Param(nameof(BuyTrade), true).SetDisplay("Buy Trade", "Enable buys", "Parameters");
		_sellTrade = Param(nameof(SellTrade), true).SetDisplay("Sell Trade", "Enable sells", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Working timeframe", "Parameters");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_lastTradeDay = default;
		_entryInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();
		_tickSize = Security.PriceStep ?? 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime.Date;
		if (date != _lastTradeDay && candle.OpenTime.Hour == StartHour && candle.OpenTime.Minute == StartMinute)
		{
			_lastTradeDay = date;
			PlacePendingOrders(candle.ClosePrice);
		}

		if (Position != 0)
		{
			CancelActiveOrders();
			ManagePosition(candle);
		}
	}

	private void PlacePendingOrders(decimal price)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var offset = PriceGap * _tickSize;
		var volume = Volume + Math.Abs(Position);

		if (BuyTrade)
			BuyStop(volume, price + offset);

		if (SellTrade)
			SellStop(volume, price - offset);
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (!_entryInitialized)
		{
			_entryPrice = PositionPrice;
			_stopPrice = _entryPrice + (Position > 0 ? -StopLoss : StopLoss) * _tickSize;
			_takePrice = _entryPrice + (Position > 0 ? TakeProfit : -TakeProfit) * _tickSize;
			_entryInitialized = true;
		}

		if (TrailStop)
			UpdateTrailingStop(candle.ClosePrice);

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || (TakeProfit > 0 && candle.HighPrice >= _takePrice))
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || (TakeProfit > 0 && candle.LowPrice <= _takePrice))
				BuyMarket(Math.Abs(Position));
		}

		if (Position == 0)
			_entryInitialized = false;
	}

	private void UpdateTrailingStop(decimal price)
	{
		if (Position > 0)
		{
			var newStop = price - TrailingStop * _tickSize;
			if (newStop > _stopPrice + TrailingGap * _tickSize)
				_stopPrice = newStop;
		}
		else if (Position < 0)
		{
			var newStop = price + TrailingStop * _tickSize;
			if (newStop < _stopPrice - TrailingGap * _tickSize)
				_stopPrice = newStop;
		}
	}
}
