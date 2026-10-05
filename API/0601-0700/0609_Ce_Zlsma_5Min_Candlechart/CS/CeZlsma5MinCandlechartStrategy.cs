using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CE ZLSMA 5MIN Candlechart strategy.
/// Everything is computed on Heikin Ashi candles. The Chandelier Exit trails AtrMultiplier times ATR(AtrPeriod) below the highest close
/// and above the lowest close of the last AtrPeriod candles, and its direction turns up when the close rises above the short stop.
/// The zero lag LSMA is twice the ZlsmaLength linear regression of the close minus the regression of that regression. Long only:
/// buys when the direction turns up and the Heikin Ashi close is above both the ZLSMA and its open, and closes the long when the
/// Heikin Ashi close falls below the ZLSMA.
/// </summary>
public class CeZlsma5MinCandlechartStrategy : Strategy
{
	private readonly StrategyParam<int> _zlsmaLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = [];
	private readonly List<decimal> _lsmas = [];
	private readonly List<decimal> _recentCloses = [];
	private decimal? _haOpen;
	private decimal? _haClose;
	private decimal? _prevHaClose;
	private decimal? _atr;
	private int _atrCount;
	private decimal? _longStop;
	private decimal? _shortStop;
	private int _direction;

	/// <summary>
	/// ZLSMA regression length.
	/// </summary>
	public int ZlsmaLength
	{
		get => _zlsmaLength.Value;
		set => _zlsmaLength.Value = value;
	}

	/// <summary>
	/// ATR period and Chandelier lookback.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the Chandelier Exit.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	public CeZlsma5MinCandlechartStrategy()
	{
		_zlsmaLength = Param(nameof(ZlsmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("ZLSMA Length", "ZLSMA regression length", "ZLSMA");

		_atrPeriod = Param(nameof(AtrPeriod), 1)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period and Chandelier lookback", "Chandelier Exit");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier of the Chandelier Exit", "Chandelier Exit");

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
		ResetState();
	}

	private void ResetState()
	{
		_closes.Clear();
		_lsmas.Clear();
		_recentCloses.Clear();
		_haOpen = null;
		_haClose = null;
		_prevHaClose = null;
		_atr = null;
		_atrCount = 0;
		_longStop = null;
		_shortStop = null;
		_direction = 1;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

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

	private static decimal? LinearRegression(List<decimal> values, decimal value, int length)
	{
		values.Add(value);

		if (values.Count > length)
			values.RemoveAt(0);

		if (values.Count < length)
			return null;

		// Least squares line over the window, evaluated at its last point.
		decimal sumX = 0, sumY = 0, sumXY = 0, sumXX = 0;

		for (var i = 0; i < length; i++)
		{
			var y = values[i];
			sumX += i;
			sumY += y;
			sumXY += i * y;
			sumXX += i * i;
		}

		var denominator = length * sumXX - sumX * sumX;

		if (denominator == 0)
			return values[^1];

		var slope = (length * sumXY - sumX * sumY) / denominator;
		var intercept = (sumY - slope * sumX) / length;
		return intercept + slope * (length - 1);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = _haOpen is decimal o && _haClose is decimal c ? (o + c) / 2m : (candle.OpenPrice + candle.ClosePrice) / 2m;
		var haHigh = Math.Max(candle.HighPrice, Math.Max(haOpen, haClose));
		var haLow = Math.Min(candle.LowPrice, Math.Min(haOpen, haClose));
		var prevHaClose = _prevHaClose;
		_haOpen = haOpen;
		_haClose = haClose;
		_prevHaClose = haClose;

		// Wilder ATR of the Heikin Ashi candles.
		var trueRange = prevHaClose is decimal pc
			? Math.Max(haHigh - haLow, Math.Max(Math.Abs(haHigh - pc), Math.Abs(haLow - pc)))
			: haHigh - haLow;
		_atrCount = Math.Min(_atrCount + 1, AtrPeriod);
		_atr = _atr is decimal a ? (a * (_atrCount - 1) + trueRange) / _atrCount : trueRange;

		var lsma = LinearRegression(_closes, haClose, ZlsmaLength);
		decimal? zlsma = null;

		if (lsma is decimal l && LinearRegression(_lsmas, l, ZlsmaLength) is decimal l2)
			zlsma = l + (l - l2);

		_recentCloses.Add(haClose);

		if (_recentCloses.Count > AtrPeriod)
			_recentCloses.RemoveAt(0);

		if (_recentCloses.Count < AtrPeriod || prevHaClose is not decimal prevClose)
			return;

		var distance = AtrMultiplier * _atr.Value;
		var highestClose = decimal.MinValue;
		var lowestClose = decimal.MaxValue;

		foreach (var value in _recentCloses)
		{
			highestClose = Math.Max(highestClose, value);
			lowestClose = Math.Min(lowestClose, value);
		}

		var longStop = highestClose - distance;
		var shortStop = lowestClose + distance;
		var prevLongStop = _longStop ?? longStop;
		var prevShortStop = _shortStop ?? shortStop;

		if (prevClose > prevLongStop)
			longStop = Math.Max(longStop, prevLongStop);

		if (prevClose < prevShortStop)
			shortStop = Math.Min(shortStop, prevShortStop);

		_longStop = longStop;
		_shortStop = shortStop;

		var prevDirection = _direction;

		if (haClose > prevShortStop)
			_direction = 1;
		else if (haClose < prevLongStop)
			_direction = -1;

		if (zlsma is not decimal z)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (haClose < z)
				SellMarket(Position);

			return;
		}

		if (Position == 0 && _direction == 1 && prevDirection == -1 && haClose > z && haClose > haOpen)
			BuyMarket(Volume);
	}
}
