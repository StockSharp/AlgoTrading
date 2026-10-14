using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi regression strategy.
/// A close crossing above the linear regression line goes long and a close crossing below it goes short, reversing an opposite
/// position. The selected risk measure (ATR, standard deviation, Bollinger or Keltner half-width) times RiskMultiplier sets the
/// distance of the optional stop loss and take profit bounds from the entry price.
/// </summary>
public class MultiRegressionStrategy : Strategy
{
	/// <summary>
	/// Volatility measure used for the bounds.
	/// </summary>
	public enum RiskMeasures
	{
		/// <summary>
		/// Average true range.
		/// </summary>
		Atr,

		/// <summary>
		/// Standard deviation of closes.
		/// </summary>
		StdDev,

		/// <summary>
		/// Bollinger half-width (two standard deviations).
		/// </summary>
		Bollinger,

		/// <summary>
		/// Keltner half-width (two ATRs).
		/// </summary>
		Keltner,
	}

	private const decimal _bandWidth = 2m;

	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<RiskMeasures> _riskMeasure;
	private readonly StrategyParam<decimal> _riskMultiplier;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevRegression;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Regression and risk measure period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Volatility measure used for the bounds.
	/// </summary>
	public RiskMeasures RiskMeasure
	{
		get => _riskMeasure.Value;
		set => _riskMeasure.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the risk measure.
	/// </summary>
	public decimal RiskMultiplier
	{
		get => _riskMultiplier.Value;
		set => _riskMultiplier.Value = value;
	}

	/// <summary>
	/// Exit at the adverse bound.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Exit at the favorable bound.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MultiRegressionStrategy()
	{
		_length = Param(nameof(Length), 90)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Regression and risk measure period", "Regression");

		_riskMeasure = Param(nameof(RiskMeasure), RiskMeasures.Atr)
			.SetDisplay("Risk Measure", "Volatility measure used for the bounds", "Risk");

		_riskMultiplier = Param(nameof(RiskMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Multiplier", "Multiplier applied to the risk measure", "Risk");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Exit at the adverse bound", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Exit at the favorable bound", "Risk");

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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var regression = new LinearReg { Length = Length };
		var atr = new AverageTrueRange { Length = Length };
		var stdDev = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(regression, atr, stdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, regression);
			DrawOwnTrades(area);
		}
	}

	private void ResetState()
	{
		_prevClose = null;
		_prevRegression = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private decimal GetRiskDistance(decimal atr, decimal stdDev)
	{
		var measure = RiskMeasure switch
		{
			RiskMeasures.StdDev => stdDev,
			RiskMeasures.Bollinger => stdDev * _bandWidth,
			RiskMeasures.Keltner => atr * _bandWidth,
			_ => atr,
		};

		return measure * RiskMultiplier;
	}

	private void ProcessCandle(ICandleMessage candle, decimal regression, decimal atr, decimal stdDev)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		var prevRegression = _prevRegression;
		_prevClose = close;
		_prevRegression = regression;

		// The bounds are checked against the candle range before a new crossing is acted on.
		if (Position > 0)
		{
			if ((_stopPrice is decimal stop && candle.LowPrice <= stop) || (_takePrice is decimal take && candle.HighPrice >= take))
			{
				SellMarket(Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}
		}
		else if (Position < 0)
		{
			if ((_stopPrice is decimal stop && candle.HighPrice >= stop) || (_takePrice is decimal take && candle.LowPrice <= take))
			{
				BuyMarket(-Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}
		}

		if (prevClose is not decimal pc || prevRegression is not decimal pr)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = pc <= pr && close > regression;
		var crossDown = pc >= pr && close < regression;
		var distance = GetRiskDistance(atr, stdDev);

		if (crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = UseStopLoss ? close - distance : null;
			_takePrice = UseTakeProfit ? close + distance : null;
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = UseStopLoss ? close + distance : null;
			_takePrice = UseTakeProfit ? close - distance : null;
		}
	}
}
