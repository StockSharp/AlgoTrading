namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Starter V6 Mod E strategy using dual EMA crossover with Laguerre RSI filter.
/// Buy when fast EMA crosses above slow EMA and Laguerre is oversold.
/// Sell when fast EMA crosses below slow EMA and Laguerre is overbought.
/// </summary>
public class StarterV6ModEStrategy : Strategy
{
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<decimal> _laguerreGamma;
	private readonly StrategyParam<decimal> _laguerreOversold;
	private readonly StrategyParam<decimal> _laguerreOverbought;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lagL0;
	private decimal _lagL1;
	private decimal _lagL2;
	private decimal _lagL3;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevLaguerre;
	private bool _hasPrev;

	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	public decimal LaguerreGamma
	{
		get => _laguerreGamma.Value;
		set => _laguerreGamma.Value = value;
	}

	public decimal LaguerreOversold
	{
		get => _laguerreOversold.Value;
		set => _laguerreOversold.Value = value;
	}

	public decimal LaguerreOverbought
	{
		get => _laguerreOverbought.Value;
		set => _laguerreOverbought.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public StarterV6ModEStrategy()
	{
		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 12)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_laguerreGamma = Param(nameof(LaguerreGamma), 0.7m)
			.SetDisplay("Laguerre Gamma", "Smoothing factor for Laguerre RSI", "Indicators");

		_laguerreOversold = Param(nameof(LaguerreOversold), 0.5m)
			.SetDisplay("Laguerre Oversold", "Oversold level (0-1)", "Indicators");

		_laguerreOverbought = Param(nameof(LaguerreOverbought), 0.5m)
			.SetDisplay("Laguerre Overbought", "Overbought level (0-1)", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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

		_lagL0 = 0m;
		_lagL1 = 0m;
		_lagL2 = 0m;
		_lagL3 = 0m;
		_prevFast = 0m;
		_prevSlow = 0m;
		_prevLaguerre = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;
		_lagL0 = _lagL1 = _lagL2 = _lagL3 = 0m;

		var fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var laguerre = CalculateLaguerre(candle.ClosePrice);

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_prevLaguerre = laguerre;
			_hasPrev = true;
			return;
		}

		// EMA crossover signals
		var bullishCross = _prevFast <= _prevSlow && fast > slow;
		var bearishCross = _prevFast >= _prevSlow && fast < slow;

		// Long: fast EMA crosses above slow + Laguerre was oversold
		if (Position <= 0 && bullishCross && _prevLaguerre <= LaguerreOversold)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Short: fast EMA crosses below slow + Laguerre was overbought
		else if (Position >= 0 && bearishCross && _prevLaguerre >= LaguerreOverbought)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
		_prevLaguerre = laguerre;
	}

	private decimal CalculateLaguerre(decimal price)
	{
		var gamma = LaguerreGamma;

		var l0Prev = _lagL0;
		var l1Prev = _lagL1;
		var l2Prev = _lagL2;
		var l3Prev = _lagL3;

		_lagL0 = (1m - gamma) * price + gamma * l0Prev;
		_lagL1 = -gamma * _lagL0 + l0Prev + gamma * l1Prev;
		_lagL2 = -gamma * _lagL1 + l1Prev + gamma * l2Prev;
		_lagL3 = -gamma * _lagL2 + l2Prev + gamma * l3Prev;

		decimal cu = 0m;
		decimal cd = 0m;

		if (_lagL0 >= _lagL1) cu = _lagL0 - _lagL1; else cd = _lagL1 - _lagL0;
		if (_lagL1 >= _lagL2) cu += _lagL1 - _lagL2; else cd += _lagL2 - _lagL1;
		if (_lagL2 >= _lagL3) cu += _lagL2 - _lagL3; else cd += _lagL3 - _lagL2;

		var denom = cu + cd;
		return denom == 0m ? 0m : cu / denom;
	}
}
