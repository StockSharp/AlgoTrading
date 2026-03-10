using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MacdLiquidityTrackerStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _minMacdPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private decimal _prevMacd;
	private bool _initialized;
	private int _barsFromSignal;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal MinMacdPercent { get => _minMacdPercent.Value; set => _minMacdPercent.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdLiquidityTrackerStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12).SetDisplay("Fast", "Fast EMA", "MACD");
		_slowLength = Param(nameof(SlowLength), 26).SetDisplay("Slow", "Slow EMA", "MACD");
		_minMacdPercent = Param(nameof(MinMacdPercent), 0.005m).SetDisplay("Min MACD %", "Minimum MACD magnitude in percent", "MACD");
		_cooldownBars = Param(nameof(CooldownBars), 6).SetDisplay("Cooldown", "Bars between signals", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_emaFast = null;
		_emaSlow = null;
		_prevMacd = 0m;
		_initialized = false;
		_barsFromSignal = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevMacd = 0;
		_initialized = false;
		_barsFromSignal = int.MaxValue;

		_emaFast = new ExponentialMovingAverage { Length = FastLength };
		_emaSlow = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_emaFast, _emaSlow, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_emaFast.IsFormed || !_emaSlow.IsFormed)
			return;

		var macd = fast - slow;
		var closePrice = candle.ClosePrice;
		if (closePrice <= 0)
			return;

		if (!_initialized) { _prevMacd = macd; _initialized = true; return; }
		_barsFromSignal++;

		// MACD zero-line crossover
		var crossUp = _prevMacd <= 0 && macd > 0;
		var crossDown = _prevMacd >= 0 && macd < 0;
		var macdPercent = Math.Abs(macd) / closePrice * 100m;
		var canSignal = _barsFromSignal >= CooldownBars && macdPercent >= MinMacdPercent;

		if (canSignal && crossUp && Position <= 0)
		{
			BuyMarket();
			_barsFromSignal = 0;
		}
		else if (canSignal && crossDown && Position >= 0)
		{
			SellMarket();
			_barsFromSignal = 0;
		}

		_prevMacd = macd;
	}
}
