using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Linear mean reversion strategy based on price z-score.
/// Buys when price is below the lower z-score threshold and sells when above the upper threshold.
/// Exits when z-score moves back toward zero or a fixed stop loss is hit.
/// </summary>
public class LinearMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _halfLife;
	private readonly StrategyParam<decimal> _scale;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private int _barsFromTrade;

	/// <summary>
	/// Lookback window for moving average and standard deviation.
	/// </summary>
	public int HalfLife { get => _halfLife.Value; set => _halfLife.Value = value; }

	/// <summary>
	/// Position scaling factor.
	/// </summary>
	public decimal Scale { get => _scale.Value; set => _scale.Value = value; }

	/// <summary>
	/// Z-score threshold for entries.
	/// </summary>
	public decimal EntryThreshold { get => _entryThreshold.Value; set => _entryThreshold.Value = value; }

	/// <summary>
	/// Z-score threshold for exits.
	/// </summary>
	public decimal ExitThreshold { get => _exitThreshold.Value; set => _exitThreshold.Value = value; }

	/// <summary>
	/// Fixed stop loss in price points.
	/// </summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Minimum bars between trade actions.
	/// </summary>
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	/// <summary>
	/// Type of candles used.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public LinearMeanReversionStrategy()
	{
		_halfLife = Param(nameof(HalfLife), 30)
			.SetGreaterThanZero()
			.SetDisplay("Half-Life", "Lookback window for mean and deviation", "General")
			
			.SetOptimize(20, 60, 5);

		_scale = Param(nameof(Scale), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Scale", "Position scaling factor", "General")
			
			.SetOptimize(1m, 3m, 1m);

		_entryThreshold = Param(nameof(EntryThreshold), 2.2m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Threshold", "Z-score entry threshold", "Parameters")
			
			.SetOptimize(1.5m, 3.5m, 0.2m);

		_exitThreshold = Param(nameof(ExitThreshold), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Exit Threshold", "Z-score exit threshold", "Parameters")
			
			.SetOptimize(0.3m, 1.2m, 0.1m);

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Points", "Fixed stop loss in price points", "Risk Management")
			
			.SetOptimize(20m, 100m, 10m);

		_cooldownBars = Param(nameof(CooldownBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Minimum bars between trade actions", "Risk Management")
			
			.SetOptimize(5, 30, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame())
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
		_entryPrice = 0;
		_barsFromTrade = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SMA { Length = HalfLife };
		var std = new StandardDeviation { Length = HalfLife };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, std, ProcessCandle)
			.Start();

		StartProtection(null, null);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
	private void ProcessCandle(ICandleMessage candle, decimal mean, decimal deviation)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (deviation == 0)
			return;

		var zscore = (candle.ClosePrice - mean) / deviation;
		var volume = Volume * Scale;
		_barsFromTrade++;

		if (_barsFromTrade < CooldownBars)
			return;

		if (volume <= 0)
			return;

		if (Position == 0)
		{
			if (zscore < -EntryThreshold)
			{
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
				_barsFromTrade = 0;
			}
			else if (zscore > EntryThreshold)
			{
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
				_barsFromTrade = 0;
			}

			return;
		}

		if (Position > 0)
		{
			var stopPrice = _entryPrice - StopLossPoints;
			if (candle.ClosePrice <= stopPrice || zscore >= -ExitThreshold)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = 0;
				_barsFromTrade = 0;
			}

			return;
		}

		if (Position < 0)
		{
			var stopPrice = _entryPrice + StopLossPoints;
			if (candle.ClosePrice >= stopPrice || zscore <= ExitThreshold)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = 0;
				_barsFromTrade = 0;
			}
		}
	}
}
