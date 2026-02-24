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
/// VR Overturn strategy implementing martingale and anti-martingale principles.
/// Alternates direction and adjusts volume based on win/loss results.
/// </summary>
public class VROverturnStrategy : Strategy
{
	public enum TradeModes
	{
		Martingale,
		AntiMartingale
	}

	private readonly StrategyParam<TradeModes> _tradeMode;
	private readonly StrategyParam<Sides> _startSide;
	private readonly StrategyParam<int> _takePoints;
	private readonly StrategyParam<int> _stopPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private Sides _currentSide;
	private int _consecutiveLosses;

	public TradeModes Mode { get => _tradeMode.Value; set => _tradeMode.Value = value; }
	public Sides StartSide { get => _startSide.Value; set => _startSide.Value = value; }
	public int TakeProfit { get => _takePoints.Value; set => _takePoints.Value = value; }
	public int StopLoss { get => _stopPoints.Value; set => _stopPoints.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VROverturnStrategy()
	{
		_tradeMode = Param(nameof(Mode), TradeModes.Martingale)
			.SetDisplay("Trade Mode", "Martingale or AntiMartingale", "General");

		_startSide = Param(nameof(StartSide), Sides.Buy)
			.SetDisplay("Start Side", "Initial trade direction", "General");

		_takePoints = Param(nameof(TakeProfit), 300)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk");

		_stopPoints = Param(nameof(StopLoss), 300)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_currentSide = StartSide;
		_consecutiveLosses = 0;

		var sma = new SimpleMovingAverage { Length = 10 };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security.PriceStep ?? 1m;
		var price = candle.ClosePrice;

		if (Position == 0)
		{
			_entryPrice = price;
			if (_currentSide == Sides.Buy)
				BuyMarket();
			else
				SellMarket();
			return;
		}

		if (Position > 0)
		{
			var tp = _entryPrice + TakeProfit * step;
			var sl = _entryPrice - StopLoss * step;

			if (price >= tp)
			{
				SellMarket();
				OnTradeResult(true);
			}
			else if (price <= sl)
			{
				SellMarket();
				OnTradeResult(false);
			}
		}
		else if (Position < 0)
		{
			var tp = _entryPrice - TakeProfit * step;
			var sl = _entryPrice + StopLoss * step;

			if (price <= tp)
			{
				BuyMarket();
				OnTradeResult(true);
			}
			else if (price >= sl)
			{
				BuyMarket();
				OnTradeResult(false);
			}
		}
	}

	private void OnTradeResult(bool isWin)
	{
		if (Mode == TradeModes.Martingale)
		{
			if (isWin)
				_consecutiveLosses = 0;
			else
				_consecutiveLosses++;
		}
		else
		{
			if (isWin)
				_consecutiveLosses++;
			else
				_consecutiveLosses = 0;
		}

		if (!isWin)
			_currentSide = _currentSide == Sides.Buy ? Sides.Sell : Sides.Buy;
	}
}
