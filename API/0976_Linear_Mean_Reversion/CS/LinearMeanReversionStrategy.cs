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
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _isLong;

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
	/// Type of candles used.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public LinearMeanReversionStrategy()
	{
		_halfLife = Param(nameof(HalfLife), 14)
			.SetGreaterThanZero()
			.SetDisplay("Half-Life", "Lookback window for mean and deviation", "General")
			
			.SetOptimize(10, 30, 5);

		_scale = Param(nameof(Scale), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Scale", "Position scaling factor", "General")
			
			.SetOptimize(1m, 3m, 1m);

		_entryThreshold = Param(nameof(EntryThreshold), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Threshold", "Z-score entry threshold", "Parameters")
			
			.SetOptimize(1m, 3m, 0.5m);

		_exitThreshold = Param(nameof(ExitThreshold), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Exit Threshold", "Z-score exit threshold", "Parameters")
			
			.SetOptimize(0.1m, 1m, 0.1m);

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Points", "Fixed stop loss in price points", "Risk Management")
			
			.SetOptimize(20m, 100m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_isLong = false;
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

		if (Position <= 0 && zscore < -EntryThreshold)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (Position >= 0 && zscore > EntryThreshold)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (Position > 0 && zscore > -ExitThreshold)
		{
			SellMarket();
			_entryPrice = 0;
		}
		else if (Position < 0 && zscore < ExitThreshold)
		{
			BuyMarket();
			_entryPrice = 0;
		}
	}
}
