using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with signal cooldown to keep trade frequency stable.
/// </summary>
public class JLinesRibbon4CycleEngineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;
	private int _barsSinceSignal;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Minimum finished candles between two signals.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="JLinesRibbon4CycleEngineStrategy"/>.
	/// </summary>
	public JLinesRibbon4CycleEngineStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastLength = Param(nameof(FastLength), 72)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA length", "Indicators");

		_slowLength = Param(nameof(SlowLength), 89)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA length", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 200)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Minimum bars between signals", "Risk");
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
		_fastEma = null;
		_slowEma = null;
		_prevFast = 0m;
		_prevSlow = 0m;
		_initialized = false;
		_barsSinceSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastEma = new ExponentialMovingAverage { Length = FastLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastEma, _slowEma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastEma.IsFormed || !_slowEma.IsFormed)
			return;

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			_barsSinceSignal = CooldownBars;
			return;
		}

		_barsSinceSignal++;

		if (_barsSinceSignal >= CooldownBars)
		{
			var crossUp = _prevFast <= _prevSlow && fast > slow;
			var crossDown = _prevFast >= _prevSlow && fast < slow;

			if (crossUp)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				else if (Position == 0)
					BuyMarket();

				_barsSinceSignal = 0;
			}
			else if (crossDown)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else if (Position == 0)
					SellMarket();

				_barsSinceSignal = 0;
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
