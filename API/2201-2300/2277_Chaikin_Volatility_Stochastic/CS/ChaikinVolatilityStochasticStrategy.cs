using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using Chaikin Volatility Stochastic turning points.
/// Computes EMA of high-low range, then stochastic of that, then WMA smoothing.
/// Trades on turning points (peak/trough) of the smoothed value.
/// </summary>
public class ChaikinVolatilityStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _wmaLength;

	private ExponentialMovingAverage _rangeEma;
	private Highest _highest;
	private Lowest _lowest;
	private WeightedMovingAverage _wma;

	private decimal? _prev;
	private decimal? _prevPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int StochLength { get => _stochLength.Value; set => _stochLength.Value = value; }
	public int WmaLength { get => _wmaLength.Value; set => _wmaLength.Value = value; }

	public ChaikinVolatilityStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculation", "General");

		_emaLength = Param(nameof(EmaLength), 10)
			.SetDisplay("EMA Length", "Length for smoothing high-low range", "Indicator");

		_stochLength = Param(nameof(StochLength), 5)
			.SetDisplay("Stochastic Length", "Lookback for stochastic calculation", "Indicator");

		_wmaLength = Param(nameof(WmaLength), 5)
			.SetDisplay("WMA Length", "Weighted moving average period", "Indicator");
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
		_rangeEma = null;
		_highest = null;
		_lowest = null;
		_wma = null;
		_prev = null;
		_prevPrev = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prev = null;
		_prevPrev = null;

		_rangeEma = new ExponentialMovingAverage { Length = EmaLength };
		_highest = new Highest { Length = StochLength };
		_lowest = new Lowest { Length = StochLength };
		_wma = new WeightedMovingAverage { Length = WmaLength };

		Indicators.Add(_rangeEma);
		Indicators.Add(_highest);
		Indicators.Add(_lowest);
		Indicators.Add(_wma);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var t = candle.ServerTime;
		var range = candle.HighPrice - candle.LowPrice;

		// Step 1: EMA of high-low range
		var emaResult = _rangeEma.Process(new DecimalIndicatorValue(_rangeEma, range, t) { IsFinal = true });
		if (!_rangeEma.IsFormed)
			return;

		var emaVal = emaResult.GetValue<decimal>();

		// Step 2: Highest and Lowest of the EMA values
		var highResult = _highest.Process(new DecimalIndicatorValue(_highest, emaVal, t) { IsFinal = true });
		var lowResult = _lowest.Process(new DecimalIndicatorValue(_lowest, emaVal, t) { IsFinal = true });

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		var hh = highResult.GetValue<decimal>();
		var ll = lowResult.GetValue<decimal>();

		if (hh == ll)
			return;

		// Step 3: Stochastic percent
		var percent = (emaVal - ll) / (hh - ll) * 100m;

		// Step 4: WMA smoothing
		var smoothResult = _wma.Process(new DecimalIndicatorValue(_wma, percent, t) { IsFinal = true });
		if (!_wma.IsFormed)
			return;

		var current = smoothResult.GetValue<decimal>();

		if (_prev.HasValue && _prevPrev.HasValue)
		{
			var wasRising = _prev.Value > _prevPrev.Value;
			var isFalling = current < _prev.Value;
			var wasFalling = _prev.Value < _prevPrev.Value;
			var isRising = current > _prev.Value;

			// Peak detected (was rising, now falling) -> sell
			if (wasRising && isFalling && Position >= 0)
				SellMarket();
			// Trough detected (was falling, now rising) -> buy
			else if (wasFalling && isRising && Position <= 0)
				BuyMarket();
		}

		_prevPrev = _prev;
		_prev = current;
	}
}
