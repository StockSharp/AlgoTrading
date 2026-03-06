using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD zero-line crossover reversal.
/// </summary>
public class MacdVolumeBboReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _emaFast;
	private EMA _emaSlow;
	private decimal _prevMacd;
	private bool _initialized;
	private int _barsFromSignal;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdVolumeBboReversalStrategy()
	{
		_fastLength = Param(nameof(FastLength), 11).SetDisplay("Fast", "Fast EMA", "MACD");
		_slowLength = Param(nameof(SlowLength), 21).SetDisplay("Slow", "Slow EMA", "MACD");
		_cooldownBars = Param(nameof(CooldownBars), 5).SetDisplay("Cooldown", "Bars between signals", "Risk");
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

		_emaFast = new EMA { Length = FastLength };
		_emaSlow = new EMA { Length = SlowLength };

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_emaFast.IsFormed || !_emaSlow.IsFormed)
			return;

		var macd = fast - slow;

		if (!_initialized) { _prevMacd = macd; _initialized = true; return; }
		_barsFromSignal++;

		var crossUp = _prevMacd <= 0 && macd > 0;
		var crossDown = _prevMacd >= 0 && macd < 0;
		var canSignal = _barsFromSignal >= CooldownBars;

		if (canSignal && crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsFromSignal = 0;
		}
		else if (canSignal && crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_barsFromSignal = 0;
		}

		_prevMacd = macd;
	}
}
