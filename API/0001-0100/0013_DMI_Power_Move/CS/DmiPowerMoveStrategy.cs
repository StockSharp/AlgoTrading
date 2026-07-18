using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on DMI (Directional Movement Index) power moves.
/// Enters long when +DI exceeds -DI by threshold and ADX is strong.
/// Enters short when -DI exceeds +DI by threshold and ADX is strong.
/// </summary>
public class DmiPowerMoveStrategy : Strategy
{
	private readonly StrategyParam<int> _dmiPeriod;
	private readonly StrategyParam<decimal> _diDifferenceThreshold;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private int _prevSignal; // -1 bearish, 0 neutral, 1 bullish

	/// <summary>
	/// Period for DMI calculation.
	/// </summary>
	public int DmiPeriod
	{
		get => _dmiPeriod.Value;
		set => _dmiPeriod.Value = value;
	}

	/// <summary>
	/// Minimum difference between +DI and -DI to generate a signal.
	/// </summary>
	public decimal DiDifferenceThreshold
	{
		get => _diDifferenceThreshold.Value;
		set => _diDifferenceThreshold.Value = value;
	}

	/// <summary>
	/// Minimum ADX value to consider trend strong enough for entry.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
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
	/// Initialize the DMI Power Move strategy.
	/// </summary>
	public DmiPowerMoveStrategy()
	{
		_dmiPeriod = Param(nameof(DmiPeriod), 14)
			.SetDisplay("DMI Period", "Period for DMI calculation", "Indicators")
			.SetOptimize(10, 20, 2);

		_diDifferenceThreshold = Param(nameof(DiDifferenceThreshold), 3m)
			.SetDisplay("DI Difference Threshold", "Min difference between +DI and -DI", "Trading parameters")
			.SetOptimize(2, 8, 1);

		_adxThreshold = Param(nameof(AdxThreshold), 15m)
			.SetDisplay("ADX Threshold", "Minimum ADX value for entry", "Trading parameters")
			.SetOptimize(10, 25, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevSignal = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var dmi = new AverageDirectionalIndex { Length = DmiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(dmi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, dmi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue dmiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (dmiValue is not AverageDirectionalIndexValue adxTyped)
			return;

		decimal adx, plusDi, minusDi;
		try
		{
			if (adxTyped.MovingAverage is not decimal a ||
				adxTyped.Dx.Plus is not decimal p ||
				adxTyped.Dx.Minus is not decimal m)
				return;
			adx = a;
			plusDi = p;
			minusDi = m;
		}
		catch (IndexOutOfRangeException)
		{
			return;
		}

		var diDiff = plusDi - minusDi;

		// Determine current directional signal (ignoring neutral)
		int signal;
		if (diDiff > DiDifferenceThreshold && adx > AdxThreshold)
			signal = 1; // bullish
		else if (diDiff < -DiDifferenceThreshold && adx > AdxThreshold)
			signal = -1; // bearish
		else
			signal = _prevSignal; // keep previous signal when neutral

		// Trade only on directional change
		if (signal != _prevSignal && signal != 0)
		{
			if (signal == 1 && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (signal == -1 && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}

			_prevSignal = signal;
		}
	}
}
