namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Adaptive mean-reversion strategy converted from the UmnickTrader MQL5 expert advisor.
/// </summary>
public class UmnickTraderStrategy : Strategy
{
	// Number of trade results stored for adaptive calculations.
	private const int BufferLength = 8;

	private readonly StrategyParam<decimal> _stopBase;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _spread;
	private readonly StrategyParam<DataType> _candleType;

	// Adaptive buffers storing profit and loss distances observed recently.
	private readonly decimal[] _profitBuffer = new decimal[BufferLength];
	private readonly decimal[] _lossBuffer = new decimal[BufferLength];

	// Rolling state for signal detection and risk metrics.
	private decimal _lastAveragePrice;
	private decimal _entryPrice;
	private decimal _takeProfitPrice;
	private decimal _stopLossPrice;
	private decimal _maxProfit;
	private decimal _drawdown;
	private decimal _lastTradeProfit;

	private int _currentIndex;
	private int _currentDirection = 1;

	private bool _positionActive;
	private bool _isLongPosition;
	private bool _positionJustClosed;

	public decimal StopBase
	{
		get => _stopBase.Value;
		set => _stopBase.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public decimal Spread
	{
		get => _spread.Value;
		set => _spread.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public UmnickTraderStrategy()
	{
		_stopBase = Param(nameof(StopBase), 0.017m)
			.SetDisplay("Base Stop Distance", "Minimum average price move required to trigger evaluation.", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.005m, 0.05m, 0.005m);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Order volume for each position.", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 1m, 0.05m);

		_spread = Param(nameof(Spread), 0.0005m)
			.SetDisplay("Spread Padding", "Spread compensation used when updating adaptive buffers.", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.0001m, 0.002m, 0.0001m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Source candle series.", "General");
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

		_lastAveragePrice = 0m;
		_entryPrice = 0m;
		_takeProfitPrice = 0m;
		_stopLossPrice = 0m;
		_maxProfit = 0m;
		_drawdown = 0m;
		_lastTradeProfit = 0m;
		_currentIndex = 0;
		_currentDirection = 1;
		_positionActive = false;
		_isLongPosition = false;
		_positionJustClosed = false;

		for (var i = 0; i < BufferLength; i++)
		{
			_profitBuffer[i] = 0m;
			_lossBuffer[i] = 0m;
		}
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update metrics for an active position before generating new signals.
		UpdateOpenPosition(candle);

		// Average of OHLC replicates the MQL5 price smoothing logic.
		var averagePrice = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		if (!ShouldProcessAverage(averagePrice))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		var limitDistance = StopBase;
		var stopDistance = StopBase;

		decimal sumProfit = 0m;
		decimal sumLoss = 0m;
		for (var i = 0; i < BufferLength; i++)
		{
			sumProfit += _profitBuffer[i];
			sumLoss += _lossBuffer[i];
		}

		// Recalculate adaptive take-profit and stop-loss distances.
		if (sumProfit > StopBase / 2m)
			limitDistance = sumProfit / BufferLength;

		if (sumLoss > StopBase / 2m)
			stopDistance = sumLoss / BufferLength;

		if (_positionJustClosed)
		{
			_positionJustClosed = false;

			// Store the most recent excursion metrics.
			if (_lastTradeProfit > 0m)
			{
				_profitBuffer[_currentIndex] = _maxProfit - Spread * 3m;
				_lossBuffer[_currentIndex] = StopBase + Spread * 7m;
			}
			else
			{
				_profitBuffer[_currentIndex] = StopBase - Spread * 3m;
				_lossBuffer[_currentIndex] = _drawdown + Spread * 7m;
				_currentDirection = -_currentDirection;
			}

			_currentIndex++;
			if (_currentIndex >= BufferLength)
				_currentIndex = 0;
		}

		if (limitDistance <= 0m || stopDistance <= 0m)
			return;

		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		// Enter in the current direction using market orders.
		if (_currentDirection > 0)
			OpenLong(candle.ClosePrice, limitDistance, stopDistance, volume);
		else
			OpenShort(candle.ClosePrice, limitDistance, stopDistance, volume);
	}

	private bool ShouldProcessAverage(decimal averagePrice)
	{
		if (_lastAveragePrice == 0m)
		{
			_lastAveragePrice = averagePrice;
			return true;
		}

		var difference = Math.Abs(averagePrice - _lastAveragePrice);
		if (difference >= StopBase)
		{
			_lastAveragePrice = averagePrice;
			return true;
		}

		return false;
	}

	private void UpdateOpenPosition(ICandleMessage candle)
	{
		if (!_positionActive)
			return;

		// Track intrabar extremes to measure maximum favorable and adverse excursions.
		if (_isLongPosition)
		{
			var profitMove = candle.HighPrice - _entryPrice;
			if (profitMove > _maxProfit)
				_maxProfit = profitMove;

			var lossMove = _entryPrice - candle.LowPrice;
			if (lossMove > _drawdown)
				_drawdown = lossMove;

			if (candle.LowPrice <= _stopLossPrice)
			{
				CloseCurrentPosition(_stopLossPrice);
				return;
			}

			if (candle.HighPrice >= _takeProfitPrice)
			{
				CloseCurrentPosition(_takeProfitPrice);
				return;
			}
		}
		else
		{
			var profitMove = _entryPrice - candle.LowPrice;
			if (profitMove > _maxProfit)
				_maxProfit = profitMove;

			var lossMove = candle.HighPrice - _entryPrice;
			if (lossMove > _drawdown)
				_drawdown = lossMove;

			if (candle.HighPrice >= _stopLossPrice)
			{
				CloseCurrentPosition(_stopLossPrice);
				return;
			}

			if (candle.LowPrice <= _takeProfitPrice)
			{
				CloseCurrentPosition(_takeProfitPrice);
				return;
			}
		}
	}

	private void CloseCurrentPosition(decimal exitPrice)
	{
		// Close the position and record realized profit for buffer updates.
		var profit = _isLongPosition ? exitPrice - _entryPrice : _entryPrice - exitPrice;
		_positionActive = false;
		_isLongPosition = false;
		_entryPrice = 0m;
		_takeProfitPrice = 0m;
		_stopLossPrice = 0m;

		_lastTradeProfit = profit;
		_positionJustClosed = true;

		if (Position != 0)
			ClosePosition();
	}

	private void OpenLong(decimal price, decimal limitDistance, decimal stopDistance, decimal volume)
	{
		BuyMarket(volume);

		// Store trade parameters for managing exits on subsequent candles.
		_entryPrice = price;
		_takeProfitPrice = price + limitDistance;
		_stopLossPrice = price - stopDistance;
		_positionActive = true;
		_isLongPosition = true;
		_lastTradeProfit = 0m;
		_maxProfit = 0m;
		_drawdown = 0m;
	}

	private void OpenShort(decimal price, decimal limitDistance, decimal stopDistance, decimal volume)
	{
		SellMarket(volume);

		// Store trade parameters for managing exits on subsequent candles.
		_entryPrice = price;
		_takeProfitPrice = price - limitDistance;
		_stopLossPrice = price + stopDistance;
		_positionActive = true;
		_isLongPosition = false;
		_lastTradeProfit = 0m;
		_maxProfit = 0m;
		_drawdown = 0m;
	}
}
