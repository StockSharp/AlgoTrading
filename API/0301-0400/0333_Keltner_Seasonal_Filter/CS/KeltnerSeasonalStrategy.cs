using System;
using System.Globalization;
using System.IO;
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
/// Strategy that trades based on Keltner Channel breakouts with seasonal bias filter.
/// Enters position when price breaks Keltner Channel with seasonal bias confirmation.
/// </summary>
public class KeltnerSeasonalStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _seasonalThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly StrategyParam<string> _seasonalityHistoryFile;
	private readonly StrategyParam<int> _minimumSeasonalSamples;
	private readonly StrategyParam<int> _seasonalityLookbackYears;
	private readonly List<(DateTime month, decimal value)> _seasonalHistory = new();
	private DateTime? _seasonalMonth;
	private decimal? _seasonalValue;
	private decimal _currentSeasonalStrength;
	private bool _isSeasonalReady;
	private decimal _stopPrice;

	/// <summary>CSV monthly opening/closing prices: SecurityId,Month,Open,Close.</summary>
	public string SeasonalityHistoryFile { get => _seasonalityHistoryFile.Value; set => _seasonalityHistoryFile.Value = value; }

	/// <summary>Required completed observations of the same calendar month.</summary>
	public int MinimumSeasonalSamples { get => _minimumSeasonalSamples.Value; set => _minimumSeasonalSamples.Value = value; }

	/// <summary>Maximum number of prior years used for each seasonal estimate.</summary>
	public int SeasonalityLookbackYears { get => _seasonalityLookbackYears.Value; set => _seasonalityLookbackYears.Value = value; }
	/// <summary>
	/// Strategy parameter: EMA period for Keltner Channel.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Strategy parameter: ATR period for Keltner Channel.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Strategy parameter: ATR multiplier for Keltner Channel.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Seasonal strength threshold.
	/// </summary>
	public decimal SeasonalThreshold
	{
		get => _seasonalThreshold.Value;
		set => _seasonalThreshold.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public KeltnerSeasonalStrategy()
	{
		_seasonalityHistoryFile = Param<string>(nameof(SeasonalityHistoryFile)).SetRequired()
			.SetDisplay("Seasonality History File", "CSV monthly prices for the execution instrument", "Seasonality");
		_minimumSeasonalSamples = Param(nameof(MinimumSeasonalSamples), 2).SetGreaterThanZero()
			.SetDisplay("Minimum Seasonal Samples", "Completed observations required for the same month", "Seasonality");
		_seasonalityLookbackYears = Param(nameof(SeasonalityLookbackYears), 10).SetRange(1, 100)
			.SetDisplay("Seasonality Lookback Years", "Historical years used by the seasonal estimate", "Seasonality");
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA in Keltner Channel", "Indicator Settings");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR in Keltner Channel", "Indicator Settings");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for ATR to set channel width", "Indicator Settings");

		_seasonalThreshold = Param(nameof(SeasonalThreshold), 0.5m)
			.SetDisplay("Seasonal Threshold", "Minimum seasonal strength to consider for trading", "Seasonal Settings");

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
		_seasonalHistory.Clear();
		_seasonalMonth = null;
		_seasonalValue = null;
		_isSeasonalReady = false;

		_currentSeasonalStrength = 0;
		_stopPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		LoadSeasonalityHistory();
		base.OnStarted2(time);

		// Create indicators for Keltner Channel
		var ema = new EMA
		{
			Length = EmaPeriod
		};

		var atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		// Create subscription for candles
		var subscription = SubscribeCandles(CandleType);

		// Bind indicators to subscription and start
		subscription
			.Bind(ema, atr, ProcessKeltner)
			.Start();

		// Add chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	/// <summary>
	/// Evaluates seasonal Keltner entries: 1 for buy, -1 for sell and 0 for no entry.
	/// </summary>
	public static int GetEntrySignal(
		decimal seasonalStrength,
		decimal close,
		decimal ema,
		decimal atr,
		decimal multiplier,
		decimal threshold,
		decimal position)
	{
		if (seasonalStrength > threshold && close > ema + atr * multiplier && position <= 0m)
			return 1;
		if (seasonalStrength < -threshold && close < ema - atr * multiplier && position >= 0m)
			return -1;
		return 0;
	}

	private void ProcessKeltner(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Seasonality belongs to the candle being evaluated, never to host/start time.
		UpdateSeasonalStrength(candle.OpenTime);

		// Calculate Keltner Channel bands
		decimal upperBand = emaValue + atrValue * AtrMultiplier;
		decimal lowerBand = emaValue - atrValue * AtrMultiplier;

		// Stop placed AtrMultiplier x ATR from the entry close, checked on the finished candle
		if (_stopPrice > 0m &&
		((Position > 0 && candle.ClosePrice <= _stopPrice) ||
		(Position < 0 && candle.ClosePrice >= _stopPrice)))
		{
			LogInfo($"Stop exit: close {candle.ClosePrice} reached ATR stop ({_stopPrice})");
			ClosePosition();
			_stopPrice = 0m;
			return;
		}


		var signal = _isSeasonalReady ? GetEntrySignal(
			_currentSeasonalStrength, candle.ClosePrice, emaValue, atrValue,
			AtrMultiplier, SeasonalThreshold, Position) : 0;

		if (signal > 0)
		{
			LogInfo($"Buy signal: breakout above Keltner upper band ({upperBand}) with seasonal bias ({_currentSeasonalStrength})");
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.ClosePrice - atrValue * AtrMultiplier;
			return;
		}
		if (signal < 0)
		{
			LogInfo($"Sell signal: breakout below Keltner lower band ({lowerBand}) with seasonal bias ({_currentSeasonalStrength})");
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.ClosePrice + atrValue * AtrMultiplier;
			return;
		}

		// Exit rules based on middle line reversion
		if ((Position > 0 && candle.ClosePrice < emaValue) ||
		(Position < 0 && candle.ClosePrice > emaValue))
		{
			LogInfo($"Exit signal: Price reverted to EMA ({emaValue})");
			ClosePosition();
			_stopPrice = 0m;
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

	private void UpdateSeasonalStrength(DateTime time)
	{
		var value = GetSeasonalStrength(time);
		_isSeasonalReady = value.HasValue;
		_currentSeasonalStrength = value ?? 0m;
	}
}
