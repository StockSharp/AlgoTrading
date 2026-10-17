using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum strategy that allows longs or shorts only when the current month historically supports that seasonal bias.
/// </summary>
public class SeasonalityAdjustedMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _seasonalityThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly StrategyParam<string> _seasonalityHistoryFile;
	private readonly StrategyParam<int> _minimumSeasonalSamples;
	private readonly StrategyParam<int> _seasonalityLookbackYears;
	private readonly List<(DateTime month, decimal value)> _seasonalHistory = new();
	private DateTime? _seasonalMonth;
	private decimal? _seasonalValue;
	private Momentum _momentum;
	private SimpleMovingAverage _momentumAverage;
	private int _cooldown;

	/// <summary>CSV monthly opening/closing prices: SecurityId,Month,Open,Close.</summary>
	public string SeasonalityHistoryFile { get => _seasonalityHistoryFile.Value; set => _seasonalityHistoryFile.Value = value; }

	/// <summary>Required completed observations of the same calendar month.</summary>
	public int MinimumSeasonalSamples { get => _minimumSeasonalSamples.Value; set => _minimumSeasonalSamples.Value = value; }

	/// <summary>Maximum number of prior years used for each seasonal estimate.</summary>
	public int SeasonalityLookbackYears { get => _seasonalityLookbackYears.Value; set => _seasonalityLookbackYears.Value = value; }
	/// <summary>
	/// Period for the momentum indicator.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute seasonality strength required to allow directional entries.
	/// </summary>
	public decimal SeasonalityThreshold
	{
		get => _seasonalityThreshold.Value;
		set => _seasonalityThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Bars to wait after each order.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public SeasonalityAdjustedMomentumStrategy()
	{
		_seasonalityHistoryFile = Param<string>(nameof(SeasonalityHistoryFile)).SetRequired()
			.SetDisplay("Seasonality History File", "CSV monthly prices for the execution instrument", "Seasonality");
		_minimumSeasonalSamples = Param(nameof(MinimumSeasonalSamples), 2).SetGreaterThanZero()
			.SetDisplay("Minimum Seasonal Samples", "Completed observations required for the same month", "Seasonality");
		_seasonalityLookbackYears = Param(nameof(SeasonalityLookbackYears), 10).SetRange(1, 100)
			.SetDisplay("Seasonality Lookback Years", "Historical years used by the seasonal estimate", "Seasonality");
		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetRange(3, 100)
			.SetDisplay("Momentum Period", "Period for the momentum indicator", "Indicators");

		_seasonalityThreshold = Param(nameof(SeasonalityThreshold), 0.5m)
			.SetRange(0m, 1m)
			.SetDisplay("Seasonality Threshold", "Minimum absolute seasonality strength required for entries", "Signals");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 120)
			.SetRange(1, 500)
			.SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for the strategy", "General");

	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_seasonalHistory.Clear();
		_seasonalMonth = null;
		_seasonalValue = null;

		_momentum = null;
		_momentumAverage = null;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		LoadSeasonalityHistory();
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		_momentum = new Momentum { Length = MomentumPeriod };
		_momentumAverage = new SimpleMovingAverage { Length = MomentumPeriod };
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_momentum, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _momentum);
			DrawIndicator(area, _momentumAverage);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Start();
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var momentumAvgValue = _momentumAverage.Process(momentumValue, candle.OpenTime, true).ToDecimal();

		if (!_momentum.IsFormed || !_momentumAverage.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var seasonalStrength = GetSeasonalStrength(candle.OpenTime);
		var allowLong = seasonalStrength >= SeasonalityThreshold;
		var allowShort = seasonalStrength <= -SeasonalityThreshold;
		var bullishMomentum = momentumValue > momentumAvgValue;
		var bearishMomentum = momentumValue < momentumAvgValue;

		if (Position > 0)
		{
			if (!allowLong || bearishMomentum)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}

			return;
		}

		if (Position < 0)
		{
			if (!allowShort || bullishMomentum)
			{
				BuyMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}

			return;
		}

		if (allowLong && bullishMomentum)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (allowShort && bearishMomentum)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
	}

	private void LoadSeasonalityHistory()
	{
		if (string.IsNullOrWhiteSpace(SeasonalityHistoryFile))
			throw new InvalidOperationException("SeasonalityHistoryFile is required.");
		_seasonalHistory.Clear();
		_seasonalMonth = null;
		_seasonalValue = null;
		var lines = File.ReadLines(SeasonalityHistoryFile).GetEnumerator();
		using (lines)
		{
			if (!lines.MoveNext() || lines.Current.Trim() != "SecurityId,Month,Open,Close")
				throw new InvalidOperationException("Seasonality history must begin with SecurityId,Month,Open,Close.");
			var months = new HashSet<DateTime>();
			while (lines.MoveNext())
			{
				if (string.IsNullOrWhiteSpace(lines.Current))
					continue;
				var columns = lines.Current.Split(',');
				if (columns.Length != 4)
					throw new InvalidOperationException("Every seasonal observation must contain four CSV fields.");
				if (!string.Equals(columns[0].Trim(), Security.Id, StringComparison.OrdinalIgnoreCase))
					continue;
				var month = DateTime.ParseExact(columns[1].Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
				var open = decimal.Parse(columns[2], CultureInfo.InvariantCulture);
				var close = decimal.Parse(columns[3], CultureInfo.InvariantCulture);
				if (open <= 0m || close <= 0m || !months.Add(month))
					throw new InvalidOperationException("Seasonality prices must be positive and months must be unique.");
				_seasonalHistory.Add((month, 100m * (close / open - 1m)));
			}
		}
		if (_seasonalHistory.Count == 0)
			throw new InvalidOperationException("Seasonality history contains no observations for the execution instrument.");
	}

	private decimal? GetSeasonalStrength(DateTime time)
	{
		var month = new DateTime(time.Year, time.Month, 1, 0, 0, 0, DateTimeKind.Utc);
		if (_seasonalMonth != month)
		{
			var earliest = month.AddYears(-SeasonalityLookbackYears);
			var history = _seasonalHistory.Where(h => h.month.Month == month.Month
				&& h.month < month && h.month >= earliest).Select(h => h.value).ToArray();
			_seasonalValue = history.Length >= MinimumSeasonalSamples ? history.Average() : null;
			_seasonalMonth = month;
		}
		return _seasonalValue;
	}

}
