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
/// Conversion of the MetaTrader expert "CorrTime".
/// The strategy filters volatility with Bollinger Bands, requires a strong ADX trend
/// and reacts to changes in the correlation between price and time.
/// </summary>
public class CorrTimeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _closeTradeOnOppositeSignal;
	private readonly StrategyParam<int> _entryHour;
	private readonly StrategyParam<int> _openHours;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _bollingerSpreadMin;
	private readonly StrategyParam<decimal> _bollingerSpreadMax;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxLevel;
	private readonly StrategyParam<CorrTimeTradeModes> _tradeMode;
	private readonly StrategyParam<int> _correlationRangeTrend;
	private readonly StrategyParam<int> _correlationRangeReverse;
	private readonly StrategyParam<CorrTimeCorrelationTypes> _correlationType;
	private readonly StrategyParam<decimal> _corrLimitTrendBuy;
	private readonly StrategyParam<decimal> _corrLimitTrendSell;
	private readonly StrategyParam<decimal> _corrLimitReverseBuy;
	private readonly StrategyParam<decimal> _corrLimitReverseSell;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _maxOpenOrders;

	private BollingerBands _bollinger = null!;
	private AverageDirectionalIndex _adx = null!;
	private readonly List<decimal> _closePrices = new();
	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="CorrTimeStrategy"/> class.
	/// </summary>
	public CorrTimeStrategy()
	{
		Volume = 0.01m;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for all indicators", "General");

		_closeTradeOnOppositeSignal = Param(nameof(CloseTradeOnOppositeSignal), false)
			.SetDisplay("Close On Opposite", "Close current position when an opposite signal appears", "Trading");

		_entryHour = Param(nameof(EntryHour), 18)
			.SetRange(0, 23)
			.SetDisplay("Entry Hour", "Start of the daily trading window (exchange time)", "Trading");

		_openHours = Param(nameof(OpenHours), 20)
			.SetRange(0, 23)
			.SetDisplay("Open Hours", "Duration of the allowed trading window", "Trading");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Number of bars for the Bollinger Bands filter", "Filters");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Filters");

		_bollingerSpreadMin = Param(nameof(BollingerSpreadMin), 45m)
			.SetDisplay("Spread Min", "Lower bound for the Bollinger band width in pips", "Filters");

		_bollingerSpreadMax = Param(nameof(BollingerSpreadMax), 120m)
			.SetDisplay("Spread Max", "Upper bound for the Bollinger band width in pips", "Filters");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Number of bars for the Average Directional Index", "Filters");

		_adxLevel = Param(nameof(AdxLevel), 22m)
			.SetDisplay("ADX Level", "Minimal ADX value required to evaluate signals", "Filters");

		_tradeMode = Param(nameof(TradeMode), CorrTimeTradeModes.Reverse)
			.SetDisplay("Trade Mode", "Match the original Trend/Reverse/Both selector", "Signals");

		_correlationRangeTrend = Param(nameof(CorrelationRangeTrend), 40)
			.SetGreaterThanZero()
			.SetDisplay("Trend Range", "Lookback for correlation based trend signals", "Signals");

		_correlationRangeReverse = Param(nameof(CorrelationRangeReverse), 35)
			.SetGreaterThanZero()
			.SetDisplay("Reverse Range", "Lookback for correlation based reversal signals", "Signals");

		_correlationType = Param(nameof(CorrelationType), CorrTimeCorrelationTypes.Fechner)
			.SetDisplay("Correlation Type", "Estimator used to measure the price-time link", "Signals");

		_corrLimitTrendBuy = Param(nameof(CorrLimitTrendBuy), 0.90m)
			.SetDisplay("Trend Buy Threshold", "Positive correlation needed for a trend-following long", "Signals");

		_corrLimitTrendSell = Param(nameof(CorrLimitTrendSell), 0.90m)
			.SetDisplay("Trend Sell Threshold", "Negative correlation needed for a trend-following short", "Signals");

		_corrLimitReverseBuy = Param(nameof(CorrLimitReverseBuy), 0.40m)
			.SetDisplay("Reverse Buy Threshold", "Negative correlation limit for reversal longs", "Signals");

		_corrLimitReverseSell = Param(nameof(CorrLimitReverseSell), 0.60m)
			.SetDisplay("Reverse Sell Threshold", "Positive correlation limit for reversal shorts", "Signals");

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
			.SetDisplay("Take Profit", "Target distance expressed in pips", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 85m)
			.SetDisplay("Stop Loss", "Protective stop distance expressed in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 45m)
			.SetDisplay("Trailing Stop", "Trailing stop distance expressed in pips", "Risk");

		_maxOpenOrders = Param(nameof(MaxOpenOrders), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Open Orders", "Maximum number of simultaneous entries", "Trading");
	}

	/// <summary>
	/// Candle type used for all calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Close existing position when an opposite signal is triggered.
	/// </summary>
	public bool CloseTradeOnOppositeSignal
	{
		get => _closeTradeOnOppositeSignal.Value;
		set => _closeTradeOnOppositeSignal.Value = value;
	}

	/// <summary>
	/// Start of the trading window (0..23 exchange time).
	/// </summary>
	public int EntryHour
	{
		get => _entryHour.Value;
		set => _entryHour.Value = value;
	}

	/// <summary>
	/// Duration of the allowed trading window in hours.
	/// </summary>
	public int OpenHours
	{
		get => _openHours.Value;
		set => _openHours.Value = value;
	}

	/// <summary>
	/// Period of the Bollinger Bands volatility filter.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Deviation multiplier for the Bollinger Bands.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Lower bound of the Bollinger band width measured in pips.
	/// </summary>
	public decimal BollingerSpreadMin
	{
		get => _bollingerSpreadMin.Value;
		set => _bollingerSpreadMin.Value = value;
	}

	/// <summary>
	/// Upper bound of the Bollinger band width measured in pips.
	/// </summary>
	public decimal BollingerSpreadMax
	{
		get => _bollingerSpreadMax.Value;
		set => _bollingerSpreadMax.Value = value;
	}

	/// <summary>
	/// Average Directional Index period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold that must be exceeded to evaluate correlation signals.
	/// </summary>
	public decimal AdxLevel
	{
		get => _adxLevel.Value;
		set => _adxLevel.Value = value;
	}

	/// <summary>
	/// Selected trading mode (trend, reverse or both).
	/// </summary>
	public CorrTimeTradeModes TradeMode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	/// <summary>
	/// Lookback for the trend-following correlation signal.
	/// </summary>
	public int CorrelationRangeTrend
	{
		get => _correlationRangeTrend.Value;
		set => _correlationRangeTrend.Value = value;
	}

	/// <summary>
	/// Lookback for the reversal correlation signal.
	/// </summary>
	public int CorrelationRangeReverse
	{
		get => _correlationRangeReverse.Value;
		set => _correlationRangeReverse.Value = value;
	}

	/// <summary>
	/// Correlation estimator applied to the closing prices.
	/// </summary>
	public CorrTimeCorrelationTypes CorrelationType
	{
		get => _correlationType.Value;
		set => _correlationType.Value = value;
	}

	/// <summary>
	/// Threshold for opening trend-following long positions.
	/// </summary>
	public decimal CorrLimitTrendBuy
	{
		get => _corrLimitTrendBuy.Value;
		set => _corrLimitTrendBuy.Value = value;
	}

	/// <summary>
	/// Threshold for opening trend-following short positions.
	/// </summary>
	public decimal CorrLimitTrendSell
	{
		get => _corrLimitTrendSell.Value;
		set => _corrLimitTrendSell.Value = value;
	}

	/// <summary>
	/// Threshold for opening reversal long positions.
	/// </summary>
	public decimal CorrLimitReverseBuy
	{
		get => _corrLimitReverseBuy.Value;
		set => _corrLimitReverseBuy.Value = value;
	}

	/// <summary>
	/// Threshold for opening reversal short positions.
	/// </summary>
	public decimal CorrLimitReverseSell
	{
		get => _corrLimitReverseSell.Value;
		set => _corrLimitReverseSell.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Maximum number of aggregated entries.
	/// </summary>
	public int MaxOpenOrders
	{
		get => _maxOpenOrders.Value;
		set => _maxOpenOrders.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_closePrices.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		_bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		_adx = new AverageDirectionalIndex
		{
			Length = AdxPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bollinger, _adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null,
			stopLoss: StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null,
			isStopTrailing: TrailingStopPips > 0m
		);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closePrices.Add(candle.ClosePrice);
		var maxRange = Math.Max(CorrelationRangeTrend, CorrelationRangeReverse) + 2;
		if (_closePrices.Count > maxRange)
			_closePrices.RemoveRange(0, _closePrices.Count - maxRange);

		var bollingerTyped = (BollingerBandsValue)bollingerValue;
		if (bollingerTyped.UpBand is not decimal upperBand || bollingerTyped.LowBand is not decimal lowerBand)
			return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx)
			return;

		var spread = _pipSize > 0m
			? (upperBand - lowerBand) / _pipSize
			: upperBand - lowerBand;

		if (spread < BollingerSpreadMin || spread > BollingerSpreadMax)
			return;

		if (adx <= AdxLevel)
			return;

		var buySignal = false;
		var sellSignal = false;

		if ((TradeMode == CorrTimeTradeModes.TrendFollow || TradeMode == CorrTimeTradeModes.Both)
			&& TryGetCorrelationTriplet(CorrelationRangeTrend, out var trendCurrent, out var trendPrevious, out var trendBefore))
		{
			if (trendBefore < trendPrevious && trendPrevious < CorrLimitTrendBuy && trendCurrent > CorrLimitTrendBuy)
				buySignal = true;

			if (trendBefore > trendPrevious && trendPrevious > -CorrLimitTrendSell && trendCurrent < -CorrLimitTrendSell)
				sellSignal = true;
		}

		if ((TradeMode == CorrTimeTradeModes.Reverse || TradeMode == CorrTimeTradeModes.Both)
			&& TryGetCorrelationTriplet(CorrelationRangeReverse, out var reverseCurrent, out var reversePrevious, out var reverseBefore))
		{
			if (reverseBefore < reversePrevious && reversePrevious < -CorrLimitReverseBuy && reverseCurrent > -CorrLimitReverseBuy)
				buySignal = true;

			if (reverseBefore > reversePrevious && reversePrevious > CorrLimitReverseSell && reverseCurrent < CorrLimitReverseSell)
				sellSignal = true;
		}

		if (buySignal && sellSignal)
		{
			buySignal = false;
			sellSignal = false;
		}

		var openHour = candle.OpenTime.Hour;
		if (!IsWithinTradingWindow(openHour))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		HandleExistingPosition(buySignal, sellSignal);
		ExecuteEntries(buySignal, sellSignal);
	}

	private void HandleExistingPosition(bool buySignal, bool sellSignal)
	{
		if (!CloseTradeOnOppositeSignal)
			return;

		if (buySignal && Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}
		else if (sellSignal && Position > 0m)
		{
			SellMarket(Position);
		}
	}

	private void ExecuteEntries(bool buySignal, bool sellSignal)
	{
		var maxVolume = Volume * MaxOpenOrders;
		if (maxVolume <= 0m)
			return;

		if (buySignal && Position <= 0m)
		{
			var targetVolume = Math.Min(Volume + Math.Max(0m, -Position), maxVolume - Math.Max(0m, Position));
			if (targetVolume > 0m)
				BuyMarket(targetVolume);
		}
		else if (sellSignal && Position >= 0m)
		{
			var targetVolume = Math.Min(Volume + Math.Max(0m, Position), maxVolume - Math.Max(0m, -Position));
			if (targetVolume > 0m)
				SellMarket(targetVolume);
		}
	}

	private bool TryGetCorrelationTriplet(int range, out decimal current, out decimal previous, out decimal before)
	{
		current = 0m;
		previous = 0m;
		before = 0m;

		if (range <= 0)
			return false;

		var required = range + 2;
		if (_closePrices.Count < required)
			return false;

		var currentStart = _closePrices.Count - range;
		var previousStart = _closePrices.Count - range - 1;
		var beforeStart = _closePrices.Count - range - 2;

		current = CalculateCorrelation(currentStart, range);
		previous = CalculateCorrelation(previousStart, range);
		before = CalculateCorrelation(beforeStart, range);
		return true;
	}

	private decimal CalculateCorrelation(int startIndex, int length)
	{
		var buffer = new decimal[length];
		for (var i = 0; i < length; i++)
			buffer[i] = _closePrices[startIndex + i];

		return CorrelationType switch
		{
			CorrTimeCorrelationTypes.Pearson => CalculatePearson(buffer),
			CorrTimeCorrelationTypes.Spearman => CalculateSpearman(buffer),
			CorrTimeCorrelationTypes.Kendall => CalculateKendall(buffer),
			CorrTimeCorrelationTypes.Fechner => CalculateFechner(buffer),
			_ => 0m,
		};
	}

	private static decimal CalculatePearson(IReadOnlyList<decimal> values)
	{
		var n = values.Count;
		if (n == 0)
			return 0m;

		decimal sumX = 0m;
		decimal sumY = 0m;
		decimal sumXX = 0m;
		decimal sumYY = 0m;
		decimal sumXY = 0m;

		for (var i = 0; i < n; i++)
		{
			var x = n - i;
			var y = values[i];

			sumX += x;
			sumY += y;
			sumXX += x * x;
			sumYY += y * y;
			sumXY += x * y;
		}

		var numerator = n * sumXY - sumX * sumY;
		var denominatorPart1 = n * sumXX - sumX * sumX;
		var denominatorPart2 = n * sumYY - sumY * sumY;
		var denominator = (double)(denominatorPart1 * denominatorPart2);

		if (denominator <= double.Epsilon)
			return 0m;

		return (decimal)((double)numerator / Math.Sqrt(denominator));
	}

	private static decimal CalculateSpearman(IReadOnlyList<decimal> values)
	{
		var n = values.Count;
		if (n <= 1)
			return 0m;

		var ranks = new decimal[n];
		AssignRanks(values, ranks);

		decimal sumd2 = 0m;
		for (var i = 0; i < n; i++)
		{
			var diff = ranks[i] - (i + 1);
			sumd2 += diff * diff;
		}

		var denominator = n * (n * n - 1m);
		if (denominator == 0m)
			return 0m;

		return 1m - 6m * sumd2 / denominator;
	}

	private static void AssignRanks(IReadOnlyList<decimal> values, decimal[] ranks)
	{
		var n = values.Count;
		var indexed = new (decimal value, int index)[n];
		for (var i = 0; i < n; i++)
			indexed[i] = (values[i], i);

		Array.Sort(indexed, (a, b) => a.value.CompareTo(b.value));

		var position = 0;
		while (position < n)
		{
			var next = position + 1;
			while (next < n && indexed[next].value == indexed[position].value)
				next++;

			var averageRank = (position + next - 1) / 2m + 1m;
			for (var k = position; k < next; k++)
				ranks[indexed[k].index] = averageRank;

			position = next;
		}
	}

	private static decimal CalculateKendall(IReadOnlyList<decimal> values)
	{
		var n = values.Count;
		if (n <= 1)
			return 0m;

		var concordant = 0;
		var discordant = 0;

		for (var i = 0; i < n - 1; i++)
		{
			for (var j = i + 1; j < n; j++)
			{
				var delta = values[i] - values[j];
				if (delta > 0m)
					concordant++;
				else if (delta < 0m)
					discordant++;
			}
		}

		var totalPairs = concordant + discordant;
		if (totalPairs == 0)
			return 0m;

		return (decimal)(concordant - discordant) / totalPairs;
	}

	private static decimal CalculateFechner(IReadOnlyList<decimal> values)
	{
		var n = values.Count;
		if (n == 0)
			return 0m;

		decimal sum = 0m;
		for (var i = 0; i < n; i++)
			sum += values[i];

		var averagePrice = sum / n;
		var averageIndex = (n + 1m) / 2m;

		var matches = 0m;
		var mismatches = 0m;

		for (var i = 0; i < n; i++)
		{
			var timeRank = n - i;
			var priceRank = values[i];
			var timeMark = timeRank > averageIndex ? 1m : -1m;
			var priceMark = priceRank > averagePrice ? 1m : -1m;

			if (timeMark == priceMark)
				matches++;
			else
				mismatches++;
		}

		var denominator = matches + mismatches;
		if (denominator == 0m)
			return 0m;

		return (matches - mismatches) / denominator;
	}

	private bool IsWithinTradingWindow(int hour)
	{
		var start = EntryHour;
		var duration = OpenHours;
		var end = (start + duration) % 24;
		var wraps = start + duration > 23;

		if (duration == 0)
			return hour == start;

		if (!wraps)
			return hour >= start && hour <= end;

		return hour >= start || hour <= end;
	}

	private decimal CalculatePipSize()
	{
		var decimals = Security?.Decimals ?? 4;
		var point = 1m;
		for (var i = 0; i < decimals; i++)
			point /= 10m;

		var multiplier = decimals == 5 || decimals == 3 ? 10m : 1m;
		var pip = point * multiplier;
		return pip > 0m ? pip : 1m;
	}

	/// <summary>
	/// Trading modes supported by the CorrTime strategy.
	/// </summary>
	public enum CorrTimeTradeModes
	{
		/// <summary>
		/// Follow the direction of the correlation trend.
		/// </summary>
		TrendFollow = 1,

		/// <summary>
		/// Trade reversals when the correlation leaves extreme levels.
		/// </summary>
		Reverse = 2,

		/// <summary>
		/// Evaluate both the trend and the reversal conditions simultaneously.
		/// </summary>
		Both = 3,
	}

	/// <summary>
	/// Correlation estimators replicated from the original include file.
	/// </summary>
	public enum CorrTimeCorrelationTypes
	{
		/// <summary>
		/// Pearson correlation between price and time ranks.
		/// </summary>
		Pearson = 1,

		/// <summary>
		/// Spearman rank correlation between price and time ranks.
		/// </summary>
		Spearman = 2,

		/// <summary>
		/// Kendall tau correlation between price and time ranks.
		/// </summary>
		Kendall = 3,

		/// <summary>
		/// Fechner sign correlation between price and time ranks.
		/// </summary>
		Fechner = 4,
	}
}
