using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive HMA Plus strategy.
/// The Hull moving average period moves between MinPeriod and MaxPeriod: while the market is active (short ATR above long ATR, or
/// short volume average above long volume average when UseVolume is set) it shrinks by AdaptPercent per bar, otherwise it grows by
/// the same rate. During active conditions a rising HMA slope above FlatThreshold goes long and a falling slope below -FlatThreshold
/// goes short; the opposite signal reverses the position.
/// </summary>
public class AdaptiveHmaPlusStrategy : Strategy
{
	private const int _shortLength = 14;
	private const int _longLength = 46;

	private readonly StrategyParam<int> _minPeriod;
	private readonly StrategyParam<int> _maxPeriod;
	private readonly StrategyParam<decimal> _adaptPercent;
	private readonly StrategyParam<decimal> _flatThreshold;
	private readonly StrategyParam<bool> _useVolume;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeShort;
	private SimpleMovingAverage _volumeLong;
	private readonly List<decimal> _closes = [];
	private decimal _dynamicPeriod;
	private decimal? _prevHma;

	/// <summary>
	/// Shortest HMA period.
	/// </summary>
	public int MinPeriod
	{
		get => _minPeriod.Value;
		set => _minPeriod.Value = value;
	}

	/// <summary>
	/// Longest HMA period.
	/// </summary>
	public int MaxPeriod
	{
		get => _maxPeriod.Value;
		set => _maxPeriod.Value = value;
	}

	/// <summary>
	/// Fraction by which the period changes per bar.
	/// </summary>
	public decimal AdaptPercent
	{
		get => _adaptPercent.Value;
		set => _adaptPercent.Value = value;
	}

	/// <summary>
	/// Minimum absolute HMA slope that counts as a trend.
	/// </summary>
	public decimal FlatThreshold
	{
		get => _flatThreshold.Value;
		set => _flatThreshold.Value = value;
	}

	/// <summary>
	/// Measure market activity by volume instead of ATR.
	/// </summary>
	public bool UseVolume
	{
		get => _useVolume.Value;
		set => _useVolume.Value = value;
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
	public AdaptiveHmaPlusStrategy()
	{
		_minPeriod = Param(nameof(MinPeriod), 172)
			.SetGreaterThanZero()
			.SetDisplay("Min Period", "Shortest HMA period", "HMA");

		_maxPeriod = Param(nameof(MaxPeriod), 233)
			.SetGreaterThanZero()
			.SetDisplay("Max Period", "Longest HMA period", "HMA");

		_adaptPercent = Param(nameof(AdaptPercent), 0.031m)
			.SetNotNegative()
			.SetDisplay("Adapt Percent", "Fraction by which the period changes per bar", "HMA");

		_flatThreshold = Param(nameof(FlatThreshold), 0m)
			.SetNotNegative()
			.SetDisplay("Flat Threshold", "Minimum absolute HMA slope that counts as a trend", "Signals");

		_useVolume = Param(nameof(UseVolume), false)
			.SetDisplay("Use Volume", "Measure market activity by volume instead of ATR", "Signals");

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
		_volumeShort = null;
		_volumeLong = null;
		_closes.Clear();
		_dynamicPeriod = 0;
		_prevHma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_closes.Clear();
		_dynamicPeriod = MaxPeriod;
		_prevHma = null;

		var atrShort = new AverageTrueRange { Length = _shortLength };
		var atrLong = new AverageTrueRange { Length = _longLength };
		_volumeShort = new SimpleMovingAverage { Length = _shortLength };
		_volumeLong = new SimpleMovingAverage { Length = _longLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atrShort, atrLong, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrShortValue, IIndicatorValue atrLongValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeShort = _volumeShort.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
		var volumeLong = _volumeLong.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

		var maxPeriod = Math.Max(MinPeriod, MaxPeriod);
		_closes.Add(candle.ClosePrice);
		var capacity = maxPeriod + (int)Math.Ceiling(Math.Sqrt(maxPeriod));
		if (_closes.Count > capacity)
			_closes.RemoveAt(0);

		if (!atrShortValue.IsFormed || !atrLongValue.IsFormed || !_volumeShort.IsFormed || !_volumeLong.IsFormed)
			return;

		var active = UseVolume
			? volumeShort > volumeLong
			: atrShortValue.ToDecimal() > atrLongValue.ToDecimal();

		// Active markets shorten the period to react faster, quiet ones lengthen it.
		_dynamicPeriod = active
			? Math.Max(MinPeriod, _dynamicPeriod * (1 - AdaptPercent))
			: Math.Min(maxPeriod, _dynamicPeriod * (1 + AdaptPercent));

		var hma = CalculateHma((int)Math.Round(_dynamicPeriod));
		if (hma is not decimal current)
			return;

		var prev = _prevHma;
		_prevHma = current;

		if (prev is not decimal previous || !IsFormedAndOnlineAndAllowTrading())
			return;

		var slope = current - previous;

		if (active && slope > FlatThreshold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (active && slope < -FlatThreshold && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}

	private decimal? CalculateHma(int length)
	{
		length = Math.Max(2, length);
		var half = Math.Max(1, length / 2);
		var sqrtLength = Math.Max(1, (int)Math.Round(Math.Sqrt(length)));

		if (_closes.Count < length + sqrtLength - 1)
			return null;

		decimal sum = 0, weightSum = 0;

		for (var i = 0; i < sqrtLength; i++)
		{
			// i bars back from the latest close.
			var end = _closes.Count - 1 - i;
			var diff = 2 * Wma(end, half) - Wma(end, length);
			var weight = sqrtLength - i;
			sum += diff * weight;
			weightSum += weight;
		}

		return sum / weightSum;
	}

	private decimal Wma(int end, int length)
	{
		decimal sum = 0, weightSum = 0;

		for (var i = 0; i < length; i++)
		{
			var weight = length - i;
			sum += _closes[end - i] * weight;
			weightSum += weight;
		}

		return sum / weightSum;
	}
}
