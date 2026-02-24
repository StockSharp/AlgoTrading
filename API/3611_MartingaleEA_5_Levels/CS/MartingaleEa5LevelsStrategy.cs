using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale averaging strategy converted from "MartingaleEA-5 Levels".
/// Opens initial position on simple momentum, then averages down with
/// increasing lot sizes up to 5 levels. Closes when floating profit
/// reaches target or stop threshold.
/// </summary>
public class MartingaleEa5LevelsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _maxAdditions;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private SimpleMovingAverage _sma;
	private decimal? _prevClose;
	private decimal? _prevMa;

	private readonly List<(decimal price, decimal vol)> _entries = new();
	private int _additions;
	private decimal _lastVolume;
	private Sides? _activeSide;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	public int MaxAdditions
	{
		get => _maxAdditions.Value;
		set => _maxAdditions.Value = value;
	}

	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	public MartingaleEa5LevelsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "SMA period for entry signal", "Indicators");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Multiplier for each martingale level", "Money Management");

		_maxAdditions = Param(nameof(MaxAdditions), 4)
			.SetDisplay("Max Additions", "Maximum martingale additions", "Money Management");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Floating profit % to close group", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Floating loss % to close group", "Risk");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = MaPeriod };
		_prevClose = null;
		_prevMa = null;
		_entries.Clear();
		_additions = 0;
		_lastVolume = 0;
		_activeSide = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevMa = smaValue;
			return;
		}

		var close = candle.ClosePrice;
		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Check martingale closure first
		if (_entries.Count > 0)
		{
			var floatingPnl = CalculateFloatingPnl(close);
			var totalCost = CalculateTotalCost();

			if (totalCost > 0)
			{
				var pnlPercent = floatingPnl / totalCost * 100m;

				if (pnlPercent >= TakeProfitPercent || pnlPercent <= -StopLossPercent)
				{
					// Close entire position
					if (Position > 0)
						SellMarket(Position);
					else if (Position < 0)
						BuyMarket(Math.Abs(Position));

					_entries.Clear();
					_additions = 0;
					_lastVolume = 0;
					_activeSide = null;

					_prevClose = close;
					_prevMa = smaValue;
					return;
				}
			}

			// Check for martingale additions
			if (_additions < MaxAdditions)
			{
				var avgPrice = CalculateAvgPrice();
				var adversePercent = _activeSide == Sides.Buy
					? (avgPrice - close) / avgPrice * 100m
					: (close - avgPrice) / avgPrice * 100m;

				// Add at each 0.3% adverse move beyond previous level
				var threshold = 0.3m * (_additions + 1);
				if (adversePercent >= threshold)
				{
					var nextVol = _lastVolume * VolumeMultiplier;
					if (nextVol < 1) nextVol = 1;

					if (_activeSide == Sides.Buy)
					{
						BuyMarket(nextVol);
						_entries.Add((close, nextVol));
					}
					else
					{
						SellMarket(nextVol);
						_entries.Add((close, nextVol));
					}

					_lastVolume = nextVol;
					_additions++;
				}
			}
		}

		// Initial entry signal: MA crossover
		if (_prevClose != null && _prevMa != null && _activeSide == null)
		{
			var buySignal = _prevClose.Value < _prevMa.Value && close > smaValue;
			var sellSignal = _prevClose.Value > _prevMa.Value && close < smaValue;

			if (buySignal)
			{
				BuyMarket(volume);
				_entries.Clear();
				_entries.Add((close, volume));
				_additions = 0;
				_lastVolume = volume;
				_activeSide = Sides.Buy;
			}
			else if (sellSignal)
			{
				SellMarket(volume);
				_entries.Clear();
				_entries.Add((close, volume));
				_additions = 0;
				_lastVolume = volume;
				_activeSide = Sides.Sell;
			}
		}

		_prevClose = close;
		_prevMa = smaValue;
	}

	private decimal CalculateFloatingPnl(decimal currentPrice)
	{
		var pnl = 0m;
		foreach (var (price, vol) in _entries)
		{
			if (_activeSide == Sides.Buy)
				pnl += (currentPrice - price) * vol;
			else
				pnl += (price - currentPrice) * vol;
		}
		return pnl;
	}

	private decimal CalculateTotalCost()
	{
		var cost = 0m;
		foreach (var (price, vol) in _entries)
			cost += price * vol;
		return cost;
	}

	private decimal CalculateAvgPrice()
	{
		var totalVol = 0m;
		var totalCost = 0m;
		foreach (var (price, vol) in _entries)
		{
			totalVol += vol;
			totalCost += price * vol;
		}
		return totalVol > 0 ? totalCost / totalVol : 0;
	}
}
