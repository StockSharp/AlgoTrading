using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Nadaraya-Watson envelope strategy.
/// The upper and lower envelopes are rational quadratic kernel regressions of log highs and log lows over the last
/// StartRegressionBar candles, with LookbackWindow as bandwidth and RelativeWeighting as the relative weight of time frames.
/// A close crossing above the lower envelope goes long, a close crossing below the upper envelope exits the long and,
/// in LongShort mode, goes short. A short exits when the close crosses above the lower envelope.
/// </summary>
public class NadarayaWatsonEnvelopeStrategy : Strategy
{
	/// <summary>
	/// Allowed trade directions.
	/// </summary>
	public enum StrategyTypes
	{
		/// <summary>
		/// Only long trades.
		/// </summary>
		LongOnly,

		/// <summary>
		/// Long and short trades.
		/// </summary>
		LongShort,
	}

	private readonly StrategyParam<int> _lookbackWindow;
	private readonly StrategyParam<decimal> _relativeWeighting;
	private readonly StrategyParam<int> _startRegressionBar;
	private readonly StrategyParam<StrategyTypes> _strategyType;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<double> _logHighs = [];
	private readonly List<double> _logLows = [];
	private decimal? _prevClose;
	private decimal? _prevUpper;
	private decimal? _prevLower;

	/// <summary>
	/// Kernel bandwidth in bars.
	/// </summary>
	public int LookbackWindow
	{
		get => _lookbackWindow.Value;
		set => _lookbackWindow.Value = value;
	}

	/// <summary>
	/// Relative weighting of time frames of the rational quadratic kernel.
	/// </summary>
	public decimal RelativeWeighting
	{
		get => _relativeWeighting.Value;
		set => _relativeWeighting.Value = value;
	}

	/// <summary>
	/// Number of bars used in the regression.
	/// </summary>
	public int StartRegressionBar
	{
		get => _startRegressionBar.Value;
		set => _startRegressionBar.Value = value;
	}

	/// <summary>
	/// Allowed trade directions.
	/// </summary>
	public StrategyTypes StrategyType
	{
		get => _strategyType.Value;
		set => _strategyType.Value = value;
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
	public NadarayaWatsonEnvelopeStrategy()
	{
		_lookbackWindow = Param(nameof(LookbackWindow), 8)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Window", "Kernel bandwidth in bars", "Kernel");

		_relativeWeighting = Param(nameof(RelativeWeighting), 8m)
			.SetGreaterThanZero()
			.SetDisplay("Relative Weighting", "Relative weighting of time frames", "Kernel");

		_startRegressionBar = Param(nameof(StartRegressionBar), 25)
			.SetGreaterThanZero()
			.SetDisplay("Start Regression Bar", "Number of bars used in the regression", "Kernel");

		_strategyType = Param(nameof(StrategyType), StrategyTypes.LongOnly)
			.SetDisplay("Strategy Type", "Long only or long and short", "General");

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

	private void ResetState()
	{
		_logHighs.Clear();
		_logLows.Clear();
		_prevClose = null;
		_prevUpper = null;
		_prevLower = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var h = (double)LookbackWindow;
		var r = (double)RelativeWeighting;
		var weights = new double[StartRegressionBar];
		for (var i = 0; i < weights.Length; i++)
			weights[i] = Math.Pow(1 + i * i / (h * h * 2 * r), -r);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(candle => ProcessCandle(candle, weights))
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	// Weighted average where index 0 is the latest bar.
	private static decimal Estimate(List<double> values, double[] weights)
	{
		double sum = 0, weightSum = 0;
		var last = values.Count - 1;

		for (var i = 0; i < weights.Length; i++)
		{
			sum += values[last - i] * weights[i];
			weightSum += weights[i];
		}

		return (decimal)Math.Exp(sum / weightSum);
	}

	private void ProcessCandle(ICandleMessage candle, double[] weights)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.HighPrice <= 0 || candle.LowPrice <= 0)
			return;

		_logHighs.Add(Math.Log((double)candle.HighPrice));
		_logLows.Add(Math.Log((double)candle.LowPrice));

		if (_logHighs.Count > StartRegressionBar)
		{
			_logHighs.RemoveAt(0);
			_logLows.RemoveAt(0);
		}

		if (_logHighs.Count < StartRegressionBar)
			return;

		var upper = Estimate(_logHighs, weights);
		var lower = Estimate(_logLows, weights);
		var close = candle.ClosePrice;

		var prevClose = _prevClose;
		var prevUpper = _prevUpper;
		var prevLower = _prevLower;
		_prevClose = close;
		_prevUpper = upper;
		_prevLower = lower;

		if (prevClose is not decimal pc || prevUpper is not decimal pu || prevLower is not decimal pl)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossAboveLower = pc <= pl && close > lower;
		var crossBelowUpper = pc >= pu && close < upper;

		if (crossAboveLower && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossBelowUpper && Position >= 0)
		{
			if (StrategyType == StrategyTypes.LongShort)
				SellMarket(Volume + Math.Abs(Position));
			else if (Position > 0)
				SellMarket(Position);
		}
	}
}
