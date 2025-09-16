using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _riskExposure;

	private BearsPower _bears;
	private BullsPower _bulls;
	private AverageTrueRange _atr;
	private Momentum _momentum;
	private MovingAverageConvergenceDivergenceSignal _macd;
	private StochasticOscillator _stochastic;
	private ForceIndex _force;

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Trade volume for orders.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Maximum allowed cumulative position volume.
	/// </summary>
	public decimal RiskExposure { get => _riskExposure.Value; set => _riskExposure.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SvmTraderStrategy"/> class.
	/// </summary>
	public SvmTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order volume", "General")
			.SetGreaterThanZero();

		_takeProfit = Param(nameof(TakeProfit), 100m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 150m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_riskExposure = Param(nameof(RiskExposure), 5m)
			.SetDisplay("Risk Exposure", "Max cumulative position", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators with constants similar to original strategy
		_bears = new BearsPower { Length = 13 };
		_bulls = new BullsPower { Length = 13 };
		_atr = new AverageTrueRange { Length = 13 };
		_momentum = new Momentum { Length = 13 };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};
		_stochastic = new StochasticOscillator
		{
			KPeriod = 5,
			DPeriod = 3,
			Smooth = 3
		};
		_force = new ForceIndex { Length = 13 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bears, _bulls, _atr, _momentum, _macd, _stochastic, _force, ProcessIndicators)
			.Start();

		// Setup position protection using stop loss and take profit
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
	IIndicatorValue atrValue,
	IIndicatorValue momentumValue,
	IIndicatorValue macdValue,
	IIndicatorValue stochasticValue,
	IIndicatorValue forceValue)
	{
		// Ensure all indicator values are final and candle is completed
		if (candle.State != CandleStates.Finished ||
			!bearsValue.IsFinal || !bullsValue.IsFinal || !atrValue.IsFinal ||
			!momentumValue.IsFinal || !macdValue.IsFinal ||
			!stochasticValue.IsFinal || !forceValue.IsFinal)
			return;

		var bears = bearsValue.ToDecimal();
		var bulls = bullsValue.ToDecimal();
		var momentum = momentumValue.ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdLine = macdTyped.Macd;
		var macdSignal = macdTyped.Signal;
		var stochTyped = (StochasticOscillatorValue)stochasticValue;
		var stochK = stochTyped.K;
		var stochD = stochTyped.D;
		var force = forceValue.ToDecimal();

		// Simple scoring mechanism to approximate SVM output
		var score = 0;
		if (bulls > bears)
			score++;
		if (momentum > 0)
			score++;
		if (macdLine > macdSignal)
			score++;
		if (stochK > stochD)
			score++;
		if (force > 0)
			score++;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var openVolume = Math.Abs(Position);

		if (score >= 3 && Position <= 0 && openVolume + Volume <= RiskExposure)
		{
			var qty = Volume + openVolume;
			BuyMarket(qty);
		}
		else if (score <= 2 && Position >= 0 && openVolume + Volume <= RiskExposure)
		{
			var qty = Volume + openVolume;
			SellMarket(qty);
		}
	}
}
