namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Golden Ratio Cubes Strategy.
/// The range spans the highest high and lowest low of the previous Lookback candles. Its golden ratio extensions are
/// lowest + Phi * range above and highest - Phi * range below. A close above the upper extension buys and a close below the
/// lower extension sells; an opposite breakout reverses the position.
/// </summary>
public class GoldenRatioCubesStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _phi;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private decimal? _prevHighest;
	private decimal? _prevLowest;

	/// <summary>
	/// Candles the range spans.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Golden ratio of the extensions.
	/// </summary>
	public decimal Phi
	{
		get => _phi.Value;
		set => _phi.Value = value;
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
	public GoldenRatioCubesStrategy()
	{
		_lookback = Param(nameof(Lookback), 34)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Candles the range spans", "Indicators");

		_phi = Param(nameof(Phi), 1.618m)
			.SetGreaterThanZero()
			.SetDisplay("Phi", "Golden ratio of the extensions", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHighest = null;
		_prevLowest = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHighest = null;
		_prevLowest = null;

		_highest = new Highest { Length = Lookback };
		_lowest = new Lowest { Length = Lookback };

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

		// The range is measured on the candles before this one.
		var rangeHigh = _prevHighest;
		var rangeLow = _prevLowest;

		var highest = _highest.Process(candle.HighPrice, candle.OpenTime, true).ToDecimal();
		var lowest = _lowest.Process(candle.LowPrice, candle.OpenTime, true).ToDecimal();

		if (_highest.IsFormed && _lowest.IsFormed)
		{
			_prevHighest = highest;
			_prevLowest = lowest;
		}

		if (rangeHigh is not decimal high || rangeLow is not decimal low)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var range = high - low;
		var upperExtension = low + Phi * range;
		var lowerExtension = high - Phi * range;
		var close = candle.ClosePrice;

		if (close > upperExtension && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < lowerExtension && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
