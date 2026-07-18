namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on KDJ (Stochastic-based) signals with smoothing.
/// Uses Stochastic K/D crossover with J-value extremes for entry/exit.
/// </summary>
public class AdaptiveKdjMtfStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kdjLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<decimal> _buyLevel;
	private readonly StrategyParam<decimal> _sellLevel;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevK;
	private decimal _prevD;
	private decimal _smoothK;
	private decimal _smoothD;
	private bool _hasPrev;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int KdjLength { get => _kdjLength.Value; set => _kdjLength.Value = value; }
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }
	public decimal BuyLevel { get => _buyLevel.Value; set => _buyLevel.Value = value; }
	public decimal SellLevel { get => _sellLevel.Value; set => _sellLevel.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdaptiveKdjMtfStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");

		_kdjLength = Param(nameof(KdjLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("KDJ Length", "Base length for Stochastic", "Parameters")
			.SetOptimize(3, 15, 3);

		_smoothingLength = Param(nameof(SmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "EMA smoothing length", "Parameters")
			.SetOptimize(3, 15, 2);

		_buyLevel = Param(nameof(BuyLevel), 30m)
			.SetDisplay("Buy Level", "J value threshold for buy signal", "Parameters")
			.SetOptimize(15m, 40m, 5m);

		_sellLevel = Param(nameof(SellLevel), 70m)
			.SetDisplay("Sell Level", "J value threshold for sell signal", "Parameters")
			.SetOptimize(60m, 85m, 5m);

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevK = 50m;
		_prevD = 50m;
		_smoothK = 50m;
		_smoothD = 50m;
		_hasPrev = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stoch = new StochasticOscillator();
		stoch.K.Length = KdjLength;
		stoch.D.Length = 3;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stoch, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stoch);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (stochValue.IsEmpty)
			return;

		var sv = (IStochasticOscillatorValue)stochValue;
		if (sv.K is not decimal k || sv.D is not decimal d)
			return;

		// Compute J = 3K - 2D (KDJ formula)
		var j = 3m * k - 2m * d;

		// EMA smoothing
		var alpha = 2m / (SmoothingLength + 1m);
		_smoothK = alpha * k + (1m - alpha) * _smoothK;
		_smoothD = alpha * d + (1m - alpha) * _smoothD;
		var smoothJ = alpha * j + (1m - alpha) * j;

		if (!_hasPrev)
		{
			_prevK = _smoothK;
			_prevD = _smoothD;
			_hasPrev = true;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevK = _smoothK;
			_prevD = _smoothD;
			return;
		}

		var crossUp = _prevK <= _prevD && _smoothK > _smoothD;
		var crossDown = _prevK >= _prevD && _smoothK < _smoothD;

		var buySignal = smoothJ < BuyLevel && crossUp;
		var sellSignal = smoothJ > SellLevel && crossDown;

		if (buySignal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}

		_prevK = _smoothK;
		_prevD = _smoothD;
	}
}
