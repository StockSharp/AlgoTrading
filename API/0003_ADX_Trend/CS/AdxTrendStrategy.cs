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
/// Strategy based on Average Directional Index (ADX) trend.
/// It enters long position when ADX > 25 and price > MA, and short position when ADX > 25 and price < MA.
/// </summary>
public class AdxTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _adxExitThreshold;
	private readonly StrategyParam<DataType> _candleType;
	
	// Current trend state
	private bool _adxAboveThreshold;
	private decimal _prevAdxValue;
	private decimal _prevMaValue;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Moving Average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// ADX threshold to exit position.
	/// </summary>
	public int AdxExitThreshold
	{
		get => _adxExitThreshold.Value;
		set => _adxExitThreshold.Value = value;
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
	/// Initialize the ADX Trend strategy.
	/// </summary>
	public AdxTrendStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 50)
			.SetDisplay("ADX Period", "Period for calculating ADX indicator", "Indicators")

			.SetOptimize(10, 30, 2);

		_maPeriod = Param(nameof(MaPeriod), 200)
			.SetDisplay("MA Period", "Period for calculating Moving Average", "Indicators")

			.SetOptimize(20, 100, 10);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetDisplay("ATR Multiplier", "Multiplier for stop-loss based on ATR", "Risk parameters")
			
			.SetOptimize(1, 3, 0.5m);

		_adxExitThreshold = Param(nameof(AdxExitThreshold), 20)
			.SetDisplay("ADX Exit Threshold", "ADX level below which to exit position", "Exit parameters")
			
			.SetOptimize(15, 25, 1);

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
		_adxAboveThreshold = default;
		_prevAdxValue = default;
		_prevMaValue = default;

	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create indicators
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var ma = new SMA { Length = MaPeriod };

		var currentAdxMa = 0m;
		var currentMaValue = 0m;

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, (candle, adxVal) =>
			{
				if (adxVal is AverageDirectionalIndexValue adxTyped && adxTyped.MovingAverage is decimal adxMaVal)
					currentAdxMa = adxMaVal;
			})
			.Bind(ma, (candle, maVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				currentMaValue = maVal;

				ProcessCandle(candle, currentAdxMa, currentMaValue);
			})
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}

	}

	private void ProcessCandle(ICandleMessage candle, decimal adxMa, decimal maValue)
	{
		if (adxMa == 0 || maValue == 0)
		{
			_prevMaValue = maValue;
			_prevAdxValue = adxMa;
			return;
		}

		var isPriceAboveMa = candle.ClosePrice > maValue;
		var wasPriceAboveMa = _prevMaValue != 0 && candle.OpenPrice > _prevMaValue;
		var isAdxStrong = adxMa > 25;

		// Only trade on MA crossover when ADX is strong
		if (_prevMaValue != 0 && isAdxStrong && wasPriceAboveMa != isPriceAboveMa)
		{
			if (isPriceAboveMa && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (!isPriceAboveMa && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		// Update previous values
		_prevAdxValue = adxMa;
		_prevMaValue = maValue;
		_adxAboveThreshold = isAdxStrong;
	}
}
