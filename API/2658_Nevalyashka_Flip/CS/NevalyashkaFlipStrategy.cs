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
/// Alternating long-short strategy that mirrors the original Nevalyashka MQL logic.
/// Opens an initial sell position and flips direction each time the position is closed.
/// </summary>
public class NevalyashkaFlipStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private Sides? _currentSide;
	private Sides? _lastCompletedSide;

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type for monitoring price.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NevalyashkaFlipStrategy"/> class.
	/// </summary>
	public NevalyashkaFlipStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 50)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pts)", "Stop loss distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pts)", "Take profit distance in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_currentSide = null;
		_lastCompletedSide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		var step = Security?.PriceStep ?? 1m;
		var stopDistance = StopLossPoints * step;
		var takeDistance = TakeProfitPoints * step;
		var price = candle.ClosePrice;

		// Check SL/TP for current position
		if (Position != 0 && _entryPrice > 0)
		{
			var hit = false;

			if (_currentSide == Sides.Buy)
			{
				if (stopDistance > 0 && candle.LowPrice <= _entryPrice - stopDistance)
					hit = true;
				if (takeDistance > 0 && candle.HighPrice >= _entryPrice + takeDistance)
					hit = true;
			}
			else if (_currentSide == Sides.Sell)
			{
				if (stopDistance > 0 && candle.HighPrice >= _entryPrice + stopDistance)
					hit = true;
				if (takeDistance > 0 && candle.LowPrice <= _entryPrice - takeDistance)
					hit = true;
			}

			if (hit)
			{
				// Close position
				if (Position > 0)
					SellMarket(Position);
				else if (Position < 0)
					BuyMarket(Math.Abs(Position));

				_lastCompletedSide = _currentSide;
				_currentSide = null;
				_entryPrice = 0m;
			}
		}

		// If flat, open next position
		if (Position == 0 && _currentSide == null)
		{
			// Alternate direction: start with sell, then flip
			var sideToOpen = _lastCompletedSide switch
			{
				Sides.Buy => Sides.Sell,
				Sides.Sell => Sides.Buy,
				_ => Sides.Sell,
			};

			if (sideToOpen == Sides.Buy)
				BuyMarket(Volume);
			else
				SellMarket(Volume);

			_currentSide = sideToOpen;
			_entryPrice = price;
		}
	}
}
