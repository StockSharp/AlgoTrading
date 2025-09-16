using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Historical Volatility Ratio (HVR).
/// The HVR compares short-term volatility against long-term volatility.
/// </summary>
public class HvrStrategy : Strategy
{
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<decimal> _ratioThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly StandardDeviation _shortSd = new();
	private readonly StandardDeviation _longSd = new();
	private decimal? _prevClose;

	/// <summary>
	/// Period for short-term volatility.
	/// </summary>
	public int ShortPeriod
	{
		get => _shortPeriod.Value;
		set => _shortPeriod.Value = value;
	}

	/// <summary>
	/// Period for long-term volatility.
	/// </summary>
	public int LongPeriod
	{
		get => _longPeriod.Value;
		set => _longPeriod.Value = value;
	}

	/// <summary>
	/// Ratio threshold used for trade direction.
	/// Values above the threshold trigger long trades,
	/// values below trigger short trades.
	/// </summary>
	public decimal RatioThreshold
	{
		get => _ratioThreshold.Value;
		set => _ratioThreshold.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public HvrStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Short HV Period", "Bars for short-term volatility", "Parameters");

		_longPeriod = Param(nameof(LongPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Long HV Period", "Bars for long-term volatility", "Parameters");

		_ratioThreshold = Param(nameof(RatioThreshold), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Ratio Threshold", "HVR level for trade direction", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculation", "General");
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
		_prevClose = null;
		_shortSd.Reset();
		_longSd.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_shortSd.Length = ShortPeriod;
		_longSd.Length = LongPeriod;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevClose is not decimal prevClose)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var logReturn = (decimal)Math.Log((double)(candle.ClosePrice / prevClose));

		var shortVal = _shortSd.Process(logReturn);
		var longVal = _longSd.Process(logReturn);

		_prevClose = candle.ClosePrice;

		if (!shortVal.IsFinal || !longVal.IsFinal)
			return;

		if (longVal.GetValue<decimal>() == 0)
			return;

		var ratio = shortVal.GetValue<decimal>() / longVal.GetValue<decimal>();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (ratio > RatioThreshold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (ratio < RatioThreshold && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
