namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that reopens positions once profit reaches a target in points.
/// </summary>
public class ReOpenPositionsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _profitThreshold;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private int _openedCount;
	private decimal _lastEntryPrice;
	private decimal _currentStop;
	private decimal _currentTake;

	public decimal ProfitThreshold
	{
		get => _profitThreshold.Value;
		set => _profitThreshold.Value = value;
	}

	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ReOpenPositionsStrategy()
	{
		_profitThreshold = Param(nameof(ProfitThreshold), 300m)
			.SetDisplay("Profit Threshold", "Points to reopen a position", "Parameters");

		_maxPositions = Param(nameof(MaxPositions), 10)
			.SetDisplay("Max Positions", "Maximum number of positions", "Parameters");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetDisplay("Stop Loss (pts)", "Stop loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetDisplay("Take Profit (pts)", "Take profit distance in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_openedCount = 0;
		_lastEntryPrice = default;
		_currentStop = default;
		_currentTake = default;
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

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			// Check for stop loss or take profit.
			if (close <= _currentStop || close >= _currentTake)
			{
				ClosePosition();
				_openedCount = 0;
			}
			else if (_openedCount < MaxPositions && close - _lastEntryPrice >= ProfitThreshold)
			{
				BuyMarket();
				_lastEntryPrice = close;
				_openedCount++;
				_currentStop = _lastEntryPrice - StopLossPoints;
				_currentTake = _lastEntryPrice + TakeProfitPoints;
			}
		}
		else if (Position < 0)
		{
			// Check for stop loss or take profit for short position.
			if (close >= _currentStop || close <= _currentTake)
			{
				ClosePosition();
				_openedCount = 0;
			}
			else if (_openedCount < MaxPositions && _lastEntryPrice - close >= ProfitThreshold)
			{
				SellMarket();
				_lastEntryPrice = close;
				_openedCount++;
				_currentStop = _lastEntryPrice + StopLossPoints;
				_currentTake = _lastEntryPrice - TakeProfitPoints;
			}
		}
		else
		{
			// Open the first position.
			BuyMarket();
			_lastEntryPrice = close;
			_openedCount = 1;
			_currentStop = _lastEntryPrice - StopLossPoints;
			_currentTake = _lastEntryPrice + TakeProfitPoints;
		}
	}
}
