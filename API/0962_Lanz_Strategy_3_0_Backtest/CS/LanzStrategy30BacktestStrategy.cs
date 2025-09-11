using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// LANZ Strategy 3.0 trades breakouts of the Asian range using Fibonacci targets.
/// </summary>
public class LanzStrategy30BacktestStrategy : Strategy
{
	private readonly StrategyParam<bool> _useOptimizedFibo;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _refHigh;
	private decimal _refLow;
	private bool _rangeSession;
	private Order _entryOrder;

	private decimal? _entryPrice;
	private decimal? _tp;
	private decimal? _sl;
	private bool _directionDefined;
	private bool _isBuy;
	private bool _tradeExecuted;
	private bool _tradeExpired;
	private bool _orderSent;
	private bool _fallbackTriggered;
	private decimal _lastClose;

	/// <summary>
	/// Use optimized Fibonacci coefficients.
	/// </summary>
	public bool UseOptimizedFibo { get => _useOptimizedFibo.Value; set => _useOptimizedFibo.Value = value; }

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="LanzStrategy30BacktestStrategy"/>.
	/// </summary>
	public LanzStrategy30BacktestStrategy()
	{
		_useOptimizedFibo = Param(nameof(UseOptimizedFibo), true)
			.SetDisplay("Use Optimized Fibo", "Use optimized Fibonacci coefficients", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_refHigh = _refLow = 0m;
		_rangeSession = false;
		_entryOrder = null!;
		_entryPrice = _tp = _sl = null;
		_directionDefined = false;
		_isBuy = false;
		_tradeExecuted = false;
		_tradeExpired = false;
		_orderSent = false;
		_fallbackTriggered = false;
		_lastClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var t = candle.OpenTime;

		var rangeSession = t.TimeOfDay >= new TimeSpan(18, 0, 0) || t.TimeOfDay < new TimeSpan(1, 15, 0);
		var newSession = rangeSession && !_rangeSession;
		_rangeSession = rangeSession;

		if (newSession)
		{
			_refHigh = candle.HighPrice;
			_refLow = candle.LowPrice;
			_entryPrice = _tp = _sl = null;
			_directionDefined = false;
			_isBuy = false;
			_tradeExecuted = false;
			_tradeExpired = false;
			_orderSent = false;
			_fallbackTriggered = false;
			_entryOrder = null!;
		}
		else if (rangeSession)
		{
			_refHigh = Math.Max(_refHigh, candle.HighPrice);
			_refLow = Math.Min(_refLow, candle.LowPrice);
		}

		var decisionWindow = t.TimeOfDay >= new TimeSpan(1, 15, 0) && t.TimeOfDay < new TimeSpan(2, 15, 0);
		var entryWindow = t.TimeOfDay >= new TimeSpan(1, 15, 0) && t.TimeOfDay < new TimeSpan(8, 0, 0);
		var expireWindow = t.TimeOfDay >= new TimeSpan(8, 0, 0) && t.TimeOfDay < new TimeSpan(8, 1, 0);
		var fallbackTime = t.Hour == 2 && t.Minute == 15;
		var closeTime = t.Hour == 15 && t.Minute == 45;

		if (decisionWindow && !_directionDefined)
		{
			var fiboRange = _refHigh - _refLow;
			var asiaMid = (_refHigh + _refLow) / 2m;
			_isBuy = candle.ClosePrice < asiaMid;
			_entryPrice = _isBuy ? _refLow : _refHigh;

			if (UseOptimizedFibo)
			{
				_tp = _isBuy ? _refLow + 1.95m * fiboRange : _refHigh - 1.95m * fiboRange;
				_sl = _isBuy ? _refLow - 0.65m * fiboRange : _refHigh + 0.65m * fiboRange;
			}
			else
			{
				_tp = _isBuy ? _refLow + 2.25m * fiboRange : _refHigh - 2.25m * fiboRange;
				_sl = _isBuy ? _refLow - 0.75m * fiboRange : _refHigh + 0.75m * fiboRange;
			}

			_directionDefined = true;
		}

		if (_directionDefined && entryWindow && !_tradeExecuted && !_orderSent && !_tradeExpired && _entryPrice is decimal price)
		{
			_entryOrder = _isBuy ? BuyLimit(price, Volume) : SellLimit(price, Volume);
			_orderSent = true;
		}

		if (!_tradeExecuted && Position != 0)
		_tradeExecuted = true;

		if (fallbackTime && !_tradeExecuted && !_fallbackTriggered && _orderSent)
		{
			if (_entryOrder != null)
			CancelOrder(_entryOrder);

			var asiaMid = (_refHigh + _refLow) / 2m;
			var currentBuy = _lastClose < asiaMid;

			if (currentBuy == _isBuy)
			{
				_orderSent = false;
			}
			else
			{
				var fiboRange = _refHigh - _refLow;
				_isBuy = !_isBuy;
				_entryPrice = _isBuy ? _refLow : _refHigh;

				if (UseOptimizedFibo)
				{
					_tp = _isBuy ? _refLow + 1.95m * fiboRange : _refHigh - 1.95m * fiboRange;
					_sl = _isBuy ? _refLow - 0.65m * fiboRange : _refHigh + 0.65m * fiboRange;
				}
				else
				{
					_tp = _isBuy ? _refLow + 2.25m * fiboRange : _refHigh - 2.25m * fiboRange;
					_sl = _isBuy ? _refLow - 0.75m * fiboRange : _refHigh + 0.75m * fiboRange;
				}

				_orderSent = false;
			}

			_directionDefined = true;
			_fallbackTriggered = true;
		}

		if (expireWindow && !_tradeExecuted && !_tradeExpired)
		{
			if (_entryOrder != null)
			CancelOrder(_entryOrder);
			_tradeExpired = true;
		}

		if (_tradeExecuted && _tp is decimal tp && _sl is decimal sl)
		{
			if (Position > 0)
			{
				if (candle.LowPrice <= sl)
				SellMarket(Math.Abs(Position));
				else if (candle.HighPrice >= tp)
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0)
			{
				if (candle.HighPrice >= sl)
				BuyMarket(Math.Abs(Position));
				else if (candle.LowPrice <= tp)
				BuyMarket(Math.Abs(Position));
			}
		}

		if (closeTime && _tradeExecuted)
		ClosePosition();

		_lastClose = candle.ClosePrice;
	}
}
