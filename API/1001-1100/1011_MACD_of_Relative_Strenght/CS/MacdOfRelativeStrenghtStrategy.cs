using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD-based strategy with RSI relative strength filter.
/// </summary>
public class MacdOfRelativeStrenghtStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private RelativeStrengthIndex _rsi;
	private bool _prevFastAbove;
	private bool _initialized;
	private int _barsFromSignal;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdOfRelativeStrenghtStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12).SetDisplay("Fast", "Fast EMA", "MACD");
		_slowLength = Param(nameof(SlowLength), 26).SetDisplay("Slow", "Slow EMA", "MACD");
		_rsiLength = Param(nameof(RsiLength), 14).SetDisplay("RSI", "RSI period", "Indicators");
		_cooldownBars = Param(nameof(CooldownBars), 10).SetDisplay("Cooldown", "Bars between signals", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_emaFast = null;
		_emaSlow = null;
		_rsi = null;
		_prevFastAbove = false;
		_initialized = false;
		_barsFromSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevFastAbove = false;
		_initialized = false;
		_barsFromSignal = 0;

		_emaFast = new ExponentialMovingAverage { Length = FastLength };
		_emaSlow = new ExponentialMovingAverage { Length = SlowLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_emaFast, _emaSlow, _rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (fast == 0 || slow == 0)
			return;

		var isFastAbove = fast > slow;

		if (!_initialized) { _prevFastAbove = isFastAbove; _initialized = true; return; }
		if (_barsFromSignal < 10000) _barsFromSignal++;

		var crossUp = isFastAbove && !_prevFastAbove;
		var crossDown = !isFastAbove && _prevFastAbove;
		var canSignal = _barsFromSignal >= CooldownBars;

		// RSI filter: buy when not overbought, sell when not oversold
		if (canSignal && crossUp && rsi < 68 && Position <= 0)
		{
			BuyMarket();
			_barsFromSignal = 0;
		}
		else if (canSignal && crossDown && rsi > 32 && Position >= 0)
		{
			SellMarket();
			_barsFromSignal = 0;
		}

		_prevFastAbove = isFastAbove;
	}
}
