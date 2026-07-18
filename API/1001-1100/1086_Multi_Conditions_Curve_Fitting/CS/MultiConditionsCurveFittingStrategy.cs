using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining EMA crossover, RSI and Stochastic oscillator.
/// </summary>
public class MultiConditionsCurveFittingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private int _barIndex;
	private int _lastSignalBar = -1000000;

	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiConditionsCurveFittingStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 10);
		_slowEmaLength = Param(nameof(SlowEmaLength), 25);
		_rsiLength = Param(nameof(RsiLength), 14);
		_rsiOverbought = Param(nameof(RsiOverbought), 68m);
		_rsiOversold = Param(nameof(RsiOversold), 32m);
		_cooldownBars = Param(nameof(CooldownBars), 5).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevFast = 0m;
		_prevSlow = 0m;
		_hasPrev = false;
		_barIndex = 0;
		_lastSignalBar = -1000000;

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEma, decimal slowEma, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		if (!_hasPrev)
		{
			_prevFast = fastEma;
			_prevSlow = slowEma;
			_hasPrev = true;
			return;
		}

		var canSignal = _barIndex - _lastSignalBar >= CooldownBars;
		var longSignal = _prevFast <= _prevSlow && fastEma > slowEma && rsi <= 60m;
		var shortSignal = _prevFast >= _prevSlow && fastEma < slowEma && rsi >= 40m;

		if (canSignal && longSignal && Position <= 0)
		{
			BuyMarket();
			_lastSignalBar = _barIndex;
		}
		else if (canSignal && shortSignal && Position >= 0)
		{
			SellMarket();
			_lastSignalBar = _barIndex;
		}

		_prevFast = fastEma;
		_prevSlow = slowEma;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = 0m;
		_prevSlow = 0m;
		_hasPrev = false;
		_barIndex = 0;
		_lastSignalBar = -1000000;
	}
}
