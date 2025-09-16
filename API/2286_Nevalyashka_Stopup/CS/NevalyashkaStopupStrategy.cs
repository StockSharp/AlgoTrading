using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alternating martingale strategy.
/// Opens opposite direction after each trade and increases
/// stop loss and take profit distances after losses.
/// </summary>
public class NevalyashkaStopupStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _martingaleCoeff;
	private readonly StrategyParam<bool> _stopAfterProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _currentStopLoss;
	private decimal _currentTakeProfit;
	private decimal _baseStopLoss;
	private decimal _baseTakeProfit;
	private bool _nextIsBuy;

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Multiplier for stop and target after loss.
	/// </summary>
	public decimal MartingaleCoeff
	{
		get => _martingaleCoeff.Value;
		set => _martingaleCoeff.Value = value;
	}

	/// <summary>
	/// Stop strategy after a profitable trade.
	/// </summary>
	public bool StopAfterProfit
	{
		get => _stopAfterProfit.Value;
		set => _stopAfterProfit.Value = value;
	}

	/// <summary>
	/// Type of candles used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="NevalyashkaStopupStrategy"/>.
	/// </summary>
	public NevalyashkaStopupStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 150)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "General")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "General")
			.SetCanOptimize(true);

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume of each order", "Trading")
			.SetCanOptimize(true);

		_martingaleCoeff = Param(nameof(MartingaleCoeff), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Martingale Coeff", "Multiplier applied after loss", "Risk")
			.SetCanOptimize(true);

		_stopAfterProfit = Param(nameof(StopAfterProfit), false)
			.SetDisplay("Stop After Profit", "Stop strategy after profit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var step = Security.Step ?? 1m;
		_baseStopLoss = StopLossPoints * step;
		_baseTakeProfit = TakeProfitPoints * step;
		_currentStopLoss = _baseStopLoss;
		_currentTakeProfit = _baseTakeProfit;
		_nextIsBuy = false; // first trade will be sell

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var closePrice = candle.ClosePrice;

		if (Position == 0)
		{
			if (_nextIsBuy)
				BuyMarket(OrderVolume);
			else
				SellMarket(OrderVolume);

			_entryPrice = closePrice;
			return;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _entryPrice - _currentStopLoss)
			{
				SellMarket(Position);
				OnTradeClosed(false);
			}
			else if (candle.HighPrice >= _entryPrice + _currentTakeProfit)
			{
				SellMarket(Position);
				OnTradeClosed(true);
			}
		}
		else if (Position < 0)
		{
			var volume = Math.Abs(Position);
			if (candle.HighPrice >= _entryPrice + _currentStopLoss)
			{
				BuyMarket(volume);
				OnTradeClosed(false);
			}
			else if (candle.LowPrice <= _entryPrice - _currentTakeProfit)
			{
				BuyMarket(volume);
				OnTradeClosed(true);
			}
		}
	}

	private void OnTradeClosed(bool wasProfit)
	{
		if (wasProfit)
		{
			_currentStopLoss = _baseStopLoss;
			_currentTakeProfit = _baseTakeProfit;

			if (StopAfterProfit)
			{
				Stop();
				return;
			}
		}
		else
		{
			_currentStopLoss *= MartingaleCoeff;
			_currentTakeProfit *= MartingaleCoeff;
		}

		_nextIsBuy = !_nextIsBuy;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = 0m;
		_currentStopLoss = 0m;
		_currentTakeProfit = 0m;
		_baseStopLoss = 0m;
		_baseTakeProfit = 0m;
		_nextIsBuy = false;
	}
}
