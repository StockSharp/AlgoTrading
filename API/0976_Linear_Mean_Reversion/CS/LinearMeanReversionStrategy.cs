using System;
using System.Collections.Generic;

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
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_scale = Param(nameof(Scale), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Scale", "Position scaling factor", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 1m);

		_entryThreshold = Param(nameof(EntryThreshold), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Threshold", "Z-score entry threshold", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_exitThreshold = Param(nameof(ExitThreshold), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Exit Threshold", "Z-score exit threshold", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Points", "Fixed stop loss in price points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(20m, 100m, 10m);

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
		_entryPrice = 0;
		_isLong = false;
	}
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SimpleMovingAverage { Length = HalfLife };
		var std = new StandardDeviation { Length = HalfLife };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, std, ProcessCandle)
			.Start();

		StartProtection();

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
			BuyMarket(volume + Math.Max(0m, -Position));
			_entryPrice = candle.ClosePrice;
			_isLong = true;
		}
		else if (Position >= 0 && zscore > EntryThreshold)
		{
			SellMarket(volume + Math.Max(0m, Position));
			_entryPrice = candle.ClosePrice;
			_isLong = false;
		}
		else if (Position > 0 && zscore > -ExitThreshold)
		{
			SellMarket(Math.Abs(Position));
			_entryPrice = 0;
		}
		else if (Position < 0 && zscore < ExitThreshold)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = 0;
		}

		if (Position > 0 && candle.ClosePrice <= _entryPrice - StopLossPoints)
		{
			SellMarket(Math.Abs(Position));
			_entryPrice = 0;
		}
		else if (Position < 0 && candle.ClosePrice >= _entryPrice + StopLossPoints)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = 0;
		}
	}
}
