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
/// Strategy based on Hull Moving Average trend.
/// It enters long position when Hull MA is rising and short position when Hull MA is falling.
/// </summary>
public class HullMaTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	// Current state
	private decimal _prevHmaValue;

	/// <summary>
	/// Period for Hull Moving Average.
	/// </summary>
	public int HmaPeriod
	{
		get => _hmaPeriod.Value;
		set => _hmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for ATR calculation (stop-loss).
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for ATR to determine stop-loss distance.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the Hull MA Trend strategy.
	/// </summary>
	public HullMaTrendStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 500)
			.SetDisplay("HMA Period", "Period for Hull Moving Average", "Indicators")

			.SetOptimize(5, 15, 2);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for Average True Range (stop-loss)", "Risk parameters")
			
			.SetOptimize(10, 20, 2);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetDisplay("ATR Multiplier", "Multiplier for ATR to determine stop-loss distance", "Risk parameters")
			
			.SetOptimize(1, 3, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevHmaValue = default;

	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create indicators
		var hma = new HullMovingAverage { Length = HmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(hma, atr, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma);
			DrawOwnTrades(area);
		}

	}

	private void ProcessCandle(ICandleMessage candle, decimal hmaValue, decimal atrValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Skip the first received value for proper trend determination
		if (_prevHmaValue == 0)
		{
			_prevHmaValue = hmaValue;
			return;
		}

		// Determine HMA direction with minimum slope threshold
		var slopeThreshold = _prevHmaValue * 0.0002m; // 0.02% minimum change
		var isHmaRising = hmaValue - _prevHmaValue > slopeThreshold;
		var isHmaFalling = _prevHmaValue - hmaValue > slopeThreshold;

		// Trading logic - only on significant direction changes
		if (isHmaRising && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (isHmaFalling && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		// Update previous HMA value
		_prevHmaValue = hmaValue;
	}
}
