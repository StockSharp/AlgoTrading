using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the MQL5 Expert_ADC_PL_CCI advisor.
/// The algorithm trades Dark Cloud Cover and Piercing Line candlestick patterns confirmed by CCI.
/// </summary>
public class DarkCloudPiercingCciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _averageBodyPeriod;
	private readonly StrategyParam<decimal> _entryConfirmationLevel;
	private readonly StrategyParam<decimal> _exitLevel;

	private CommodityChannelIndex _cciIndicator = null!;
	private SimpleMovingAverage _bodyAverage = null!;
	private SimpleMovingAverage _closeAverage = null!;

	private CandleInfo? _lastCandle;
	private CandleInfo? _previousCandle;
	private decimal? _lastCci;
	private decimal? _previousCci;

	/// <summary>
	/// Initializes a new instance of <see cref="DarkCloudPiercingCciStrategy"/>.
	/// </summary>
	public DarkCloudPiercingCciStrategy()
	{
		Volume = 1;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for pattern recognition", "General");

		_cciPeriod = Param(nameof(CciPeriod), 49)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Commodity Channel Index lookback period", "Indicators");

		_averageBodyPeriod = Param(nameof(AverageBodyPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("Body Average Period", "Number of candles used to determine long bodies", "Indicators");

		_entryConfirmationLevel = Param(nameof(EntryConfirmationLevel), 50m)
			.SetNotNegative()
			.SetDisplay("CCI Entry Level", "Absolute CCI level required to confirm entries", "Trading Rules");

		_exitLevel = Param(nameof(ExitLevel), 80m)
			.SetNotNegative()
			.SetDisplay("CCI Exit Level", "Absolute CCI level used for position exits", "Trading Rules");
	}

	/// <summary>
	/// Candle type used for pattern analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// CCI calculation period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Number of candles used to compute the average body size.
	/// </summary>
	public int AverageBodyPeriod
	{
		get => _averageBodyPeriod.Value;
		set => _averageBodyPeriod.Value = value;
	}

	/// <summary>
	/// Absolute CCI level required to validate entries.
	/// </summary>
	public decimal EntryConfirmationLevel
	{
		get => _entryConfirmationLevel.Value;
		set => _entryConfirmationLevel.Value = value;
	}

	/// <summary>
	/// Absolute CCI level used when closing positions.
	/// </summary>
	public decimal ExitLevel
	{
		get => _exitLevel.Value;
		set => _exitLevel.Value = value;
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

		_lastCandle = null;
		_previousCandle = null;
		_lastCci = null;
		_previousCci = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators used by the strategy.
		_bodyAverage = new SimpleMovingAverage { Length = AverageBodyPeriod };
		_closeAverage = new SimpleMovingAverage { Length = AverageBodyPeriod };
		_cciIndicator = new CommodityChannelIndex { Length = CciPeriod };

		// Subscribe to candle data and bind indicators.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_cciIndicator, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue cciValue)
	{
		if (candle.State != CandleStates.Finished || !cciValue.IsFinal)
			return;

		// Update moving averages based on the freshly closed candle.
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var bodyAverageValue = _bodyAverage.Process(body, candle.OpenTime, true);
		var closeAverageValue = _closeAverage.Process(candle.ClosePrice, candle.OpenTime, true);

		if (_lastCandle is { } last && _previousCandle is { } previous &&
			_lastCci is decimal lastCci && _previousCci is decimal previousCci)
		{
			var canTrade = IsFormedAndOnlineAndAllowTrading();

			if (canTrade)
			{
				// Detect bullish Piercing Line pattern with bearish CCI extremes.
				if (IsPiercing(previous, last) && lastCci <= -EntryConfirmationLevel)
					OpenLong();

				// Detect bearish Dark Cloud Cover pattern with bullish CCI extremes.
				if (IsDarkCloud(previous, last) && lastCci >= EntryConfirmationLevel)
					OpenShort();
			}

			// Manage long positions when CCI leaves extreme zones.
			if (Position > 0 && ShouldExitLong(lastCci, previousCci))
				SellMarket(Math.Abs(Position));

			// Manage short positions when CCI leaves extreme zones.
			if (Position < 0 && ShouldExitShort(lastCci, previousCci))
				BuyMarket(Math.Abs(Position));
		}

		// Shift history with the latest candle and indicator values.
		_previousCandle = _lastCandle;
		_previousCci = _lastCci;

		_lastCandle = new CandleInfo(
			candle.OpenPrice,
			candle.HighPrice,
			candle.LowPrice,
			candle.ClosePrice,
			body,
			bodyAverageValue.ToDecimal(),
			_bodyAverage.IsFormed,
			closeAverageValue.ToDecimal(),
			_closeAverage.IsFormed);

		_lastCci = cciValue.ToDecimal();
	}

	private void OpenLong()
	{
		var baseVolume = Volume;

		if (baseVolume <= 0)
			return;

		var volume = baseVolume + (Position < 0 ? Math.Abs(Position) : 0m);

		BuyMarket(volume);
	}

	private void OpenShort()
	{
		var baseVolume = Volume;

		if (baseVolume <= 0)
			return;

		var volume = baseVolume + (Position > 0 ? Math.Abs(Position) : 0m);

		SellMarket(volume);
	}

	private bool ShouldExitLong(decimal lastCci, decimal previousCci)
	{
		if (ExitLevel <= 0)
			return false;

		var level = ExitLevel;

		return (lastCci < level && previousCci > level) ||
			(lastCci < -level && previousCci > -level);
	}

	private bool ShouldExitShort(decimal lastCci, decimal previousCci)
	{
		if (ExitLevel <= 0)
			return false;

		var level = ExitLevel;

		return (lastCci > -level && previousCci < -level) ||
			(lastCci > level && previousCci < level);
	}

	private bool IsPiercing(CandleInfo older, CandleInfo recent)
	{
		if (!older.AvgBodyFormed || !recent.AvgBodyFormed ||
			!older.CloseAverageFormed || !recent.CloseAverageFormed)
			return false;

		if (!older.IsBearish || !recent.IsBullish)
			return false;

		if (older.Body <= older.AvgBody || recent.Body <= recent.AvgBody)
			return false;

		var midpoint = older.Midpoint;

		if (recent.Open >= older.Low)
			return false;

		if (recent.Close <= midpoint || recent.Close >= older.Open)
			return false;

		return midpoint < older.CloseAverage;
	}

	private bool IsDarkCloud(CandleInfo older, CandleInfo recent)
	{
		if (!older.AvgBodyFormed || !recent.AvgBodyFormed ||
			!older.CloseAverageFormed || !recent.CloseAverageFormed)
			return false;

		if (!older.IsBullish || !recent.IsBearish)
			return false;

		if (older.Body <= older.AvgBody || recent.Body <= recent.AvgBody)
			return false;

		var midpoint = older.Midpoint;

		if (recent.Open <= older.High)
			return false;

		if (recent.Close >= midpoint || recent.Close <= older.Open)
			return false;

		return midpoint > older.CloseAverage;
	}

	private readonly record struct CandleInfo(
		decimal Open,
		decimal High,
		decimal Low,
		decimal Close,
		decimal Body,
		decimal AvgBody,
		bool AvgBodyFormed,
		decimal CloseAverage,
		bool CloseAverageFormed)
	{
		public bool IsBullish => Close > Open;
		public bool IsBearish => Close < Open;
		public decimal Midpoint => (Open + Close) / 2m;
	}
}

