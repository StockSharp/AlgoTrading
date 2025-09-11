using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on price crossing a regression line with volatility-based bounds.
/// </summary>
public class MultiRegressionStrategy : Strategy
{
	public enum RiskMeasureOptions
	{
		Atr,
		StdDev,
		BbWidth,
		KcWidth
	}

	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<RiskMeasureOptions> _riskMeasure;
	private readonly StrategyParam<decimal> _riskMultiplier;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private LinearRegression _regression;
	private AverageTrueRange _atr;
	private StandardDeviation _stdev;
	private BollingerBands _bollinger;
	private KeltnerChannels _keltner;

	private decimal _prevClose;
	private decimal _prevReg;
	private bool _isFirst;

	/// <summary>
	/// Number of bars for regression and risk calculations.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Selected volatility measure for bounds.
	/// </summary>
	public RiskMeasureOptions RiskMeasure { get => _riskMeasure.Value; set => _riskMeasure.Value = value; }

	/// <summary>
	/// Multiplier for the selected risk measure.
	/// </summary>
	public decimal RiskMultiplier { get => _riskMultiplier.Value; set => _riskMultiplier.Value = value; }

	/// <summary>
	/// Enable stop loss at the opposite bound.
	/// </summary>
	public bool UseStopLoss { get => _useStopLoss.Value; set => _useStopLoss.Value = value; }

	/// <summary>
	/// Enable take profit at the opposite bound.
	/// </summary>
	public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="MultiRegressionStrategy"/>.
	/// </summary>
	public MultiRegressionStrategy()
	{
		_length = Param(nameof(Length), 90)
			.SetDisplay("Length", "Number of bars for regression and risk calculations", "Regression")
			.SetCanOptimize(true)
			.SetOptimize(30, 200, 10);

		_riskMeasure = Param(nameof(RiskMeasure), RiskMeasureOptions.Atr)
			.SetDisplay("Risk Measure", "Volatility metric for bounds", "Risk Management");

		_riskMultiplier = Param(nameof(RiskMultiplier), 1m)
			.SetDisplay("Risk Multiplier", "Multiplier for selected risk measure", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable stop loss at lower/upper bound", "Risk Management");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Enable take profit at upper/lower bound", "Risk Management");

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle Type", "Type of candles for calculations", "Common");

		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_regression = new LinearRegression { Length = Length };
		_atr = new AverageTrueRange { Length = Length };
		_stdev = new StandardDeviation { Length = Length };
		_bollinger = new BollingerBands { Length = Length, Width = 2m };
		_keltner = new KeltnerChannels { Length = Length, Multiplier = 1.5m };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_regression, _atr, _stdev, _bollinger, _keltner, ProcessCandle)
			.Start();
	}
	private void ProcessCandle(ICandleMessage candle, decimal regValue, decimal atrValue, decimal stdevValue,
		decimal bbMiddle, decimal bbUpper, decimal bbLower, decimal kcMiddle, decimal kcUpper, decimal kcLower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		var riskPercent = RiskMeasure switch
		{
			RiskMeasureOptions.StdDev => stdevValue / price,
			RiskMeasureOptions.BbWidth => (bbUpper - bbLower) / bbMiddle,
			RiskMeasureOptions.KcWidth => (kcUpper - kcLower) / kcMiddle,
			_ => atrValue / price,
		};

		riskPercent *= RiskMultiplier;

		var upperBound = regValue * (1 + riskPercent);
		var lowerBound = regValue * (1 - riskPercent);

		if (!_isFirst)
		{
			var longCross = _prevClose <= _prevReg && price > regValue;
			var shortCross = _prevClose >= _prevReg && price < regValue;

			if (longCross && Position <= 0)
				BuyMarket();
			else if (shortCross && Position >= 0)
				SellMarket();
		}

		if (UseStopLoss)
		{
			if (Position > 0 && price <= lowerBound)
				SellMarket();
			else if (Position < 0 && price >= upperBound)
				BuyMarket();
		}

		if (UseTakeProfit)
		{
			if (Position > 0 && price >= upperBound)
				SellMarket();
			else if (Position < 0 && price <= lowerBound)
				BuyMarket();
		}

		_prevClose = price;
		_prevReg = regValue;
		_isFirst = false;
	}
}
