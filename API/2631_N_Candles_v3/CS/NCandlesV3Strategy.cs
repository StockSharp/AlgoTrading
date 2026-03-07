using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that opens positions after detecting a sequence of candles with the same direction.
/// Uses StartProtection for take profit and stop loss management.
/// </summary>
public class NCandlesV3Strategy : Strategy
{
	private readonly StrategyParam<int> _identicalCandles;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private int _sequenceDirection;
	private int _sequenceCount;

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of consecutive candles required before entering a trade.
	/// </summary>
	public int IdenticalCandles
	{
		get => _identicalCandles.Value;
		set => _identicalCandles.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public NCandlesV3Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles analysed by the strategy", "General");

		_identicalCandles = Param(nameof(IdenticalCandles), 3)
			.SetRange(1, 10)
			.SetDisplay("Identical Candles", "Required number of equal candles", "Pattern");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetRange(0m, 500m)
			.SetDisplay("Take Profit Points", "Take profit distance in price steps", "Risk Management");

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetRange(0m, 500m)
			.SetDisplay("Stop Loss Points", "Stop loss distance in price steps", "Risk Management");
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
		_sequenceDirection = 0;
		_sequenceCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sequenceDirection = 0;
		_sequenceCount = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		var takeProfit = TakeProfitPoints > 0 ? TakeProfitPoints * step : (decimal?)null;
		var stopLoss = StopLossPoints > 0 ? StopLossPoints * step : (decimal?)null;

		if (takeProfit != null || stopLoss != null)
			StartProtection(takeProfit ?? 0, stopLoss ?? 0);

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

		var direction = GetDirection(candle);
		UpdateSequence(direction);

		if (_sequenceCount < IdenticalCandles)
			return;

		if (_sequenceDirection > 0 && Position <= 0)
		{
			BuyMarket();
		}
		else if (_sequenceDirection < 0 && Position >= 0)
		{
			SellMarket();
		}
	}

	private static int GetDirection(ICandleMessage candle)
	{
		if (candle.ClosePrice > candle.OpenPrice)
			return 1;
		if (candle.ClosePrice < candle.OpenPrice)
			return -1;
		return 0;
	}

	private void UpdateSequence(int direction)
	{
		if (direction == 0)
		{
			_sequenceDirection = 0;
			_sequenceCount = 0;
			return;
		}

		if (_sequenceDirection == direction)
		{
			_sequenceCount++;
		}
		else
		{
			_sequenceDirection = direction;
			_sequenceCount = 1;
		}
	}
}
