using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe RSI strategy.
/// </summary>
public class MtfOscillatorFrameworkStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevRsi;
	private bool _hasPrev;
	private int _barIndex;
	private int _lastSignalBar = -1000000;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public MtfOscillatorFrameworkStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
		_rsiLength = Param(nameof(RsiLength), 14);
		_overbought = Param(nameof(Overbought), 68m);
		_oversold = Param(nameof(Oversold), 32m);
		_cooldownBars = Param(nameof(CooldownBars), 6).SetGreaterThanZero();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevRsi = 0m;
		_hasPrev = false;
		_barIndex = 0;
		_lastSignalBar = -1000000;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ema = new ExponentialMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		if (!_hasPrev)
		{
			_prevRsi = rsi;
			_hasPrev = true;
			return;
		}

		var canSignal = _barIndex - _lastSignalBar >= CooldownBars;
		var longSignal = _prevRsi <= Oversold && rsi > Oversold;
		var shortSignal = _prevRsi >= Overbought && rsi < Overbought;

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

		if (Position > 0 && rsi >= 60m)
			SellMarket();
		else if (Position < 0 && rsi <= 40m)
			BuyMarket();

		_prevRsi = rsi;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevRsi = 0m;
		_hasPrev = false;
		_barIndex = 0;
		_lastSignalBar = -1000000;
	}
}
