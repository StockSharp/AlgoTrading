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
/// Short strategy entering after consecutive closes above moving average.
/// </summary>
public class ConsecutiveBarsAboveMaStrategy : Strategy
{
	private readonly StrategyParam<int> _threshold;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private int _bullCount;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _isReady;

	public int Threshold { get => _threshold.Value; set => _threshold.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ConsecutiveBarsAboveMaStrategy()
	{
		_threshold = Param(nameof(Threshold), 3)
			.SetGreaterThanZero()
			.SetDisplay("Signal Threshold", "Number of bars above MA", "Parameters");

		_maLength = Param(nameof(MaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "Parameters");

		_emaPeriod = Param(nameof(EmaPeriod), 50)
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
		_bullCount = 0;
		_prevHigh = 0;
		_prevLow = 0;
		_isReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = MaLength };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isReady)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_isReady = true;
			return;
		}

		if (candle.ClosePrice > maValue)
			_bullCount++;
		else
			_bullCount = 0;

		// Short: consecutive bars above MA, below EMA trend filter (mean reversion)
		var shortCondition = _bullCount >= Threshold &&
			candle.ClosePrice < emaValue;

		if (shortCondition && Position >= 0)
			SellMarket();
		else if (Position < 0 && candle.ClosePrice < _prevLow)
			BuyMarket();

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
