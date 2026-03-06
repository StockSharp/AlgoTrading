using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Parabolic SAR trend direction with Stochastic entry confirmation.
/// </summary>
public class ParabolicSarStochasticStrategy : Strategy
{
	private readonly StrategyParam<decimal> _accelerationFactor;
	private readonly StrategyParam<decimal> _maxAccelerationFactor;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<int> _stochD;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _sarValue;
	private decimal _lastStochK;
	private bool _isAboveSar;
	private bool _hasTrendState;
	private int _cooldown;

	/// <summary>
	/// Parabolic SAR acceleration factor.
	/// </summary>
	public decimal AccelerationFactor
	{
		get => _accelerationFactor.Value;
		set => _accelerationFactor.Value = value;
	}

	/// <summary>
	/// Parabolic SAR max acceleration factor.
	/// </summary>
	public decimal MaxAccelerationFactor
	{
		get => _maxAccelerationFactor.Value;
		set => _maxAccelerationFactor.Value = value;
	}

	/// <summary>
	/// Stochastic K period.
	/// </summary>
	public int StochK
	{
		get => _stochK.Value;
		set => _stochK.Value = value;
	}

	/// <summary>
	/// Stochastic D period.
	/// </summary>
	public int StochD
	{
		get => _stochD.Value;
		set => _stochD.Value = value;
	}

	/// <summary>
	/// Stochastic main period.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic oversold level.
	/// </summary>
	public decimal StochOversold
	{
		get => _stochOversold.Value;
		set => _stochOversold.Value = value;
	}

	/// <summary>
	/// Stochastic overbought level.
	/// </summary>
	public decimal StochOverbought
	{
		get => _stochOverbought.Value;
		set => _stochOverbought.Value = value;
	}

	/// <summary>
	/// Bars to wait between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type used for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Strategy constructor.
	/// </summary>
	public ParabolicSarStochasticStrategy()
	{
		_accelerationFactor = Param(nameof(AccelerationFactor), 0.02m)
			.SetRange(0.01m, 0.2m)
			.SetDisplay("Acceleration Factor", "Initial acceleration factor for SAR", "SAR");

		_maxAccelerationFactor = Param(nameof(MaxAccelerationFactor), 0.2m)
			.SetRange(0.05m, 0.5m)
			.SetDisplay("Max Acceleration Factor", "Maximum acceleration factor for SAR", "SAR");

		_stochK = Param(nameof(StochK), 3)
			.SetRange(1, 10)
			.SetDisplay("Stochastic %K", "Stochastic %K smoothing period", "Stochastic");

		_stochD = Param(nameof(StochD), 3)
			.SetRange(1, 10)
			.SetDisplay("Stochastic %D", "Stochastic %D smoothing period", "Stochastic");

		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetRange(5, 30)
			.SetDisplay("Stochastic Period", "Main stochastic period", "Stochastic");

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetDisplay("Oversold Level", "Stochastic oversold level", "Stochastic");

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetDisplay("Overbought Level", "Stochastic overbought level", "Stochastic");

		_cooldownBars = Param(nameof(CooldownBars), 160)
			.SetRange(5, 500)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");
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
		_sarValue = 0;
		_lastStochK = 50m;
		_isAboveSar = false;
		_hasTrendState = false;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var parabolicSar = new ParabolicSar
		{
			AccelerationStep = AccelerationFactor,
			AccelerationMax = MaxAccelerationFactor
		};

		var stochastic = new StochasticOscillator
		{
			K = { Length = StochK },
			D = { Length = StochD },
		};

		var subscription = SubscribeCandles(CandleType);

		subscription.BindEx(parabolicSar, OnSar);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawOwnTrades(area);

			var stochArea = CreateChartArea();
			if (stochArea != null)
				DrawIndicator(stochArea, stochastic);
		}
	}

	private void OnSar(ICandleMessage candle, IIndicatorValue sarValue)
	{
		if (candle is null || sarValue is null)
			return;

		if (candle.State != CandleStates.Finished || !sarValue.IsFormed)
			return;

		_sarValue = sarValue.ToDecimal();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle is null || stochValue is null)
			return;

		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_sarValue == 0 || !stochValue.IsFormed)
			return;

		if (stochValue is not IStochasticOscillatorValue stochTyped || stochTyped.K is not decimal stochK)
			return;

		var close = candle.ClosePrice;
		var priceAboveSar = close > _sarValue;

		if (!_hasTrendState)
		{
			_isAboveSar = priceAboveSar;
			_hasTrendState = true;
			_lastStochK = stochK;
			return;
		}

		var sarSignalChange = priceAboveSar != _isAboveSar;

		if (_cooldown > 0)
		{
			_cooldown--;
			_lastStochK = stochK;
			_isAboveSar = priceAboveSar;
			return;
		}

		var longEntry = Position == 0
			&& priceAboveSar
			&& _lastStochK <= StochOversold
			&& stochK > _lastStochK;

		var shortEntry = Position == 0
			&& !priceAboveSar
			&& _lastStochK >= StochOverbought
			&& stochK < _lastStochK;

		if (longEntry)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (shortEntry)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (sarSignalChange && Position > 0 && !priceAboveSar)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (sarSignalChange && Position < 0 && priceAboveSar)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_lastStochK = stochK;
		_isAboveSar = priceAboveSar;
	}
}
