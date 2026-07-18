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
/// Short Only 10 Bar Low Pullback Strategy - sells on new lows with IBS filter and EMA trend confirmation.
/// </summary>
public class ShortOnly10BarLowPullbackStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lowestPeriod;
	private readonly StrategyParam<decimal> _ibsThreshold;
	private readonly StrategyParam<int> _emaPeriod;

	private decimal _prevLowest;
	private decimal _prevLow;
	private bool _isReady;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int LowestPeriod { get => _lowestPeriod.Value; set => _lowestPeriod.Value = value; }
	public decimal IbsThreshold { get => _ibsThreshold.Value; set => _ibsThreshold.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public ShortOnly10BarLowPullbackStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");

		_lowestPeriod = Param(nameof(LowestPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Lowest Low Period", "Lookback for lowest low", "Indicators");

		_ibsThreshold = Param(nameof(IbsThreshold), 0.85m)
			.SetDisplay("IBS Threshold", "Internal bar strength threshold", "Signals");

		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for filter", "Trend Filter");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevLowest = 0;
		_prevLow = 0;
		_isReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var lowest = new Lowest { Length = LowestPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(lowest, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal lowestVal, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isReady)
		{
			_prevLowest = lowestVal;
			_prevLow = candle.LowPrice;
			_isReady = true;
			return;
		}

		var range = candle.HighPrice - candle.LowPrice;
		if (range == 0)
		{
			_prevLowest = lowestVal;
			_prevLow = candle.LowPrice;
			return;
		}

		var ibs = (candle.ClosePrice - candle.LowPrice) / range;

		// Short: new low breakout with high IBS and below EMA
		var shortCondition = candle.LowPrice < _prevLowest && ibs > IbsThreshold && candle.ClosePrice < emaVal;

		if (shortCondition && Position >= 0)
			SellMarket();

		// Cover: close below previous low
		if (Position < 0 && candle.ClosePrice < _prevLow)
			BuyMarket();

		_prevLowest = lowestVal;
		_prevLow = candle.LowPrice;
	}
}
