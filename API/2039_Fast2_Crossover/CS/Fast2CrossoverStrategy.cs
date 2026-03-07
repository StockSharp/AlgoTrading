using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trading strategy based on Fast2 histogram moving average crossover.
/// Uses weighted candle body differences with WMA smoothing.
/// </summary>
public class Fast2CrossoverStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrevAverage;
	private decimal _prevDiff1;
	private decimal _prevDiff2;
	private bool _hasPrevDiff1;
	private bool _hasPrevDiff2;

	/// <summary>Candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Fast WMA length.</summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>Slow WMA length.</summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	public Fast2CrossoverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame());
		_fastLength = Param(nameof(FastLength), 5).SetDisplay("Fast length", "Fast length", "General");
		_slowLength = Param(nameof(SlowLength), 13).SetDisplay("Slow length", "Slow length", "General");
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
		_prevFast = default;
		_prevSlow = default;
		_hasPrevAverage = default;
		_prevDiff1 = default;
		_prevDiff2 = default;
		_hasPrevDiff1 = default;
		_hasPrevDiff2 = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new WeightedMovingAverage { Length = FastLength };
		var slow = new WeightedMovingAverage { Length = SlowLength };
		Indicators.Add(fast);
		Indicators.Add(slow);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(candle =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			var diff = candle.ClosePrice - candle.OpenPrice;
			var hist = diff;
			if (_hasPrevDiff1)
				hist += _prevDiff1 / (decimal)Math.Sqrt(2);
			if (_hasPrevDiff2)
				hist += _prevDiff2 / (decimal)Math.Sqrt(3);

			var fastValue = fast.Process(hist, candle.OpenTime, true);
			var slowValue = slow.Process(hist, candle.OpenTime, true);

			_prevDiff2 = _prevDiff1;
			_prevDiff1 = diff;
			_hasPrevDiff2 = _hasPrevDiff1;
			_hasPrevDiff1 = true;

			if (fastValue.IsEmpty || slowValue.IsEmpty || !fast.IsFormed || !slow.IsFormed)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var f = fastValue.ToDecimal();
			var s = slowValue.ToDecimal();

			if (_hasPrevAverage)
			{
				if (_prevFast > _prevSlow && f < s && Position <= 0)
					BuyMarket();

				if (_prevFast < _prevSlow && f > s && Position >= 0)
					SellMarket();
			}

			_prevFast = f;
			_prevSlow = s;
			_hasPrevAverage = true;
		}).Start();
	}
}
