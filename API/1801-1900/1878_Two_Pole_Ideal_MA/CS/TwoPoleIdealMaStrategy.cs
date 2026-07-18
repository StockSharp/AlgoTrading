using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Crosses a fast EMA and a slow TEMA to approximate the original 2pb Ideal MA logic.
/// </summary>
public class TwoPoleIdealMaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _minSpreadPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _fastMa = null!;
	private TripleExponentialMovingAverage _slowMa = null!;

	private decimal _prevFast;
	private decimal _prevSlow;
	private int _cooldownRemaining;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow TEMA length.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Candle timeframe used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum normalized spread between the fast and slow averages.
	/// </summary>
	public decimal MinSpreadPercent
	{
		get => _minSpreadPercent.Value;
		set => _minSpreadPercent.Value = value;
	}

	/// <summary>
	/// Number of completed candles to wait after a position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TwoPoleIdealMaStrategy"/>.
	/// </summary>
	public TwoPoleIdealMaStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10).SetDisplay("Fast Period", "Fast MA length", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 30).SetDisplay("Slow Period", "Slow MA length", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
		_minSpreadPercent = Param(nameof(MinSpreadPercent), 0.001m).SetDisplay("Minimum Spread %", "Minimum normalized spread between fast and slow averages", "Filters");
		_cooldownBars = Param(nameof(CooldownBars), 4).SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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
		_fastMa = null!;
		_slowMa = null!;
		_prevFast = 0m;
		_prevSlow = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Initialize indicators.
		_fastMa = new ExponentialMovingAverage { Length = FastPeriod };
		_slowMa = new TripleExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();

		// Enable default protective behavior.
		StartProtection(null, null);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		// Process only finished candles.
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;
		var spreadPercent = candle.ClosePrice != 0m ? Math.Abs(fast - slow) / candle.ClosePrice : 0m;
		_prevFast = fast;
		_prevSlow = slow;

		if (crossUp && Position <= 0 && spreadPercent >= MinSpreadPercent && _cooldownRemaining == 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}
		else if (crossDown && Position >= 0 && spreadPercent >= MinSpreadPercent && _cooldownRemaining == 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_cooldownRemaining = CooldownBars;
		}
	}
}

