using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alternating buy/sell strategy with martingale take profit.
/// Opens trades in opposite direction after each close,
/// enlarges take profit after losses.
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

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal MartinFactor { get => _martinFactor.Value; set => _martinFactor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SuperTakeStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 3000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Base take profit distance", "Risk");

		_stopLoss = Param(nameof(StopLoss), 5000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");

		_martinFactor = Param(nameof(MartinFactor), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Martingale Factor", "Multiplier after losing trade", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0;
		_currentTakeProfit = 0;
		_lastTakeProfitDistance = 0;
		_isLong = false;
		_lastTradeWasLoss = false;
		_lastClosedWasBuy = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_lastTakeProfitDistance = TakeProfit;

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position != 0)
		{
			if (_isLong)
			{
				var profit = candle.ClosePrice - _entryPrice;

				if (profit >= _currentTakeProfit)
				{
					SellMarket();
					_lastTradeWasLoss = false;
					_lastTakeProfitDistance = _currentTakeProfit;
					_lastClosedWasBuy = true;
					_entryPrice = 0;
				}
				else if (profit <= -StopLoss)
				{
					SellMarket();
					_lastTradeWasLoss = true;
					_lastTakeProfitDistance = _currentTakeProfit;
					_lastClosedWasBuy = true;
					_entryPrice = 0;
				}
			}
			else
			{
				var profit = _entryPrice - candle.ClosePrice;

				if (profit >= _currentTakeProfit)
				{
					BuyMarket();
					_lastTradeWasLoss = false;
					_lastTakeProfitDistance = _currentTakeProfit;
					_lastClosedWasBuy = false;
					_entryPrice = 0;
				}
				else if (profit <= -StopLoss)
				{
					BuyMarket();
					_lastTradeWasLoss = true;
					_lastTakeProfitDistance = _currentTakeProfit;
					_lastClosedWasBuy = false;
					_entryPrice = 0;
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
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_isLong = true;
		}
		else
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_isLong = false;
		}
	}
}
