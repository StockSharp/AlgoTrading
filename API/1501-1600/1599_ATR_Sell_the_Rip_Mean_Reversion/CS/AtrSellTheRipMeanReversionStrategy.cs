using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR Sell the Rip mean reversion short strategy.
/// Shorts when price rises above EMA-based threshold, covers on new low.
/// Uses StandardDeviation as volatility measure instead of ATR.
/// </summary>
public class AtrSellTheRipMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _stdPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevLow;
	private decimal _prevStd;
	private decimal _prevEma;
	private bool _isReady;

	public int StdPeriod { get => _stdPeriod.Value; set => _stdPeriod.Value = value; }
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AtrSellTheRipMeanReversionStrategy()
	{
		_stdPeriod = Param(nameof(StdPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Period", "Standard deviation period", "Parameters");

		_multiplier = Param(nameof(Multiplier), 1.0m)
			.SetDisplay("Multiplier", "Multiplier for threshold", "Parameters");

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA length for trend filter", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevLow = 0;
		_prevStd = 0;
		_prevEma = 0;
		_isReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stdDev = new StandardDeviation { Length = StdPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(stdDev, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isReady)
		{
			_prevLow = candle.LowPrice;
			_prevStd = stdValue;
			_prevEma = emaValue;
			_isReady = true;
			return;
		}

		// Short condition: price exceeds previous bar's threshold above EMA (overextended)
		if (_prevStd > 0 && _prevEma > 0)
		{
			var upperThreshold = _prevEma + _prevStd * Multiplier;
			var shortCondition = candle.ClosePrice > upperThreshold;

			if (shortCondition && Position >= 0)
				SellMarket();
		}

		// Cover condition: close below previous low (mean reversion complete)
		if (Position < 0 && candle.ClosePrice < _prevLow)
			BuyMarket();

		_prevLow = candle.LowPrice;
		_prevStd = stdValue;
		_prevEma = emaValue;
	}
}
