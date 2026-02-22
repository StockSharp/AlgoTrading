using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy.
/// </summary>
public class MovingAverageStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;

	public int ShortLength { get => _shortLength.Value; set => _shortLength.Value = value; }
	public int LongLength { get => _longLength.Value; set => _longLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MovingAverageStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 5).SetGreaterThanZero();
		_longLength = Param(nameof(LongLength), 20).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_initialized = false;

		var fastMa = new EMA { Length = ShortLength };
		var slowMa = new EMA { Length = LongLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastMa, slowMa, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		if (_prevFast < _prevSlow && fast >= slow && Position <= 0)
			BuyMarket();
		else if (_prevFast >= _prevSlow && fast < slow && Position > 0)
			SellMarket();

		_prevFast = fast;
		_prevSlow = slow;
	}
}
