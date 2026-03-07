using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic strategy filtered by deterministic implied-volatility skew regime changes.
/// </summary>
public class StochasticImpliedVolatilitySkewStrategy : Strategy
{
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<int> _stochD;
	private readonly StrategyParam<int> _ivPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stochastic = null!;
	private SimpleMovingAverage _ivSkewSma = null!;
	private decimal _currentIvSkew;
	private decimal _avgIvSkew;
	private decimal? _prevK;
	private bool _prevHighSkew;
	private bool _prevLowSkew;
	private int _cooldownRemaining;

	/// <summary>
	/// Stochastic length parameter.
	/// </summary>
	public int StochLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	/// <summary>
	/// Stochastic %K smoothing parameter.
	/// </summary>
	public int StochK
	{
		get => _stochK.Value;
		set => _stochK.Value = value;
	}

	/// <summary>
	/// Stochastic %D smoothing parameter.
	/// </summary>
	public int StochD
	{
		get => _stochD.Value;
		set => _stochD.Value = value;
	}

	/// <summary>
	/// IV skew averaging period.
	/// </summary>
	public int IvPeriod
	{
		get => _ivPeriod.Value;
		set => _ivPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Closed candles to wait before another position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy.
	/// </summary>
	public StochasticImpliedVolatilitySkewStrategy()
	{
		_stochLength = Param(nameof(StochLength), 14)
			.SetRange(5, 30)
			.SetDisplay("Stoch Length", "Period for stochastic oscillator", "Indicators");

		_stochK = Param(nameof(StochK), 3)
			.SetRange(1, 10)
			.SetDisplay("Stoch %K", "Smoothing for stochastic %K line", "Indicators");

		_stochD = Param(nameof(StochD), 3)
			.SetRange(1, 10)
			.SetDisplay("Stoch %D", "Smoothing for stochastic %D line", "Indicators");

		_ivPeriod = Param(nameof(IvPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("IV Period", "Period for IV skew averaging", "Options");

		_stopLoss = Param(nameof(StopLoss), 2m)
			.SetRange(1m, 5m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 18)
			.SetNotNegative()
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_stochastic?.Reset();
		_ivSkewSma?.Reset();

		_stochastic = null!;
		_ivSkewSma = null!;
		_currentIvSkew = 0m;
		_avgIvSkew = 0m;
		_prevK = null;
		_prevHighSkew = false;
		_prevLowSkew = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stochastic = new StochasticOscillator
		{
			K = { Length = StochLength },
			D = { Length = StochD },
		};

		_ivSkewSma = new SimpleMovingAverage
		{
			Length = IvPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(2, UnitTypes.Percent),
			new Unit(StopLoss, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		SimulateIvSkew(candle);

		var ivSkewSmaValue = _ivSkewSma.Process(new DecimalIndicatorValue(_ivSkewSma, _currentIvSkew, candle.OpenTime) { IsFinal = true });
		if (!_ivSkewSma.IsFormed || ivSkewSmaValue.IsEmpty)
			return;

		_avgIvSkew = ivSkewSmaValue.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (stochValue is not StochasticOscillatorValue stochTyped || stochTyped.K is not decimal stochK)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var highSkew = _currentIvSkew > (_avgIvSkew + 0.05m);
		var lowSkew = _currentIvSkew < (_avgIvSkew - 0.05m);
		var highSkewTransition = !_prevHighSkew && highSkew;
		var lowSkewTransition = !_prevLowSkew && lowSkew;
		var oversoldCross = _prevK is decimal previousK && previousK >= 25m && stochK < 25m;
		var overboughtCross = _prevK is decimal previousK2 && previousK2 <= 75m && stochK > 75m;

		if (_cooldownRemaining == 0 && oversoldCross && highSkewTransition && Position <= 0)
		{
			BuyMarket(Volume + (Position < 0 ? Math.Abs(Position) : 0m));
			_cooldownRemaining = CooldownBars;
		}
		else if (_cooldownRemaining == 0 && overboughtCross && lowSkewTransition && Position >= 0)
		{
			SellMarket(Volume + (Position > 0 ? Math.Abs(Position) : 0m));
			_cooldownRemaining = CooldownBars;
		}
		else if (Position > 0 && stochK >= 55m)
		{
			SellMarket(Position);
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && stochK <= 45m)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevK = stochK;
		_prevHighSkew = highSkew;
		_prevLowSkew = lowSkew;
	}

	private void SimulateIvSkew(ICandleMessage candle)
	{
		var range = Math.Max(candle.HighPrice - candle.LowPrice, 1m);
		var body = candle.ClosePrice - candle.OpenPrice;
		var rangeRatio = range / Math.Max(candle.OpenPrice, 1m);
		var bodyRatio = body / range;

		// Rising candles tend to keep skew more negative, falling candles less negative or positive.
		_currentIvSkew = (bodyRatio * 0.2m) - Math.Min(0.15m, rangeRatio * 10m);
	}
}
