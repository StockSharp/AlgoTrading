using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Random-entry martingale strategy with adjustable volume and distance.
/// </summary>
public class PureMartingaleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _slTp;
	private readonly StrategyParam<decimal> _lotsMultiplier;
	private readonly StrategyParam<decimal> _distanceMultiplier;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Random _random = new();

	private decimal _currentVolume;
	private decimal _currentDistance;
	private decimal _entryPrice;

	public decimal StopLossTakeProfit { get => _slTp.Value; set => _slTp.Value = value; }
	public decimal LotsMultiplier { get => _lotsMultiplier.Value; set => _lotsMultiplier.Value = value; }
	public decimal DistanceMultiplier { get => _distanceMultiplier.Value; set => _distanceMultiplier.Value = value; }
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PureMartingaleStrategy()
	{
		_slTp = Param(nameof(StopLossTakeProfit), 20m)
			.SetGreaterThanZero()
			.SetDisplay("SL/TP Distance", "Initial stop loss and take profit distance", "Risk");

		_lotsMultiplier = Param(nameof(LotsMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Lots Multiplier", "Volume multiplier after loss", "Risk");

		_distanceMultiplier = Param(nameof(DistanceMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Distance Multiplier", "SL/TP distance multiplier after loss", "Risk");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Initial trade volume", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for trade timing", "General");
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

		_currentVolume = Volume;
		_currentDistance = StopLossTakeProfit;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentVolume = Volume;
		_currentDistance = StopLossTakeProfit;

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
		// Process only finished candles.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the strategy is ready to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
		{
			// Choose a random direction for the next trade.
			_entryPrice = candle.ClosePrice;

			if (_random.Next(2) == 0)
				BuyMarket(_currentVolume);
			else
				SellMarket(_currentVolume);

			return;
		}

		if (Position > 0)
		{
			// Check target or stop for long position.
			if (candle.ClosePrice >= _entryPrice + _currentDistance)
			{
				SellMarket(Position);
				AdjustAfterTrade(true);
			}
			else if (candle.ClosePrice <= _entryPrice - _currentDistance)
			{
				SellMarket(Position);
				AdjustAfterTrade(false);
			}
		}
		else
		{
			// Position < 0, check target or stop for short position.
			if (candle.ClosePrice <= _entryPrice - _currentDistance)
			{
				BuyMarket(-Position);
				AdjustAfterTrade(true);
			}
			else if (candle.ClosePrice >= _entryPrice + _currentDistance)
			{
				BuyMarket(-Position);
				AdjustAfterTrade(false);
			}
		}
	}

	private void AdjustAfterTrade(bool wasProfit)
	{
		if (wasProfit)
		{
			// Reset parameters after a winning trade.
			_currentVolume = Volume;
			_currentDistance = StopLossTakeProfit;
		}
		else
		{
			// Increase volume and distance after a losing trade.
			_currentVolume *= LotsMultiplier;
			_currentDistance *= DistanceMultiplier;
		}
	}
}
