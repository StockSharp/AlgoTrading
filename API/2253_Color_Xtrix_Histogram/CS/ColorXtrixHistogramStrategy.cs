using System;
using System.Collections.Generic;

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

	private TripleExponentialMovingAverage _tripleEma = null!;
	private RateOfChange _roc = null!;
	private ExponentialMovingAverage _smoother = null!;

	private decimal? _prev1;
	private decimal? _prev2;

	/// <summary>
	/// TRIX base length.
	/// </summary>
	public int TrixLength
	{
		get => _trixLength.Value;
		set => _trixLength.Value = value;
	}

	/// <summary>
	/// Additional smoothing length.
	/// </summary>
	public int SmoothLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	/// <summary>
	/// Momentum calculation period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorXtrixHistogramStrategy"/>.
	/// </summary>
	public ColorXtrixHistogramStrategy()
	{
		_trixLength = Param(nameof(TrixLength), 5)
			.SetDisplay("TRIX Length", "Length for base triple EMA", "Indicators")
			.SetCanOptimize(true);

		_smoothLength = Param(nameof(SmoothLength), 5)
			.SetDisplay("Smooth Length", "Length for additional smoothing", "Indicators")
			.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 1)
			.SetDisplay("Momentum Period", "Period for rate of change", "Indicators")
			.SetCanOptimize(true);

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
		_prev1 = null;
		_prev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tripleEma = new TripleExponentialMovingAverage { Length = TrixLength };
		_roc = new RateOfChange { Length = MomentumPeriod };
		_smoother = new ExponentialMovingAverage { Length = SmoothLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var logClose = (decimal)Math.Log((double)candle.ClosePrice);
		var emaVal = _tripleEma.Process(logClose);
		if (!emaVal.IsFinal)
			return;

		var rocVal = _roc.Process(emaVal.ToDecimal());
		if (!rocVal.IsFinal)
			return;

		var smoothVal = _smoother.Process(rocVal.ToDecimal());
		if (!smoothVal.IsFinal)
			return;

		var trix = smoothVal.ToDecimal();

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
