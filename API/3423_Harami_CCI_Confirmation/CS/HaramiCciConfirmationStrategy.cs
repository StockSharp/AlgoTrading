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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Harami pattern strategy confirmed by a Commodity Channel Index filter.
/// Converts the MetaTrader Expert_ABH_BH_CCI expert advisor.
/// </summary>
public class HaramiCciConfirmationStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _bodyAveragePeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitBand;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<CandleSnapshot> _history = new();
	private readonly List<decimal> _cciHistory = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="HaramiCciConfirmationStrategy"/> class.
	/// </summary>
	public HaramiCciConfirmationStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Base volume used for entries", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_cciPeriod = Param(nameof(CciPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Length of the Commodity Channel Index", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_bodyAveragePeriod = Param(nameof(BodyAveragePeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Body Average", "Number of candles used for average body size", "Indicator");

		_entryThreshold = Param(nameof(EntryThreshold), 50m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Entry", "Absolute CCI value that confirms Harami entries", "Indicator");

		_exitBand = Param(nameof(ExitBand), 80m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Exit Band", "Magnitude for the CCI band crossover exit", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for the pattern scan", "General");
	}

	/// <summary>
	/// Order volume used for market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Period applied to the Commodity Channel Index.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Number of candles used to compute the average body size and trend filter.
	/// </summary>
	public int BodyAveragePeriod
	{
		get => _bodyAveragePeriod.Value;
		set => _bodyAveragePeriod.Value = value;
	}

	/// <summary>
	/// CCI magnitude required to confirm a Harami entry signal.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// Absolute CCI level that defines the exit band for crossover exits.
	/// </summary>
	public decimal ExitBand
	{
		get => _exitBand.Value;
		set => _exitBand.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_history.Clear();
		_cciHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(cci, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateHistory(candle, cciValue);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!HasEnoughHistory())
			return;

		var currentPrice = candle.ClosePrice;
		var previousCci = _cciHistory[^2];
		var twoAgoCci = _cciHistory[^3];

		var bullishHarami = IsBullishHarami();
		var bearishHarami = IsBearishHarami();

		var crossedAboveLowerBand = previousCci > -ExitBand && twoAgoCci <= -ExitBand;
		var crossedBelowUpperBand = previousCci < ExitBand && twoAgoCci >= ExitBand;

		if (bullishHarami && previousCci <= -EntryThreshold && Position <= 0)
		{
			var volume = OrderVolume + Math.Max(0m, -Position);
			BuyMarket(volume);
			LogInfo($"Bullish Harami confirmed by CCI. Close={currentPrice}, CCI[1]={previousCci}, Volume={volume}");
		}
		else if (bearishHarami && previousCci >= EntryThreshold && Position >= 0)
		{
			var volume = OrderVolume + Math.Max(0m, Position);
			SellMarket(volume);
			LogInfo($"Bearish Harami confirmed by CCI. Close={currentPrice}, CCI[1]={previousCci}, Volume={volume}");
		}

		if (Position > 0 && crossedBelowUpperBand)
		{
			SellMarket(Position);
			LogInfo($"Exit long triggered by CCI falling below +{ExitBand}. Close={currentPrice}, CCI[1]={previousCci}");
		}
		else if (Position < 0 && crossedAboveLowerBand)
		{
			BuyMarket(-Position);
			LogInfo($"Exit short triggered by CCI rising above -{ExitBand}. Close={currentPrice}, CCI[1]={previousCci}");
		}
	}

	private void UpdateHistory(ICandleMessage candle, decimal cciValue)
	{
		_history.Add(new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice));
		_cciHistory.Add(cciValue);

		var maxHistory = Math.Max(BodyAveragePeriod + 5, 12);

		if (_history.Count > maxHistory)
			_history.RemoveRange(0, _history.Count - maxHistory);

		if (_cciHistory.Count > maxHistory)
			_cciHistory.RemoveRange(0, _cciHistory.Count - maxHistory);
	}

	private bool HasEnoughHistory()
	{
		if (BodyAveragePeriod <= 0 || CciPeriod <= 0)
			return false;

		return _history.Count >= BodyAveragePeriod + 2 && _cciHistory.Count >= CciPeriod + 2;
	}

	private bool IsBullishHarami()
	{
		var avgBody = AverageBody(1);
		var previous = GetCandle(1);
		var twoAgo = GetCandle(2);
		var closeAverage = CloseAverage(2);

		var previousBodyPositive = previous.Close > previous.Open;
		var priorBodyNegative = twoAgo.Open > twoAgo.Close;
		var priorBodyLong = Math.Abs(twoAgo.Open - twoAgo.Close) > avgBody;
		var insideBody = previous.Close < twoAgo.Open && previous.Open > twoAgo.Close;
		var downTrend = MidPoint(twoAgo) < closeAverage;

		return previousBodyPositive && priorBodyNegative && priorBodyLong && insideBody && downTrend;
	}

	private bool IsBearishHarami()
	{
		var avgBody = AverageBody(1);
		var previous = GetCandle(1);
		var twoAgo = GetCandle(2);
		var closeAverage = CloseAverage(2);

		var previousBodyNegative = previous.Close < previous.Open;
		var priorBodyPositive = twoAgo.Close > twoAgo.Open;
		var priorBodyLong = Math.Abs(twoAgo.Close - twoAgo.Open) > avgBody;
		var insideBody = previous.Close > twoAgo.Open && previous.Open < twoAgo.Close;
		var upTrend = MidPoint(twoAgo) > closeAverage;

		return previousBodyNegative && priorBodyPositive && priorBodyLong && insideBody && upTrend;
	}

	private CandleSnapshot GetCandle(int shift)
	{
		return _history[^ (shift + 1)];
	}

	private decimal AverageBody(int startShift)
	{
		var period = BodyAveragePeriod;
		decimal sum = 0m;

		for (var i = 0; i < period; i++)
		{
			var candle = GetCandle(startShift + i);
			sum += Math.Abs(candle.Close - candle.Open);
		}

		return sum / period;
	}

	private decimal CloseAverage(int startShift)
	{
		var period = BodyAveragePeriod;
		decimal sum = 0m;

		for (var i = 0; i < period; i++)
		{
			var candle = GetCandle(startShift + i);
			sum += candle.Close;
		}

		return sum / period;
	}

	private static decimal MidPoint(CandleSnapshot candle)
	{
		return (candle.High + candle.Low) / 2m;
	}

	private readonly record struct CandleSnapshot(decimal Open, decimal High, decimal Low, decimal Close);
}

