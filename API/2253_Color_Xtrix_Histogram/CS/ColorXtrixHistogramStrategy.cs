using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Color XTRIX Histogram strategy.
/// Opens or closes positions when smoothed TRIX turns direction.
/// </summary>
public class ColorXtrixHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _trixLength;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private TripleExponentialMovingAverage _tripleEma;
	private RateOfChange _roc;
	private ExponentialMovingAverage _smoother;

	private decimal? _prev1;
	private decimal? _prev2;

	public int TrixLength { get => _trixLength.Value; set => _trixLength.Value = value; }
	public int SmoothLength { get => _smoothLength.Value; set => _smoothLength.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorXtrixHistogramStrategy()
	{
		_trixLength = Param(nameof(TrixLength), 5)
			.SetDisplay("TRIX Length", "Length for base triple EMA", "Indicators");

		_smoothLength = Param(nameof(SmoothLength), 5)
			.SetDisplay("Smooth Length", "Length for additional smoothing", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 1)
			.SetDisplay("Momentum Period", "Period for rate of change", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prev1 = null;
		_prev2 = null;

		_tripleEma = new TripleExponentialMovingAverage { Length = TrixLength };
		_roc = new RateOfChange { Length = MomentumPeriod };
		_smoother = new ExponentialMovingAverage { Length = SmoothLength };

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
		var logClose = (decimal)Math.Log((double)candle.ClosePrice);

		var emaResult = _tripleEma.Process(logClose, t, true);
		if (!_tripleEma.IsFormed)
			return;

		var emaVal = emaResult.GetValue<decimal>();
		var rocResult = _roc.Process(emaVal, t, true);
		if (!_roc.IsFormed)
			return;

		var rocVal = rocResult.GetValue<decimal>();
		var smoothResult = _smoother.Process(rocVal, t, true);
		if (!_smoother.IsFormed)
			return;

		var trix = smoothResult.GetValue<decimal>();

		if (_prev1 is null || _prev2 is null)
		{
			_prev2 = _prev1;
			_prev1 = trix;
			return;
		}

		var wasDown = _prev1 < _prev2;
		var isUp = trix > _prev1;
		var wasUp = _prev1 > _prev2;
		var isDown = trix < _prev1;

		if (wasDown && isUp && Position <= 0)
			BuyMarket();
		else if (wasUp && isDown && Position >= 0)
			SellMarket();

		_prev2 = _prev1;
		_prev1 = trix;
	}
}
