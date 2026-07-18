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
/// Moving average crossover strategy.
/// </summary>
public class MovingAverageStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;
	private int _barIndex;
	private int _lastTradeBar = -1000000;

	public int ShortLength { get => _shortLength.Value; set => _shortLength.Value = value; }
	public int LongLength { get => _longLength.Value; set => _longLength.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MovingAverageStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 6).SetGreaterThanZero();
		_longLength = Param(nameof(LongLength), 21).SetGreaterThanZero();
		_cooldownBars = Param(nameof(CooldownBars), 50).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_initialized = false;
		_barIndex = 0;
		_lastTradeBar = -1000000;

		var fastMa = new EMA { Length = ShortLength };
		var slowMa = new EMA { Length = LongLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastMa, slowMa, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		var canTrade = _barIndex - _lastTradeBar >= CooldownBars;

		if (canTrade && _prevFast < _prevSlow && fast >= slow && Position <= 0)
		{
			BuyMarket();
			_lastTradeBar = _barIndex;
		}
		else if (canTrade && _prevFast >= _prevSlow && fast < slow && Position > 0)
		{
			SellMarket();
			_lastTradeBar = _barIndex;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = 0m;
		_prevSlow = 0m;
		_initialized = false;
		_barIndex = 0;
		_lastTradeBar = -1000000;
	}
}
