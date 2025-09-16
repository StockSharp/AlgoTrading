using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alternating buy/sell strategy with martingale-based take profit.
/// Opens trades in opposite direction after each close and enlarges
/// take profit after losses.
/// </summary>
public class SuperTakeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _martinFactor;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _currentTakeProfit;
	private decimal _lastTakeProfitDistance;
	private bool _isLong;
	private bool _lastTradeWasLoss;
	private bool? _lastClosedWasBuy;

	/// <summary>
	/// Base take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Multiplier for take profit after a losing trade.
	/// </summary>
	public decimal MartinFactor
	{
		get => _martinFactor.Value;
		set => _martinFactor.Value = value;
	}

	/// <summary>
	/// Candle type used to drive strategy logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="SuperTakeStrategy"/>.
	/// </summary>
	public SuperTakeStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Base Take Profit", "Base take profit distance in price units", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(5m, 20m, 5m);

		_stopLoss = Param(nameof(StopLoss), 15m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop loss distance in price units", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(5m, 30m, 5m);

		_martinFactor = Param(nameof(MartinFactor), 1.8m)
		.SetGreaterThanZero()
		.SetDisplay("Martingale Factor", "Multiplier for take profit after a losing trade", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.2m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to drive the logic", "General");
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

		_entryPrice = 0m;
		_currentTakeProfit = 0m;
		_lastTakeProfitDistance = 0m;
		_isLong = false;
		_lastTradeWasLoss = false;
		_lastClosedWasBuy = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_lastTakeProfitDistance = TakeProfit;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position != 0)
		{
			if (_isLong)
			{
				var profit = candle.ClosePrice - _entryPrice;

				if (profit >= _currentTakeProfit)
				{
					SellMarket(Position);
					_lastTradeWasLoss = false;
					_lastTakeProfitDistance = _currentTakeProfit;
					_lastClosedWasBuy = true;
					_entryPrice = 0m;
				}
				else if (profit <= -StopLoss)
				{
					SellMarket(Position);
					_lastTradeWasLoss = true;
					_lastTakeProfitDistance = _currentTakeProfit;
					_lastClosedWasBuy = true;
					_entryPrice = 0m;
				}
			}
			else
			{
				var profit = _entryPrice - candle.ClosePrice;

				if (profit >= _currentTakeProfit)
				{
					BuyMarket(Math.Abs(Position));
					_lastTradeWasLoss = false;
					_lastTakeProfitDistance = _currentTakeProfit;
					_lastClosedWasBuy = false;
					_entryPrice = 0m;
				}
				else if (profit <= -StopLoss)
				{
					BuyMarket(Math.Abs(Position));
					_lastTradeWasLoss = true;
					_lastTakeProfitDistance = _currentTakeProfit;
					_lastClosedWasBuy = false;
					_entryPrice = 0m;
				}
			}

			return;
		}

		var openBuy = _lastClosedWasBuy is not true;

		_currentTakeProfit = _lastTradeWasLoss
		? _lastTakeProfitDistance * MartinFactor
		: TakeProfit;

		if (openBuy)
		{
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_isLong = true;
		}
		else
		{
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_isLong = false;
		}
	}
}
