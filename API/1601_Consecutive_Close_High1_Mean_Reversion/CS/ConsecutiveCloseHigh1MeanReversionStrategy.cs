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
/// Short strategy based on consecutive closes above previous highs.
/// </summary>
public class ConsecutiveCloseHigh1MeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _threshold;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private int _bullCount;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _isReady;

	public int Threshold { get => _threshold.Value; set => _threshold.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ConsecutiveCloseHigh1MeanReversionStrategy()
	{
		_threshold = Param(nameof(Threshold), 3)
			.SetGreaterThanZero()
			.SetDisplay("Threshold", "Consecutive closes above prior high", "Parameters");

		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA length for trend filter", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
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

		if (candle.ClosePrice > _prevHigh)
			_bullCount++;
		if (candle.ClosePrice < _prevLow)
			_bullCount = 0;

		// Short: consecutive closes above prior high, below EMA
		if (_bullCount >= Threshold && candle.ClosePrice < emaValue && Position >= 0)
			SellMarket();
		else if (Position < 0 && candle.ClosePrice < _prevLow)
			BuyMarket();

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
