namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Prior completed daily Fibonacci range and daily R/S Hurst filter with capped intraday entries.</summary>
public class FibHurstBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _hurstPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<int> _maxTotalTrades;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<string> _dailyHistoryFile;
	private Order _pending;
	private readonly SortedDictionary<DateTime, (decimal high, decimal low, decimal close)> _daily = [];
	private decimal? _previousClose;
	private DateTime _day;
	private int _dailyTrades;
	private int _totalTrades;
	/// <summary>Finished signal candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Completed daily closes in the R/S Hurst window.</summary>
	public int HurstPeriod { get => _hurstPeriod.Value; set => _hurstPeriod.Value = value; }

	/// <summary>Prior completed daily bars in the Fibonacci range.</summary>
	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }

	/// <summary>Maximum filled-entry attempts per historical UTC day.</summary>
	public int MaxTradesPerDay { get => _maxTradesPerDay.Value; set => _maxTradesPerDay.Value = value; }

	/// <summary>Maximum entry attempts in one run.</summary>
	public int MaxTotalTrades { get => _maxTotalTrades.Value; set => _maxTotalTrades.Value = value; }

	/// <summary>Native stop distance as a percentage of the actual fill.</summary>
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }

	/// <summary>Take-profit percentage divided by the stop percentage.</summary>
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }

	/// <summary>Optional dated daily High/Low/Close CSV for warmup.</summary>
	public string DailyHistoryFile { get => _dailyHistoryFile.Value; set => _dailyHistoryFile.Value = value; }

	/// <summary>Initialize the strategy parameters.</summary>
	public FibHurstBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("CandleType", "Finished signal candle type.", "Parameters");
		_hurstPeriod = Param(nameof(HurstPeriod), 50).SetRange(2, int.MaxValue)
			.SetDisplay("HurstPeriod", "Completed daily closes in the R/S Hurst window.", "Parameters");
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20).SetRange(1, int.MaxValue)
			.SetDisplay("LookbackPeriod", "Prior completed daily bars in the Fibonacci range.", "Parameters");
		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 5).SetRange(1, int.MaxValue)
			.SetDisplay("MaxTradesPerDay", "Maximum filled-entry attempts per historical UTC day.", "Parameters");
		_maxTotalTrades = Param(nameof(MaxTotalTrades), 510).SetRange(1, int.MaxValue)
			.SetDisplay("MaxTotalTrades", "Maximum entry attempts in one run.", "Parameters");
		_riskPercent = Param(nameof(RiskPercent), 2m).SetRange(0.000001m, decimal.MaxValue)
			.SetDisplay("RiskPercent", "Native stop distance as a percentage of the actual fill.", "Parameters");
		_riskReward = Param(nameof(RiskReward), 2m).SetRange(0.000001m, decimal.MaxValue)
			.SetDisplay("RiskReward", "Take-profit percentage divided by the stop percentage.", "Parameters");
		_dailyHistoryFile = Param<string>(nameof(DailyHistoryFile))
			.SetDisplay("DailyHistoryFile", "Optional dated daily High/Low/Close CSV for warmup.", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pending = null;
		_daily.Clear();
		_previousClose = null;
		_day = default;
		_dailyTrades = 0;
		_totalTrades = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (CandleType.Arg is not TimeSpan frame || frame <= TimeSpan.Zero || frame >= TimeSpan.FromDays(1))
			throw new InvalidOperationException("CandleType must be an intraday time frame.");
		LoadDailyHistory();
		base.OnStarted2(time);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quote = new Subscription(DataType.Level1, Security);
			quote.MarketData.BuildField = field;
			SubscribeLevel1(quote).Start();
		}
		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame()).Bind(candle =>
		{
			if (candle.State == CandleStates.Finished)
				_daily[candle.OpenTime.Date] = (candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		}).Start();
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private bool IsPending => _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed);

	private void Submit(Sides side, decimal quantity, string comment)
	{
		_pending = CreateOrder(side, 0m, quantity);
		_pending.Type = OrderTypes.Market;
		_pending.Comment = comment;
		RegisterOrder(_pending);
	}

	private void LoadDailyHistory()
	{
		if (string.IsNullOrWhiteSpace(DailyHistoryFile)) return;
		using var lines = File.ReadLines(DailyHistoryFile).GetEnumerator();
		if (!lines.MoveNext() || lines.Current.Trim() != "Date,High,Low,Close")
			throw new InvalidOperationException("Daily CSV header must be Date,High,Low,Close.");
		while (lines.MoveNext())
		{
			if (string.IsNullOrWhiteSpace(lines.Current)) continue;
			var row = lines.Current.Split(',');
			if (row.Length != 4) throw new InvalidOperationException("Invalid daily CSV field count.");
			var date = DateTime.ParseExact(row[0].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
			var high = decimal.Parse(row[1], CultureInfo.InvariantCulture);
			var low = decimal.Parse(row[2], CultureInfo.InvariantCulture);
			var close = decimal.Parse(row[3], CultureInfo.InvariantCulture);
			if (low <= 0m || high < low || close < low || close > high || !_daily.TryAdd(date, (high, low, close)))
				throw new InvalidOperationException("Daily history must contain unique dates and valid positive OHLC.");
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished) return;
		var previous = _previousClose;
		_previousClose = candle.ClosePrice;
		if (_day != candle.OpenTime.Date)
		{
			_day = candle.OpenTime.Date;
			_dailyTrades = 0;
		}
		var days = _daily.Where(d => d.Key < _day).Select(d => d.Value).ToArray();
		if (days.Length < Math.Max(HurstPeriod, LookbackPeriod) || previous is null || Position != 0m || IsPending
			|| _dailyTrades >= MaxTradesPerDay || _totalTrades >= MaxTotalTrades || !IsFormedAndOnlineAndAllowTrading()) return;
		var prices = days.TakeLast(HurstPeriod).Select(d => (double)d.close).ToArray();
		var mean = prices.Average();
		var variance = prices.Sum(p => (p - mean) * (p - mean)) / prices.Length;
		if (variance <= 0d) return;
		var cumulative = 0d;
		var minimum = double.MaxValue;
		var maximum = double.MinValue;
		foreach (var price in prices)
		{
			cumulative += price - mean;
			minimum = Math.Min(minimum, cumulative);
			maximum = Math.Max(maximum, cumulative);
		}
		var rescaled = (maximum - minimum) / Math.Sqrt(variance);
		if (rescaled <= 0d) return;
		var hurst = Math.Log(rescaled) / Math.Log(prices.Length);
		var range = days.TakeLast(LookbackPeriod).ToArray();
		var low = range.Min(d => d.low);
		var high = range.Max(d => d.high);
		if (high <= low) return;
		var upper = low + 0.618m * (high - low);
		var lower = low + 0.382m * (high - low);
		Sides? side = previous <= upper && candle.ClosePrice > upper && hurst > 0.5d ? Sides.Buy
			: previous >= lower && candle.ClosePrice < lower && hurst < 0.5d ? Sides.Sell : null;
		if (side is null) return;
		_dailyTrades++;
		_totalTrades++;
		StartProtection(new Unit(RiskPercent * RiskReward, UnitTypes.Percent), new Unit(RiskPercent, UnitTypes.Percent),
			useMarketOrders: true, isLocalStop: true);
		Submit(side.Value, Volume, "0502Entry");
	}

}
