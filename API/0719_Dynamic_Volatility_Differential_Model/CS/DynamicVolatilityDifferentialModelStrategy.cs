using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dynamic Volatility Differential Model strategy.
/// Trades the spread between implied and historical volatility.
/// </summary>
public class DynamicVolatilityDifferentialModelStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _stdevMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _volatilitySecurity;

	private StandardDeviation _logReturnStd;
	private StandardDeviation _spreadStd;

	private decimal _prevClose;
	private decimal _prevSpread;
	private decimal _volIndexClose;

	/// <summary>
	/// Periods for historical volatility.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for thresholds.
	/// </summary>
	public decimal StdevMultiplier
	{
		get => _stdevMultiplier.Value;
		set => _stdevMultiplier.Value = value;
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
	/// Volatility index security.
	/// </summary>
	public Security VolatilitySecurity
	{
		get => _volatilitySecurity.Value;
		set => _volatilitySecurity.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="DynamicVolatilityDifferentialModelStrategy"/>.
	/// </summary>
	public DynamicVolatilityDifferentialModelStrategy()
	{
		_length = Param(nameof(Length), 5)
			.SetGreaterThanZero()
			.SetDisplay("HV Period", "Periods for Historical Volatility", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_stdevMultiplier = Param(nameof(StdevMultiplier), 7.1m)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Multiplier", "Multiplier for dynamic thresholds", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_volatilitySecurity = Param(nameof(VolatilitySecurity), new Security { Id = "TVC:VIX" })
			.SetDisplay("Volatility Index", "Symbol of volatility index", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(VolatilitySecurity, CandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_logReturnStd = null;
		_spreadStd = null;
		_prevClose = default;
		_prevSpread = default;
		_volIndexClose = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (VolatilitySecurity == null)
			throw new InvalidOperationException("Volatility security is not specified.");

		_logReturnStd = new StandardDeviation { Length = Length };
		_spreadStd = new StandardDeviation { Length = Length };

		var mainSub = SubscribeCandles(CandleType);
		mainSub.Bind(ProcessMainCandle).Start();

		var volSub = SubscribeCandles(CandleType, security: VolatilitySecurity);
		volSub.Bind(ProcessVolatilityCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawIndicator(area, _spreadStd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessVolatilityCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_volIndexClose = candle.ClosePrice;
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_volIndexClose == 0)
		return;

		if (_prevClose == 0)
		{
		_prevClose = candle.ClosePrice;
		return;
		}

		var logReturn = (decimal)Math.Log((double)(candle.ClosePrice / _prevClose));
		var hvValue = _logReturnStd.Process(logReturn, candle.OpenTime, true);

		if (!_logReturnStd.IsFormed)
		{
		_prevClose = candle.ClosePrice;
		return;
		}

		var historicalVol = hvValue.ToDecimal() * (decimal)Math.Sqrt(252) * 100m;
		var spread = _volIndexClose - historicalVol;
		var spreadStdVal = _spreadStd.Process(spread, candle.OpenTime, true);

		if (!_spreadStd.IsFormed)
		{
		_prevClose = candle.ClosePrice;
		_prevSpread = spread;
		return;
		}

		var stdev = spreadStdVal.ToDecimal();
		var upperThreshold = stdev * StdevMultiplier;
		var lowerThreshold = -stdev * StdevMultiplier;

		if (spread > upperThreshold && Position <= 0)
		BuyMarket(Volume + Math.Abs(Position));
		else if (spread < lowerThreshold && Position >= 0)
		SellMarket(Volume + Math.Abs(Position));

		if (_prevSpread < 0 && spread > 0 && Position > 0)
		SellMarket(Math.Abs(Position));
		else if (_prevSpread > 0 && spread < 0 && Position < 0)
		BuyMarket(Math.Abs(Position));

		_prevClose = candle.ClosePrice;
		_prevSpread = spread;
	}
}
