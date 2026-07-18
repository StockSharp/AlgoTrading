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
/// SVM Trader Strategy using multiple indicators to approximate SVM classification.
/// </summary>
public class SvmTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _riskExposure;
	private readonly StrategyParam<int> _buyThreshold;
	private readonly StrategyParam<int> _sellThreshold;
	private readonly StrategyParam<int> _cooldownBars;

	private BearPower _bears;
	private BullPower _bulls;
	private Momentum _momentum;
	private MovingAverageConvergenceDivergenceSignal _macd;
	private StochasticOscillator _stochastic;
	private ForceIndex _force;
	private int _previousScore;
	private bool _hasPreviousScore;
	private int _barsSinceTrade;

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Maximum allowed cumulative position volume.
	/// </summary>
	public decimal RiskExposure
	{
		get => _riskExposure.Value;
		set => _riskExposure.Value = value;
	}

	/// <summary>
	/// Minimum score required for a long signal.
	/// </summary>
	public int BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	/// <summary>
	/// Maximum score allowed for a short signal.
	/// </summary>
	public int SellThreshold
	{
		get => _sellThreshold.Value;
		set => _sellThreshold.Value = value;
	}

	/// <summary>
	/// Bars to wait after a position is closed.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SvmTraderStrategy"/> class.
	/// </summary>
	public SvmTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_takeProfit = Param(nameof(TakeProfit), 1400m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 900m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_riskExposure = Param(nameof(RiskExposure), 1m)
			.SetDisplay("Risk Exposure", "Max cumulative position", "Risk");

		_buyThreshold = Param(nameof(BuyThreshold), 4)
			.SetDisplay("Buy Threshold", "Score required for a long signal", "Signal");

		_sellThreshold = Param(nameof(SellThreshold), 1)
			.SetDisplay("Sell Threshold", "Score required for a short signal", "Signal");

		_cooldownBars = Param(nameof(CooldownBars), 2)
			.SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk");
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

		_previousScore = 0;
		_hasPreviousScore = false;
		_barsSinceTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bears = new BearPower { Length = 13 };
		_bulls = new BullPower { Length = 13 };
		_momentum = new Momentum { Length = 13 };
		_macd = new MovingAverageConvergenceDivergenceSignal();
		_stochastic = new StochasticOscillator();
		_force = new ForceIndex { Length = 13 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bears, _bulls, _momentum, _macd, _stochastic, _force, ProcessIndicators)
			.Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Absolute),
			new Unit(StopLoss, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessIndicators(
		ICandleMessage candle,
		IIndicatorValue bearsValue,
		IIndicatorValue bullsValue,
		IIndicatorValue momentumValue,
		IIndicatorValue macdValue,
		IIndicatorValue stochasticValue,
		IIndicatorValue forceValue)
	{
		if (candle.State != CandleStates.Finished ||
			!bearsValue.IsFinal || !bullsValue.IsFinal || !momentumValue.IsFinal ||
			!macdValue.IsFinal || !stochasticValue.IsFinal || !forceValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_barsSinceTrade < CooldownBars)
			_barsSinceTrade++;

		var score = 0;
		var bears = bearsValue.ToDecimal();
		var bulls = bullsValue.ToDecimal();
		var momentum = momentumValue.ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var stochasticTyped = (StochasticOscillatorValue)stochasticValue;
		var force = forceValue.ToDecimal();

		if (bulls > bears)
			score++;
		if (momentum > 100m)
			score++;
		if (macdTyped.Macd > macdTyped.Signal)
			score++;
		if (stochasticTyped.K > stochasticTyped.D && stochasticTyped.K > 55m)
			score++;
		if (force > 0m)
			score++;

		if (!_hasPreviousScore)
		{
			_previousScore = score;
			_hasPreviousScore = true;
			return;
		}

		var longSignal = _previousScore < BuyThreshold && score >= BuyThreshold;
		var shortSignal = _previousScore > SellThreshold && score <= SellThreshold;
		var openVolume = Math.Abs(Position);

		if (_barsSinceTrade >= CooldownBars && openVolume + Volume <= RiskExposure)
		{
			if (longSignal && Position <= 0)
			{
				BuyMarket(Volume + (Position < 0 ? -Position : 0m));
				_barsSinceTrade = 0;
			}
			else if (shortSignal && Position >= 0)
			{
				SellMarket(Volume + (Position > 0 ? Position : 0m));
				_barsSinceTrade = 0;
			}
		}

		_previousScore = score;
	}
}
