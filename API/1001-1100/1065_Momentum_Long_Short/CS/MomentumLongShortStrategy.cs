using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum strategy with EMA cross, RSI filter and cooldown.
/// </summary>
public class MomentumLongShortStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private int _barsFromSignal;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MomentumLongShortStrategy()
	{
		_fastLength = Param(nameof(FastLength), 20).SetGreaterThanZero();
		_slowLength = Param(nameof(SlowLength), 50).SetGreaterThanZero();
		_rsiLength = Param(nameof(RsiLength), 14).SetGreaterThanZero();
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 10).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
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
		_prevFast = 0m;
		_prevSlow = 0m;
		_hasPrev = false;
		_barsFromSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_prevFast = 0m;
		_prevSlow = 0m;
		_hasPrev = false;
		_barsFromSignal = SignalCooldownBars;

		var maFast = new ExponentialMovingAverage { Length = FastLength };
		var maSlow = new ExponentialMovingAverage { Length = SlowLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(maFast, maSlow, rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maFast, decimal maSlow, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_barsFromSignal++;

		if (!_hasPrev)
		{
			_prevFast = maFast;
			_prevSlow = maSlow;
			_hasPrev = true;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && maFast > maSlow;
		var crossDown = _prevFast >= _prevSlow && maFast < maSlow;

		if (_barsFromSignal >= SignalCooldownBars && crossUp && rsiValue <= 65m && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_barsFromSignal = 0;
		}
		else if (_barsFromSignal >= SignalCooldownBars && crossDown && rsiValue >= 35m && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_barsFromSignal = 0;
		}

		_prevFast = maFast;
		_prevSlow = maSlow;
	}
}
