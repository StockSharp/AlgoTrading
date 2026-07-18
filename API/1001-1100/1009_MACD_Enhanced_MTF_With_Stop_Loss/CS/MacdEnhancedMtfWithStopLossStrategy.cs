using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD crossover strategy with ATR-based stop loss.
/// </summary>
public class MacdEnhancedMtfWithStopLossStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopAtrMult;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private AverageTrueRange _atr;
	private bool _prevFastAbove;
	private bool _initialized;
	private decimal _stopPrice;
	private int _barsFromSignal;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal StopAtrMult { get => _stopAtrMult.Value; set => _stopAtrMult.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdEnhancedMtfWithStopLossStrategy()
	{
		_fastLength = Param(nameof(FastLength), 8).SetDisplay("Fast", "Fast EMA", "MACD");
		_slowLength = Param(nameof(SlowLength), 17).SetDisplay("Slow", "Slow EMA", "MACD");
		_atrLength = Param(nameof(AtrLength), 14).SetDisplay("ATR", "ATR period", "Risk");
		_stopAtrMult = Param(nameof(StopAtrMult), 3m).SetDisplay("SL Mult", "ATR stop mult", "Risk");
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
		_atr = null;
		_prevFastAbove = false;
		_initialized = false;
		_stopPrice = 0m;
		_barsFromSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevFastAbove = false;
		_initialized = false;
		_stopPrice = 0;
		_barsFromSignal = 0;

		_emaFast = new ExponentialMovingAverage { Length = FastLength };
		_emaSlow = new ExponentialMovingAverage { Length = SlowLength };
		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaFast, _emaSlow, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
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

		// Stop loss check
		if (Position > 0 && _stopPrice > 0 && candle.ClosePrice <= _stopPrice)
		{
			SellMarket();
			_stopPrice = 0;
			_barsFromSignal = 0;
			_prevFastAbove = isFastAbove;
			return;
		}
		if (Position < 0 && _stopPrice > 0 && candle.ClosePrice >= _stopPrice)
		{
			BuyMarket();
			_stopPrice = 0;
			_barsFromSignal = 0;
			_prevFastAbove = isFastAbove;
			return;
		}

		if (canSignal && crossUp && Position <= 0)
		{
			BuyMarket();
			_stopPrice = atr > 0 ? candle.ClosePrice - atr * StopAtrMult : 0;
			_barsFromSignal = 0;
		}
		else if (canSignal && crossDown && Position >= 0)
		{
			SellMarket();
			_stopPrice = atr > 0 ? candle.ClosePrice + atr * StopAtrMult : 0;
			_barsFromSignal = 0;
		}

		_prevFastAbove = isFastAbove;
	}
}
