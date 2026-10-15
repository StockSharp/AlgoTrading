using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NY opening range breakout with retest confirmation.
/// The range is the high and low of the 9:30-9:45 New York candles and must span at least MinRangePoints price steps. After a close
/// above the range high, a candle that dips back to the high and closes above it goes long when the close is above the EMA and the
/// session VWAP and its volume is above the volume SMA; the short side mirrors it at the range low. The stop lies 0.33 of the range
/// from the entry and the target RiskReward times that. At most MaxTradesPerSession trades are taken per day and trading stops for
/// the day once the day's PnL reaches MaxDailyLoss.
/// </summary>
public class NyOrbCpStrategy : Strategy
{
	// The README names EMA, VWAP and volume SMA confirmations without giving their lengths.
	private const int _trendLength = 20;
	private const int _volumeLength = 20;
	private const decimal _stopShare = 0.33m;

	private static readonly TimeSpan _rangeStart = new(9, 30, 0);
	private static readonly TimeSpan _rangeEnd = new(9, 45, 0);
	private static readonly TimeZoneInfo _newYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

	private readonly StrategyParam<decimal> _minRangePoints;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _maxTradesPerSession;
	private readonly StrategyParam<decimal> _maxDailyLoss;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeAverage;
	private DateTime? _sessionDate;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private bool _brokeUp;
	private bool _brokeDown;
	private int _tradesToday;
	private decimal _dayStartPnL;
	private decimal _vwapPriceVolume;
	private decimal _vwapVolume;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Minimum range size in price steps.
	/// </summary>
	public decimal MinRangePoints
	{
		get => _minRangePoints.Value;
		set => _minRangePoints.Value = value;
	}

	/// <summary>
	/// Target in multiples of the stop distance.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Maximum trades per session.
	/// </summary>
	public int MaxTradesPerSession
	{
		get => _maxTradesPerSession.Value;
		set => _maxTradesPerSession.Value = value;
	}

	/// <summary>
	/// Daily PnL at which trading stops for the day.
	/// </summary>
	public decimal MaxDailyLoss
	{
		get => _maxDailyLoss.Value;
		set => _maxDailyLoss.Value = value;
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
	public NyOrbCpStrategy()
	{
		_minRangePoints = Param(nameof(MinRangePoints), 60m)
			.SetNotNegative()
			.SetDisplay("Min Range Points", "Minimum range size in price steps", "Range");

		_riskReward = Param(nameof(RiskReward), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Target in multiples of the stop distance", "Risk");

		_maxTradesPerSession = Param(nameof(MaxTradesPerSession), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum trades per session", "Risk");

		_maxDailyLoss = Param(nameof(MaxDailyLoss), -1000m)
			.SetDisplay("Max Daily Loss", "Daily PnL at which trading stops for the day", "Risk");

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
		_sessionDate = null;
		_dayStartPnL = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		ResetSession();
	}

	private void ResetSession()
	{
		_rangeHigh = null;
		_rangeLow = null;
		_brokeUp = false;
		_brokeDown = false;
		_tradesToday = 0;
		_vwapPriceVolume = 0m;
		_vwapVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sessionDate = null;
		ResetSession();

		var ema = new ExponentialMovingAverage { Length = _trendLength };
		_volumeAverage = new SimpleMovingAverage { Length = _volumeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var nyTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(candle.OpenTime, DateTimeKind.Utc), _newYork);

		if (_sessionDate != nyTime.Date)
		{
			_sessionDate = nyTime.Date;
			ResetSession();
			_dayStartPnL = PnL;
		}

		var volumeValue = _volumeAverage.Process(new DecimalIndicatorValue(_volumeAverage, candle.TotalVolume, candle.OpenTime) { IsFinal = true });

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		_vwapPriceVolume += typical * candle.TotalVolume;
		_vwapVolume += candle.TotalVolume;
		var vwap = _vwapVolume > 0m ? _vwapPriceVolume / _vwapVolume : candle.ClosePrice;

		var timeOfDay = nyTime.TimeOfDay;

		if (timeOfDay >= _rangeStart && timeOfDay < _rangeEnd)
		{
			_rangeHigh = _rangeHigh is decimal h ? Math.Max(h, candle.HighPrice) : candle.HighPrice;
			_rangeLow = _rangeLow is decimal l ? Math.Min(l, candle.LowPrice) : candle.LowPrice;
			return;
		}

		// A breakout is remembered from the candle that closes beyond the range; the retest comes later.
		var brokeUp = _brokeUp;
		var brokeDown = _brokeDown;

		if (timeOfDay >= _rangeEnd && _rangeHigh is decimal rangeHigh && _rangeLow is decimal rangeLow)
		{
			if (candle.ClosePrice > rangeHigh)
				_brokeUp = true;
			if (candle.ClosePrice < rangeLow)
				_brokeDown = true;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket(Position);
			return;
		}

		if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				BuyMarket(-Position);
			return;
		}

		if (timeOfDay < _rangeEnd || _rangeHigh is not decimal high || _rangeLow is not decimal low || !volumeValue.IsFormed)
			return;

		var range = high - low;
		if (range <= 0m || range < MinRangePoints * (Security?.PriceStep ?? 1m))
			return;

		if (_tradesToday >= MaxTradesPerSession || PnL - _dayStartPnL <= MaxDailyLoss)
			return;

		var close = candle.ClosePrice;
		var volumeOk = candle.TotalVolume > volumeValue.GetValue<decimal>();
		var stopDistance = range * _stopShare;

		if (brokeUp && candle.LowPrice <= high && close > high && close > ema && close > vwap && volumeOk)
		{
			_stopPrice = close - stopDistance;
			_takePrice = close + stopDistance * RiskReward;
			_tradesToday++;
			BuyMarket(Volume);
		}
		else if (brokeDown && candle.HighPrice >= low && close < low && close < ema && close < vwap && volumeOk)
		{
			_stopPrice = close + stopDistance;
			_takePrice = close - stopDistance * RiskReward;
			_tradesToday++;
			SellMarket(Volume);
		}
	}
}
