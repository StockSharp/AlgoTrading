using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-currency vector trend strategy converted from the MetaTrader 5 "Vector" expert.
/// Trades EURUSD, GBPUSD, USDCHF, and USDJPY simultaneously using smoothed moving averages.
/// </summary>
public class VectorStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<decimal> _profitPercent;
	private readonly StrategyParam<decimal> _lossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _rangeCandleType;
	private readonly StrategyParam<int> _rangePeriod;
	private readonly StrategyParam<Security> _eurUsdSecurity;
	private readonly StrategyParam<Security> _gbpUsdSecurity;
	private readonly StrategyParam<Security> _usdChfSecurity;
	private readonly StrategyParam<Security> _usdJpySecurity;

	private readonly List<PairState> _pairs = new();
	private readonly Dictionary<Security, PairState> _pairBySecurity = new();

	private SimpleMovingAverage _rangeAverage;
	private decimal _pipTarget = 13m;
	private decimal _initialBalance;

	/// <summary>
	/// Fast smoothed moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow smoothed moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Additional warm-up shift in bars.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Floating profit target for closing all positions, percent of balance.
	/// </summary>
	public decimal ProfitPercent
	{
		get => _profitPercent.Value;
		set => _profitPercent.Value = value;
	}

	/// <summary>
	/// Floating loss limit for closing all positions, percent of balance.
	/// </summary>
	public decimal LossPercent
	{
		get => _lossPercent.Value;
		set => _lossPercent.Value = value;
	}

	/// <summary>
	/// Working timeframe for trading signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Timeframe used to estimate the dynamic pip target.
	/// </summary>
	public DataType RangeCandleType
	{
		get => _rangeCandleType.Value;
		set => _rangeCandleType.Value = value;
	}

	/// <summary>
	/// Number of higher timeframe candles used for pip averaging.
	/// </summary>
	public int RangePeriod
	{
		get => _rangePeriod.Value;
		set => _rangePeriod.Value = value;
	}

	/// <summary>
	/// EURUSD security handled by the strategy.
	/// </summary>
	public Security EurUsdSecurity
	{
		get => _eurUsdSecurity.Value;
		set => _eurUsdSecurity.Value = value;
	}

	/// <summary>
	/// GBPUSD security handled by the strategy.
	/// </summary>
	public Security GbpUsdSecurity
	{
		get => _gbpUsdSecurity.Value;
		set => _gbpUsdSecurity.Value = value;
	}

	/// <summary>
	/// USDCHF security handled by the strategy.
	/// </summary>
	public Security UsdChfSecurity
	{
		get => _usdChfSecurity.Value;
		set => _usdChfSecurity.Value = value;
	}

	/// <summary>
	/// USDJPY security handled by the strategy.
	/// </summary>
	public Security UsdJpySecurity
	{
		get => _usdJpySecurity.Value;
		set => _usdJpySecurity.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public VectorStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 3)
				.SetGreaterThanZero()
				.SetDisplay("Fast MA", "Fast smoothed moving average period", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(3, 15, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 7)
				.SetGreaterThanZero()
				.SetDisplay("Slow MA", "Slow smoothed moving average period", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 25, 1);

		_maShift = Param(nameof(MaShift), 8)
				.SetGreaterOrEqual(0)
				.SetDisplay("MA Shift", "Additional warm-up bars before signals", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(0, 20, 1);

		_profitPercent = Param(nameof(ProfitPercent), 0.5m)
				.SetGreaterOrEqual(0)
				.SetDisplay("Equity Take Profit %", "Close all trades when floating profit reaches this percent", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(0.2m, 2m, 0.2m);

		_lossPercent = Param(nameof(LossPercent), 30m)
				.SetGreaterOrEqual(0)
				.SetDisplay("Equity Stop Loss %", "Close all trades when floating loss reaches this percent", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(5m, 50m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Signal Timeframe", "Timeframe used for smoothed moving averages", "General");

		_rangeCandleType = Param(nameof(RangeCandleType), TimeSpan.FromHours(4).TimeFrame())
				.SetDisplay("Range Timeframe", "Timeframe used to calculate the pip target", "General");

		_rangePeriod = Param(nameof(RangePeriod), 50)
				.SetGreaterThanZero()
				.SetDisplay("Range Period", "Number of range candles in the average", "General")
				.SetCanOptimize(true)
				.SetOptimize(20, 80, 5);

		_eurUsdSecurity = Param<Security>(nameof(EurUsdSecurity))
				.SetDisplay("EURUSD", "EURUSD security", "Securities")
				.SetRequired();

		_gbpUsdSecurity = Param<Security>(nameof(GbpUsdSecurity))
				.SetDisplay("GBPUSD", "GBPUSD security", "Securities")
				.SetRequired();

		_usdChfSecurity = Param<Security>(nameof(UsdChfSecurity))
				.SetDisplay("USDCHF", "USDCHF security", "Securities")
				.SetRequired();

		_usdJpySecurity = Param<Security>(nameof(UsdJpySecurity))
				.SetDisplay("USDJPY", "USDJPY security", "Securities")
				.SetRequired();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (EurUsdSecurity != null)
		{
			yield return (EurUsdSecurity, CandleType);
			yield return (EurUsdSecurity, RangeCandleType);
		}

		if (GbpUsdSecurity != null)
		yield return (GbpUsdSecurity, CandleType);

		if (UsdChfSecurity != null)
		yield return (UsdChfSecurity, CandleType);

		if (UsdJpySecurity != null)
		yield return (UsdJpySecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pairs.Clear();
		_pairBySecurity.Clear();
		_rangeAverage = null;
		_pipTarget = 13m;
		_initialBalance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var eur = EurUsdSecurity ?? Security ?? throw new InvalidOperationException("EURUSD security is not specified.");
		var gbp = GbpUsdSecurity ?? throw new InvalidOperationException("GBPUSD security is not specified.");
		var chf = UsdChfSecurity ?? throw new InvalidOperationException("USDCHF security is not specified.");
		var jpy = UsdJpySecurity ?? throw new InvalidOperationException("USDJPY security is not specified.");

		_pairs.Clear();
		_pairBySecurity.Clear();

		var area = CreateChartArea();

		foreach (var pair in new[]
		{
			CreatePairState("EURUSD", eur),
			CreatePairState("GBPUSD", gbp),
			CreatePairState("USDCHF", chf),
			CreatePairState("USDJPY", jpy)
		})
		{
			_pairs.Add(pair);
			_pairBySecurity[pair.Security] = pair;

			var subscription = SubscribeCandles(CandleType, security: pair.Security);
			subscription.Bind(candle => ProcessPairCandle(pair, candle)).Start();

			if (area != null && ReferenceEquals(pair.Security, eur))
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, pair.FastMa);
				DrawIndicator(area, pair.SlowMa);
				DrawOwnTrades(area);
			}
		}

		_rangeAverage = new SimpleMovingAverage { Length = RangePeriod };

		SubscribeCandles(RangeCandleType, security: eur)
				.Bind(ProcessRangeCandle)
				.Start();

		_initialBalance = Portfolio?.CurrentValue ?? 0m;

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var security = trade?.Order?.Security ?? trade?.Trade?.Security;
		if (security == null)
		return;

		if (!_pairBySecurity.TryGetValue(security, out var pair))
		return;

		var previousVolume = pair.PositionVolume;
		var currentVolume = GetPositionValue(security, Portfolio) ?? 0m;
		pair.PositionVolume = currentVolume;

		if (currentVolume == 0m)
		{
			pair.AveragePrice = 0m;
			return;
		}

		var tradePrice = trade.Trade?.Price ?? 0m;
		var tradeVolume = trade.Trade?.Volume ?? 0m;

		if (previousVolume == 0m || Math.Sign(previousVolume) != Math.Sign(currentVolume))
		{
			// Fresh position or direct reversal resets the average price.
			pair.AveragePrice = tradePrice;
			return;
		}

		if (Math.Abs(currentVolume) > Math.Abs(previousVolume) && tradeVolume > 0m)
		{
			// Additional entry increases the net size, update the weighted average price.
			var newAbsVolume = Math.Abs(previousVolume) + tradeVolume;
			if (newAbsVolume > 0m)
			{
				pair.AveragePrice = ((pair.AveragePrice * Math.Abs(previousVolume)) + tradePrice * tradeVolume) / newAbsVolume;
			}
		}
	}

	private PairState CreatePairState(string name, Security security)
	{
		var adjustedPoint = GetAdjustedPoint(security);
		return new PairState(name, security, FastMaPeriod, SlowMaPeriod, adjustedPoint);
	}

	private void ProcessPairCandle(PairState pair, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var time = candle.OpenTime;

		var fastValue = pair.FastMa.Process(new DecimalIndicatorValue(pair.FastMa, median, time));
		var slowValue = pair.SlowMa.Process(new DecimalIndicatorValue(pair.SlowMa, median, time));

		if (!fastValue.IsFormed || !slowValue.IsFormed)
		return;

		pair.FastValue = fastValue.ToDecimal();
		pair.SlowValue = slowValue.ToDecimal();
		pair.ProcessedBars++;
		pair.IsReady = pair.FastMa.IsFormed && pair.SlowMa.IsFormed && pair.ProcessedBars > MaShift;

		TryCloseOnPips(pair, candle.ClosePrice);
		EvaluateAccountThresholds();
		EvaluateEntries();
	}

	private void ProcessRangeCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var primary = _pairs.FirstOrDefault();
		if (primary == null)
		return;

		var point = primary.AdjustedPoint;
		if (point <= 0m)
		return;

		var range = (candle.HighPrice - candle.LowPrice) / point;
		var value = _rangeAverage.Process(new DecimalIndicatorValue(_rangeAverage, range, candle.OpenTime));

		if (!value.IsFormed)
		return;

		var averageRange = value.ToDecimal();
		_pipTarget = Math.Max(13m, averageRange);
	}

	private void TryCloseOnPips(PairState pair, decimal currentPrice)
	{
		var volume = pair.PositionVolume;
		if (volume == 0m || pair.AveragePrice == 0m || pair.AdjustedPoint <= 0m)
		return;

		var profitPips = volume > 0m
		? (currentPrice - pair.AveragePrice) / pair.AdjustedPoint
		: (pair.AveragePrice - currentPrice) / pair.AdjustedPoint;

		if (profitPips >= _pipTarget)
		{
			ClosePairPosition(pair);
		}
	}

	private void EvaluateEntries()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_pairs.Any(p => !p.IsReady))
		return;

		var fastSum = _pairs.Sum(p => p.FastValue);
		var slowSum = _pairs.Sum(p => p.SlowValue);
		var bias = fastSum - slowSum;

		foreach (var pair in _pairs)
		{
			if (pair.PositionVolume != 0m)
			continue;

			if (bias > 0m && pair.FastValue > pair.SlowValue)
			{
				OpenPairPosition(pair, Sides.Buy);
			}
			else if (bias < 0m && pair.FastValue < pair.SlowValue)
			{
				OpenPairPosition(pair, Sides.Sell);
			}
		}
	}

	private void EvaluateAccountThresholds()
	{
		if (Portfolio == null)
		return;

		var equity = Portfolio.CurrentValue ?? 0m;
		var floating = equity - _initialBalance;

		var profitThreshold = _initialBalance * ProfitPercent / 100m;
		var lossThreshold = _initialBalance * LossPercent / 100m;

		if (profitThreshold > 0m && floating >= profitThreshold)
		{
			CloseAllPairs();
		}
		else if (lossThreshold > 0m && floating <= -lossThreshold)
		{
			CloseAllPairs();
		}
	}

	private void CloseAllPairs()
	{
		foreach (var pair in _pairs)
		{
			ClosePairPosition(pair);
		}
	}

	private void OpenPairPosition(PairState pair, Sides side)
	{
		var volume = Volume;
		if (volume <= 0m)
		return;

		RegisterOrder(new Order
		{
			Security = pair.Security,
			Portfolio = Portfolio,
			Side = side,
			Volume = volume,
			Type = OrderTypes.Market,
			Comment = $"{pair.Name} entry"
		});
	}

	private void ClosePairPosition(PairState pair)
	{
		var volume = Math.Abs(pair.PositionVolume);
		if (volume <= 0m)
		return;

		var side = pair.PositionVolume > 0m ? Sides.Sell : Sides.Buy;

		RegisterOrder(new Order
		{
			Security = pair.Security,
			Portfolio = Portfolio,
			Side = side,
			Volume = volume,
			Type = OrderTypes.Market,
			Comment = $"{pair.Name} exit"
		});
	}

	private static decimal GetAdjustedPoint(Security security)
	{
		if (security == null)
		return 0m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value >= 0)
			{
				step = (decimal)Math.Pow(10, -decimals.Value);
			}
			else
			{
				step = 0.0001m;
			}
		}

		var digits = security.Decimals ?? 0;
		var adjust = digits is 3 or 5 ? 10m : 1m;

		return step * adjust;
	}

	private sealed class PairState
	{
		public PairState(string name, Security security, int fastPeriod, int slowPeriod, decimal adjustedPoint)
		{
			Name = name;
			Security = security;
			AdjustedPoint = adjustedPoint > 0m ? adjustedPoint : 0.0001m;
			FastMa = new SmoothedMovingAverage { Length = fastPeriod };
			SlowMa = new SmoothedMovingAverage { Length = slowPeriod };
		}

		public string Name { get; }
		public Security Security { get; }
		public SmoothedMovingAverage FastMa { get; }
		public SmoothedMovingAverage SlowMa { get; }
		public decimal FastValue { get; set; }
		public decimal SlowValue { get; set; }
		public decimal PositionVolume { get; set; }
		public decimal AveragePrice { get; set; }
		public decimal AdjustedPoint { get; }
		public bool IsReady { get; set; }
		public int ProcessedBars { get; set; }
	}
}
