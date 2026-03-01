using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Random direction strategy with pip-based stop loss and take profit that doubles the volume after losses.
/// </summary>
public class StopLossTakeProfitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossDistance;
	private readonly StrategyParam<decimal> _takeProfitDistance;
	private readonly StrategyParam<decimal> _initialVolume;

	private readonly Random _random = new(System.Environment.TickCount);

	private decimal _currentVolume;
	private decimal _entryPrice;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal StopLossDistance
	{
		get => _stopLossDistance.Value;
		set => _stopLossDistance.Value = value;
	}

	public decimal TakeProfitDistance
	{
		get => _takeProfitDistance.Value;
		set => _takeProfitDistance.Value = value;
	}

	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	public StopLossTakeProfitStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for evaluating entries", "General");

		_stopLossDistance = Param(nameof(StopLossDistance), 200m)
			.SetDisplay("Stop Loss Distance", "Stop loss distance in price units", "Risk");

		_takeProfitDistance = Param(nameof(TakeProfitDistance), 200m)
			.SetDisplay("Take Profit Distance", "Take profit distance in price units", "Risk");

		_initialVolume = Param(nameof(InitialVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Starting order volume", "Risk");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_currentVolume = InitialVolume;
		_entryPrice = 0m;

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

		// Check SL/TP for open position
		if (Position != 0 && _entryPrice > 0)
		{
			if (Position > 0)
			{
				var hitStop = StopLossDistance > 0 && candle.LowPrice <= _entryPrice - StopLossDistance;
				var hitTake = TakeProfitDistance > 0 && candle.HighPrice >= _entryPrice + TakeProfitDistance;

				if (hitStop)
				{
					SellMarket();
					HandleStopLoss();
					return;
				}

				if (hitTake)
				{
					SellMarket();
					HandleTakeProfit();
					return;
				}
			}
			else if (Position < 0)
			{
				var hitStop = StopLossDistance > 0 && candle.HighPrice >= _entryPrice + StopLossDistance;
				var hitTake = TakeProfitDistance > 0 && candle.LowPrice <= _entryPrice - TakeProfitDistance;

				if (hitStop)
				{
					BuyMarket();
					HandleStopLoss();
					return;
				}

				if (hitTake)
				{
					BuyMarket();
					HandleTakeProfit();
					return;
				}
			}
		}

		// Enter new position when flat
		if (Position == 0)
		{
			var goShort = _random.Next(0, 2) == 0;

			if (goShort)
			{
				SellMarket();
			}
			else
			{
				BuyMarket();
			}

			_entryPrice = candle.ClosePrice;
		}
	}

	private void HandleStopLoss()
	{
		// Double volume on loss (martingale)
		_currentVolume *= 2m;

		var maxVol = Security?.MaxVolume;
		if (maxVol.HasValue && maxVol.Value > 0 && _currentVolume > maxVol.Value)
			_currentVolume = maxVol.Value;

		Volume = _currentVolume;
		_entryPrice = 0m;
	}

	private void HandleTakeProfit()
	{
		// Reset volume on profit
		_currentVolume = InitialVolume;
		Volume = _currentVolume;
		_entryPrice = 0m;
	}
}
