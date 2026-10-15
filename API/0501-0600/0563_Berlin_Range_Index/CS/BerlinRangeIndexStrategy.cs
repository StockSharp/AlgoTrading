using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Berlin Range Index strategy.
/// The Choppiness Index(Length) is multiplied by the ATR factor lowest ATR(AtrLength) over LowLookback bars / current ATR, so expanding
/// volatility pushes it down. With UseNormalized the filtered value is rescaled to 0-100 so that its StdDevLength-bar mean minus and
/// plus two standard deviations map to 0 and 100. Below ChopMin the strategy enters in the direction of the candle (reversing an
/// opposite position) and above ChopMax it closes the position.
/// </summary>
public class BerlinRangeIndexStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _chopMax;
	private readonly StrategyParam<decimal> _chopMin;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _lowLookback;
	private readonly StrategyParam<bool> _useNormalized;
	private readonly StrategyParam<int> _stdDevLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _atrs = new();
	private readonly Queue<decimal> _filtered = new();

	/// <summary>
	/// Choppiness Index period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Index level above which the market is ranging and the position closes.
	/// </summary>
	public decimal ChopMax
	{
		get => _chopMax.Value;
		set => _chopMax.Value = value;
	}

	/// <summary>
	/// Index level below which the market trends and a position opens.
	/// </summary>
	public decimal ChopMin
	{
		get => _chopMin.Value;
		set => _chopMin.Value = value;
	}

	/// <summary>
	/// ATR period of the filter factor.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Bars over which the lowest ATR is taken.
	/// </summary>
	public int LowLookback
	{
		get => _lowLookback.Value;
		set => _lowLookback.Value = value;
	}

	/// <summary>
	/// Rescale the filtered index by its standard deviation.
	/// </summary>
	public bool UseNormalized
	{
		get => _useNormalized.Value;
		set => _useNormalized.Value = value;
	}

	/// <summary>
	/// Bars of the normalization mean and standard deviation.
	/// </summary>
	public int StdDevLength
	{
		get => _stdDevLength.Value;
		set => _stdDevLength.Value = value;
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
	public BerlinRangeIndexStrategy()
	{
		_length = Param(nameof(Length), 9)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Choppiness Index period", "Indicators");

		_chopMax = Param(nameof(ChopMax), 40m)
			.SetDisplay("Chop Max", "Index level above which the position closes", "Signals");

		_chopMin = Param(nameof(ChopMin), 10m)
			.SetDisplay("Chop Min", "Index level below which a position opens", "Signals");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period of the filter factor", "Indicators");

		_lowLookback = Param(nameof(LowLookback), 14)
			.SetGreaterThanZero()
			.SetDisplay("Low Lookback", "Bars over which the lowest ATR is taken", "Indicators");

		_useNormalized = Param(nameof(UseNormalized), true)
			.SetDisplay("Use Normalized", "Rescale the filtered index by its standard deviation", "Indicators");

		_stdDevLength = Param(nameof(StdDevLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Length", "Bars of the normalization mean and standard deviation", "Indicators");

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
		_atrs.Clear();
		_filtered.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_atrs.Clear();
		_filtered.Clear();

		var chop = new ChoppinessIndex { Length = Length };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(chop, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, chop);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue chopValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!chopValue.IsFormed || !atrValue.IsFormed)
			return;

		var chop = chopValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();

		_atrs.Enqueue(atr);
		while (_atrs.Count > LowLookback)
			_atrs.Dequeue();

		if (_atrs.Count < LowLookback || atr <= 0)
			return;

		var filtered = chop * _atrs.Min() / atr;

		_filtered.Enqueue(filtered);
		while (_filtered.Count > StdDevLength)
			_filtered.Dequeue();

		decimal index;
		if (UseNormalized)
		{
			if (_filtered.Count < StdDevLength)
				return;

			var mean = _filtered.Average();
			var variance = _filtered.Sum(v => (v - mean) * (v - mean)) / _filtered.Count;
			var std = (decimal)Math.Sqrt((double)variance);
			if (std <= 0)
				return;

			index = Math.Clamp(50m + 50m * (filtered - mean) / (2m * std), 0m, 100m);
		}
		else
		{
			index = filtered;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bullish = candle.ClosePrice > candle.OpenPrice;
		var bearish = candle.ClosePrice < candle.OpenPrice;

		if (index < ChopMin)
		{
			if (bullish && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (bearish && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
		else if (index > ChopMax)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);
		}
	}
}
