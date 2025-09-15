using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the smoothed difference between RVI and its signal line.
/// Opens a long position when the indicator stops falling and starts rising.
/// Opens a short position when the indicator stops rising and starts falling.
/// </summary>
public class RviDiffReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _rviLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeVigorIndex _rvi;
	private SimpleMovingAverage _signal;
	private ExponentialMovingAverage _smoother;

	private decimal? _prevDiff;
	private decimal? _prevPrevDiff;

	/// <summary>
	/// RVI calculation length.
	/// </summary>
	public int RviLength { get => _rviLength.Value; set => _rviLength.Value = value; }

	/// <summary>
	/// Smoothing length for the difference series.
	/// </summary>
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="RviDiffReversalStrategy"/>.
	/// </summary>
	public RviDiffReversalStrategy()
	{
		_rviLength = Param(nameof(RviLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("RVI Length", "Length of RVI", "General")
			.SetCanOptimize(true);

		_smoothingLength = Param(nameof(SmoothingLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Length of EMA smoothing", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_rvi = default;
		_signal = default;
		_smoother = default;
		_prevDiff = default;
		_prevPrevDiff = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rvi = new RelativeVigorIndex { Length = RviLength };
		_signal = new SimpleMovingAverage { Length = RviLength };
		_smoother = new ExponentialMovingAverage { Length = SmoothingLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _smoother);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rviValue = _rvi.Process(candle);
		var signalValue = _signal.Process(rviValue);

		if (!signalValue.IsFinal)
			return;

		var diff = rviValue.ToDecimal() - signalValue.ToDecimal();

		var smoothValue = _smoother.Process(diff);

		if (!smoothValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var current = smoothValue.ToDecimal();

		if (_prevDiff.HasValue && _prevPrevDiff.HasValue)
		{
			var wasFalling = _prevPrevDiff > _prevDiff;
			var wasRising = _prevPrevDiff < _prevDiff;
			var nowRising = _prevDiff <= current;
			var nowFalling = _prevDiff >= current;

			if (wasFalling && nowRising && Position <= 0)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
			}
			else if (wasRising && nowFalling && Position >= 0)
			{
				var volume = Volume + (Position > 0 ? Position : 0m);
				SellMarket(volume);
			}
		}

		_prevPrevDiff = _prevDiff;
		_prevDiff = current;
	}
}
