using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// John Ehlers' Reflex Oscillator strategy.
/// Goes long when the oscillator crosses above the upper level
/// and goes short when it crosses below the lower level.
/// Positions are closed when the oscillator crosses the zero line.
/// </summary>
public class ReflexOscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _reflexPeriod;
	private readonly StrategyParam<decimal> _superSmootherPeriod;
	private readonly StrategyParam<decimal> _postSmoothPeriod;
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _lowerLevel;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _superSmoothQueue = new();
	private decimal _prevPrice;
	private decimal _superSmooth1;
	private decimal _superSmooth2;
	private decimal _ema;
	private decimal _prevReflex;

	/// <summary>
	/// Reflex calculation period.
	/// </summary>
	public int ReflexPeriod
	{
		get => _reflexPeriod.Value;
		set => _reflexPeriod.Value = value;
	}

	/// <summary>
	/// Super smoother filter period.
	/// </summary>
	public decimal SuperSmootherPeriod
	{
		get => _superSmootherPeriod.Value;
		set => _superSmootherPeriod.Value = value;
	}

	/// <summary>
	/// EMA period used for post smoothing.
	/// </summary>
	public decimal PostSmoothPeriod
	{
		get => _postSmoothPeriod.Value;
		set => _postSmoothPeriod.Value = value;
	}

	/// <summary>
	/// Upper threshold for oscillator.
	/// </summary>
	public decimal UpperLevel
	{
		get => _upperLevel.Value;
		set => _upperLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold for oscillator.
	/// </summary>
	public decimal LowerLevel
	{
		get => _lowerLevel.Value;
		set => _lowerLevel.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ReflexOscillatorStrategy()
	{
		_reflexPeriod = Param(nameof(ReflexPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Reflex Period", "Reflex calculation period", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_superSmootherPeriod = Param(nameof(SuperSmootherPeriod), 8m)
			.SetGreaterThanZero()
			.SetDisplay("Super Smoother Period", "Super smoother filter period", "General")
			.SetCanOptimize(true)
			.SetOptimize(4m, 20m, 1m);

		_postSmoothPeriod = Param(nameof(PostSmoothPeriod), 33m)
			.SetGreaterThanZero()
			.SetDisplay("Post Smooth Period", "EMA period for post smoothing", "General")
			.SetCanOptimize(true)
			.SetOptimize(10m, 50m, 5m);

		_upperLevel = Param(nameof(UpperLevel), 1m)
			.SetDisplay("Upper Level", "Upper threshold", "Levels");

		_lowerLevel = Param(nameof(LowerLevel), -1m)
			.SetDisplay("Lower Level", "Lower threshold", "Levels");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_superSmoothQueue.Clear();
		_prevPrice = 0;
		_superSmooth1 = 0;
		_superSmooth2 = 0;
		_ema = 0;
		_prevReflex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		const decimal sqrt2Pi = 4.44288293816m;

		var alpha = sqrt2Pi / SuperSmootherPeriod;
		var beta = (decimal)Math.Exp((double)(-alpha));
		var gamma = -beta * beta;
		var delta = 2m * beta * (decimal)Math.Cos((double)alpha);

		var price = candle.ClosePrice;
		var superSmooth = (1m - delta - gamma) * (price + _prevPrice) * 0.5m
			+ delta * _superSmooth1
			+ gamma * _superSmooth2;

		_superSmooth2 = _superSmooth1;
		_superSmooth1 = superSmooth;
		_prevPrice = price;

		var reflexPeriod = ReflexPeriod;
		_superSmoothQueue.Enqueue(superSmooth);
		if (_superSmoothQueue.Count > reflexPeriod + 1)
			_superSmoothQueue.Dequeue();

		if (_superSmoothQueue.Count < reflexPeriod + 1)
			return;

		var ss = _superSmoothQueue.ToArray();
		var ssReflex = ss[0];
		var slope = (ssReflex - superSmooth) / reflexPeriod;

		decimal e = 0m;
		for (var i = 1; i <= reflexPeriod; i++)
		{
			var ss_i = ss[ss.Length - 1 - i];
			e += (superSmooth + i * slope) - ss_i;
		}

		var epsilon = e / reflexPeriod;
		var zeta = 2m / (PostSmoothPeriod + 1m);
		_ema = zeta * epsilon * epsilon + (1m - zeta) * _ema;
		var reflex = _ema == 0m ? 0m : epsilon / (decimal)Math.Sqrt((double)_ema);

		var prevReflex = _prevReflex;
		_prevReflex = reflex;

		if (prevReflex <= UpperLevel && reflex > UpperLevel && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (prevReflex >= LowerLevel && reflex < LowerLevel && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0 && prevReflex >= 0m && reflex < 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && prevReflex <= 0m && reflex > 0m)
		{
			BuyMarket(-Position);
		}
	}
}