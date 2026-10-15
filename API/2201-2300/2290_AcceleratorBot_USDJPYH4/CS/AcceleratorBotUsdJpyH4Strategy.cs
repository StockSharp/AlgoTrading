using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Accelerator bot strategy using ADX trend filter and Stochastic crossover.
/// In strong trends (ADX > threshold): trades on candle direction.
/// In ranges (ADX below threshold): trades on Stochastic K/D cross.
/// </summary>
public class AcceleratorBotUsdJpyH4Strategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevK;
	private decimal? _prevD;

	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AcceleratorBotUsdJpyH4Strategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetDisplay("ADX Threshold", "Minimum ADX to use trend rules", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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
		_prevK = null;
		_prevD = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevK = null;
		_prevD = null;

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var stochastic = new StochasticOscillator
		{
			K = { Length = 8 },
			D = { Length = 3 },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxVal, IIndicatorValue stochVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (adxVal is not IAverageDirectionalIndexValue adxTyped || adxTyped.MovingAverage is not decimal adx)
			return;

		if (stochVal is not IStochasticOscillatorValue stochTyped || stochTyped.K is not decimal stochK || stochTyped.D is not decimal stochD)
			return;

		if (_prevK is null || _prevD is null)
		{
			_prevK = stochK;
			_prevD = stochD;
			return;
		}

		var bullCross = _prevK <= _prevD && stochK > stochD;
		var bearCross = _prevK >= _prevD && stochK < stochD;

		// In trending market: use ADX + candle direction
		if (adx > AdxThreshold)
		{
			if (candle.ClosePrice > candle.OpenPrice && bullCross && Position <= 0)
				BuyMarket();
			else if (candle.ClosePrice < candle.OpenPrice && bearCross && Position >= 0)
				SellMarket();
		}
		else
		{
			// In range: use stochastic crossover
			if (bullCross && Position <= 0)
				BuyMarket();
			else if (bearCross && Position >= 0)
				SellMarket();
		}

		_prevK = stochK;
		_prevD = stochD;
	}
}
