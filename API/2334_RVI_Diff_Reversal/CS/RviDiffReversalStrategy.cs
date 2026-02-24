using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the smoothed difference between RVI average and signal.
/// Buy when smoothed diff turns up, sell when it turns down.
/// </summary>
public class RviDiffReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _rviLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _smoother;
	private decimal? _prevDiff;
	private decimal? _prevPrevDiff;

	public int RviLength { get => _rviLength.Value; set => _rviLength.Value = value; }
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RviDiffReversalStrategy()
	{
		_rviLength = Param(nameof(RviLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("RVI Length", "Length of RVI", "General");

		_smoothingLength = Param(nameof(SmoothingLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Length of EMA smoothing", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevDiff = null;
		_prevPrevDiff = null;
		_smoother = new ExponentialMovingAverage { Length = SmoothingLength };

		var rvi = new RelativeVigorIndex();
		rvi.Average.Length = RviLength;

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(rvi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rvi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rviVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (rviVal is not IRelativeVigorIndexValue rviTyped)
			return;

		if (rviTyped.Average is not decimal avg || rviTyped.Signal is not decimal sig)
			return;

		var diff = avg - sig;
		var smoothResult = _smoother.Process(diff, candle.CloseTime, true);

		if (!_smoother.IsFormed)
			return;

		var current = smoothResult.GetValue<decimal>();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevPrevDiff = _prevDiff;
			_prevDiff = current;
			return;
		}

		if (_prevDiff.HasValue && _prevPrevDiff.HasValue)
		{
			var wasFalling = _prevPrevDiff > _prevDiff;
			var wasRising = _prevPrevDiff < _prevDiff;

			if (wasFalling && current > _prevDiff && Position <= 0)
				BuyMarket();
			else if (wasRising && current < _prevDiff && Position >= 0)
				SellMarket();
		}

		_prevPrevDiff = _prevDiff;
		_prevDiff = current;
	}
}
