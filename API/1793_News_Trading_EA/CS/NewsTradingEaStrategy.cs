using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// News trading straddle strategy.
/// Places buy and sell stop orders around a scheduled news time.
/// </summary>
public class NewsTradingEaStrategy : Strategy
{
	private readonly StrategyParam<DateTime> _startDateTime;
	private readonly StrategyParam<int> _startStraddle;
	private readonly StrategyParam<int> _stopStraddle;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _distance;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _expiration;
	private readonly StrategyParam<DataType> _candleType;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private DateTimeOffset _straddleStart;
	private DateTimeOffset _straddleEnd;
	private DateTimeOffset _expirationTime;
	private decimal? _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;

	/// <summary>
	/// Base date and time for straddle.
	/// </summary>
	public DateTime StartDateTime { get => _startDateTime.Value; set => _startDateTime.Value = value; }

	/// <summary>
	/// Minutes after <see cref="StartDateTime"/> to begin tracking price.
	/// </summary>
	public int StartStraddle { get => _startStraddle.Value; set => _startStraddle.Value = value; }

	/// <summary>
	/// Minutes after start to stop modifying pending orders.
	/// </summary>
	public int StopStraddle { get => _stopStraddle.Value; set => _stopStraddle.Value = value; }

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Distance from current price in points.
	/// </summary>
	public decimal Distance { get => _distance.Value; set => _distance.Value = value; }

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Pending order lifetime in minutes.
	/// </summary>
	public int Expiration { get => _expiration.Value; set => _expiration.Value = value; }

	/// <summary>
	/// Candle type for price tracking.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="NewsTradingEaStrategy"/>.
	/// </summary>
	public NewsTradingEaStrategy()
	{
		_startDateTime = Param(nameof(StartDateTime), DateTime.Now)
			.SetDisplay("Start Date Time", "Base server time for straddle", "General");

		_startStraddle = Param(nameof(StartStraddle), 0)
			.SetDisplay("Start Straddle", "Delay in minutes after start time", "General");

		_stopStraddle = Param(nameof(StopStraddle), 15)
			.SetDisplay("Stop Straddle", "Duration in minutes for straddle", "General");

		_volume = Param(nameof(Volume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume in lots", "General");

		_distance = Param(nameof(Distance), 55m)
			.SetGreaterThanZero()
			.SetDisplay("Distance", "Distance from price in points", "General");

		_takeProfit = Param(nameof(TakeProfit), 30m)
			.SetDisplay("Take Profit", "Take profit in points", "General");

		_stopLoss = Param(nameof(StopLoss), 30m)
			.SetDisplay("Stop Loss", "Stop loss in points", "General");

		_expiration = Param(nameof(Expiration), 20)
			.SetDisplay("Expiration", "Pending order lifetime in minutes", "General");

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle Type", "Candle type for processing", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_straddleStart = StartDateTime + TimeSpan.FromMinutes(StartStraddle);
		_straddleEnd = _straddleStart + TimeSpan.FromMinutes(StopStraddle);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var now = candle.CloseTime;

		if (now < _straddleStart)
		{
			CancelPending();
			return;
		}

		if (now >= _straddleEnd)
		{
			CancelPending();
			return;
		}

		// Manage open position and protective exits
		if (Position != 0)
		{
			if (_entryPrice is null)
			{
				_entryPrice = candle.ClosePrice;
				var step = Security.PriceStep ?? 1m;
				_stopPrice = Position > 0 ? _entryPrice.Value - StopLoss * step : _entryPrice.Value + StopLoss * step;
				_targetPrice = Position > 0 ? _entryPrice.Value + TakeProfit * step : _entryPrice.Value - TakeProfit * step;
			}

			if (Position > 0)
			{
				if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice)
					SellMarket(Math.Abs(Position));
			}
			else
			{
				if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice)
					BuyMarket(Math.Abs(Position));
			}

			CancelPending();
			return;
		}

		var stepSize = Security.PriceStep ?? 1m;
		var buyPrice = candle.ClosePrice + Distance * stepSize;
		var sellPrice = candle.ClosePrice - Distance * stepSize;

		if (_buyStopOrder is null && _sellStopOrder is null)
		{
			_buyStopOrder = BuyStop(Volume, buyPrice);
			_sellStopOrder = SellStop(Volume, sellPrice);
			_expirationTime = now + TimeSpan.FromMinutes(Expiration);
		}
		else
		{
			ChangeOrder(_buyStopOrder!, buyPrice, _buyStopOrder!.Volume);
			ChangeOrder(_sellStopOrder!, sellPrice, _sellStopOrder!.Volume);
			_expirationTime = now + TimeSpan.FromMinutes(Expiration);
		}

		if (now >= _expirationTime)
			CancelPending();
	}

	private void CancelPending()
	{
		if (_buyStopOrder != null)
		{
			CancelOrder(_buyStopOrder);
			_buyStopOrder = null;
		}

		if (_sellStopOrder != null)
		{
			CancelOrder(_sellStopOrder);
			_sellStopOrder = null;
		}
	}
}
