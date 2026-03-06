using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MicuRobertEmaCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _signalThresholdPercent;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ZeroLagExponentialMovingAverage _fastMa;
	private ZeroLagExponentialMovingAverage _slowMa;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private int _barsFromSignal;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal SignalThresholdPercent { get => _signalThresholdPercent.Value; set => _signalThresholdPercent.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MicuRobertEmaCrossStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA length", "General");
		_slowLength = Param(nameof(SlowLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA length", "General");
		_signalThresholdPercent = Param(nameof(SignalThresholdPercent), 0.08m)
			.SetGreaterThanZero()
			.SetDisplay("Signal Threshold %", "Minimum EMA spread in percent", "General");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 8)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_fastMa = null;
		_slowMa = null;
		_prevFast = 0m;
		_prevSlow = 0m;
		_hasPrev = false;
		_barsFromSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_fastMa = new ZeroLagExponentialMovingAverage { Length = FastLength };
		_slowMa = new ZeroLagExponentialMovingAverage { Length = SlowLength };
		_prevFast = 0;
		_prevSlow = 0;
		_hasPrev = false;
		_barsFromSignal = SignalCooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;
		var close = candle.ClosePrice;
		if (close <= 0m)
			return;

		var spreadPercent = Math.Abs(fast - slow) / close * 100m;
		_barsFromSignal++;

		if (_barsFromSignal >= SignalCooldownBars && spreadPercent >= SignalThresholdPercent && crossUp && Position <= 0)
		{
			BuyMarket();
			_barsFromSignal = 0;
		}
		else if (_barsFromSignal >= SignalCooldownBars && spreadPercent >= SignalThresholdPercent && crossDown && Position >= 0)
		{
			SellMarket();
			_barsFromSignal = 0;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
