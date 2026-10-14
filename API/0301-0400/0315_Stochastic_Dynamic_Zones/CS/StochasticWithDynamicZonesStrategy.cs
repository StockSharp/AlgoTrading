namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Stochastic Oscillator with dynamic overbought and oversold zones.
/// The zones are the average of %K over <see cref="LookbackPeriod"/> bars plus/minus
/// <see cref="StandardDeviationFactor"/> standard deviations. Buys when %K crosses above %D inside the oversold zone
/// and sells when %K crosses below %D inside the overbought zone; the opposite signal reverses the position.
/// </summary>
public class StochasticWithDynamicZonesStrategy : Strategy
{
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _standardDeviationFactor;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _kSmoothing;
	private SimpleMovingAverage _dLine;
	private SimpleMovingAverage _zoneAverage;
	private StandardDeviation _zoneStdDev;
	private decimal? _prevK;
	private decimal? _prevD;

	/// <summary>
	/// Stochastic lookback period.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period of %K.
	/// </summary>
	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	/// <summary>
	/// Period of %D.
	/// </summary>
	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period for the dynamic zones.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation factor for the dynamic zones.
	/// </summary>
	public decimal StandardDeviationFactor
	{
		get => _standardDeviationFactor.Value;
		set => _standardDeviationFactor.Value = value;
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
	/// Initialize <see cref="StochasticWithDynamicZonesStrategy"/>.
	/// </summary>
	public StochasticWithDynamicZonesStrategy()
	{
		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Stochastic lookback period", "Indicators");

		_stochKPeriod = Param(nameof(StochKPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stoch %K Period", "Smoothing period of %K", "Indicators");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stoch %D Period", "Period of %D", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for dynamic zones", "Indicators");

		_standardDeviationFactor = Param(nameof(StandardDeviationFactor), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Factor", "Standard deviation factor for dynamic zones", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_kSmoothing = null;
		_dLine = null;
		_zoneAverage = null;
		_zoneStdDev = null;
		_prevK = null;
		_prevD = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rawK = new StochasticK { Length = StochPeriod };
		_kSmoothing = new SimpleMovingAverage { Length = StochKPeriod };
		_dLine = new SimpleMovingAverage { Length = StochDPeriod };
		_zoneAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_zoneStdDev = new StandardDeviation { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rawK, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rawKValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var k = _kSmoothing.Process(rawKValue, candle.ServerTime, true).ToDecimal();
		if (!_kSmoothing.IsFormed)
			return;

		var d = _dLine.Process(k, candle.ServerTime, true).ToDecimal();
		var average = _zoneAverage.Process(k, candle.ServerTime, true).ToDecimal();
		var stdDev = _zoneStdDev.Process(k, candle.ServerTime, true).ToDecimal();

		var prevK = _prevK;
		var prevD = _prevD;
		_prevK = k;
		_prevD = d;

		if (!_dLine.IsFormed || !_zoneAverage.IsFormed || !_zoneStdDev.IsFormed)
			return;

		if (prevK is not decimal pk || prevD is not decimal pd)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var oversold = average - StandardDeviationFactor * stdDev;
		var overbought = average + StandardDeviationFactor * stdDev;

		var crossUp = pk <= pd && k > d;
		var crossDown = pk >= pd && k < d;

		if (crossUp && k < oversold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && k > overbought && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
