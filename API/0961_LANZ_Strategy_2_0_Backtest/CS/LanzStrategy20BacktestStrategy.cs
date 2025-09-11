using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// LANZ strategy based on swing structure and break of structure.
/// Enters at 02:00 New York time when BOS aligns with trend.
/// Calculates stop-loss from swing points and risk-reward multiplier for take-profit.
/// Positions are closed manually at 11:45 New York time.
/// </summary>
public class LanzStrategy20BacktestStrategy : Strategy
{
	private readonly StrategyParam<decimal> _accountSizeUsd;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<SlProtectionModeOption> _slProtectionMode;
	private readonly StrategyParam<int> _fullCoveragePips;
	private readonly StrategyParam<decimal> _minBosBreakPips;
	private readonly StrategyParam<decimal> _rrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	
	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _lastSwingHigh;
	private decimal? _lastSwingLow;
	private decimal? _olderSwingHigh;
	private decimal? _olderSwingLow;
	private int? _trendDir;
	private decimal? _bosLevel;
	private int? _bosDir;
	private int? _lastBosDir;
	private int? _lastTrendDir;
	
	private decimal _pipSize;
	private decimal _minBosBreakDist;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private readonly TimeZoneInfo _nyZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
	
	/// <summary>
	/// Account size in USD.
	/// </summary>
	public decimal AccountSizeUsd
	{
		get => _accountSizeUsd.Value;
		set => _accountSizeUsd.Value = value;
	}
	
	/// <summary>
	/// Risk per trade in percent.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}
	
	/// <summary>
	/// Stop-loss protection mode.
	/// </summary>
	public SlProtectionModeOption SlProtectionMode
	{
		get => _slProtectionMode.Value;
		set => _slProtectionMode.Value = value;
	}
	
	/// <summary>
	/// Pips used in full coverage mode.
	/// </summary>
	public int FullCoveragePips
	{
		get => _fullCoveragePips.Value;
		set => _fullCoveragePips.Value = value;
	}
	
	/// <summary>
	/// Minimum break of structure distance in pips.
	/// </summary>
	public decimal MinBosBreakPips
	{
		get => _minBosBreakPips.Value;
		set => _minBosBreakPips.Value = value;
	}
	
	/// <summary>
	/// Risk reward multiplier.
	/// </summary>
	public decimal RrMultiplier
	{
		get => _rrMultiplier.Value;
		set => _rrMultiplier.Value = value;
	}
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public LanzStrategy20BacktestStrategy()
	{
		_accountSizeUsd = Param(nameof(AccountSizeUsd), 100000000m)
		.SetDisplay("Account Size", "Account size in USD", "Risk");
		
		_riskPercent = Param(nameof(RiskPercent), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Risk %", "Risk percentage", "Risk");
		
		_slProtectionMode = Param(nameof(SlProtectionMode), SlProtectionModeOption.FullCoverage)
		.SetDisplay("SL Mode", "Stop-loss protection mode", "Risk");
		
		_fullCoveragePips = Param(nameof(FullCoveragePips), 12)
		.SetDisplay("Full Coverage Pips", "Pips for full coverage", "Risk");
		
		_minBosBreakPips = Param(nameof(MinBosBreakPips), 0.5m)
		.SetGreaterThanZero()
		.SetDisplay("Min BOS Break", "Minimum break of structure in pips", "General");
		
		_rrMultiplier = Param(nameof(RrMultiplier), 5.5m)
		.SetGreaterThanZero()
		.SetDisplay("RR Multiplier", "Risk reward multiplier", "Risk");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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
		
		_highs.Clear();
		_lows.Clear();
		_prevHigh = null;
		_prevLow = null;
		_lastSwingHigh = null;
		_lastSwingLow = null;
		_olderSwingHigh = null;
		_olderSwingLow = null;
		_trendDir = null;
		_bosLevel = null;
		_bosDir = null;
		_lastBosDir = null;
		_lastTrendDir = null;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_pipSize = (Security.PriceStep ?? 1m) * 10m;
		_minBosBreakDist = MinBosBreakPips * _pipSize;
		
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
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		if (_highs.Count > 10)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}
		
		if (_highs.Count >= 5)
		{
			var high2 = _highs[^3];
			var high3 = _highs[^4];
			var high4 = _highs[^5];
			var low2 = _lows[^3];
			var low3 = _lows[^4];
			var low4 = _lows[^5];
			
			var hh = high2 > high3 && high3 > high4;
			var ll = low2 < low3 && low3 < low4;
			
			if (hh)
			{
				_olderSwingHigh = _prevHigh;
				_prevHigh = _lastSwingHigh;
				_lastSwingHigh = high2;
			}
			
			if (ll)
			{
				_olderSwingLow = _prevLow;
				_prevLow = _lastSwingLow;
				_lastSwingLow = low2;
			}
		}
		
		if (_prevHigh.HasValue && _lastSwingHigh.HasValue && _prevLow.HasValue && _lastSwingLow.HasValue)
		{
			var isBullish = _lastSwingHigh > _prevHigh && _lastSwingLow > _prevLow;
			var isBearish = _lastSwingHigh < _prevHigh && _lastSwingLow < _prevLow;
			_trendDir = isBullish ? 1 : isBearish ? -1 : _trendDir;
		}
		
		var newBosUp = _lastSwingHigh.HasValue && candle.ClosePrice > _lastSwingHigh + _minBosBreakDist;
		var newBosDown = _lastSwingLow.HasValue && candle.ClosePrice < _lastSwingLow - _minBosBreakDist;
		
		if (newBosUp)
		{
			_bosLevel = _lastSwingHigh;
			_bosDir = 1;
		}
		else if (newBosDown)
		{
			_bosLevel = _lastSwingLow;
			_bosDir = -1;
		}
		
		if (_bosDir.HasValue)
		_lastBosDir = _bosDir;
		
		if (_trendDir.HasValue)
		_lastTrendDir = _trendDir;
		
		var nyTime = TimeZoneInfo.ConvertTime(candle.OpenTime, _nyZone);
		var isAnalysisBar = nyTime.Hour == 2 && nyTime.Minute == 0;
		var manualClose = nyTime.Hour == 11 && nyTime.Minute == 45;
		
		var alreadyInTrade = Position != 0;
		
		if (alreadyInTrade)
		{
			if (Position > 0)
			{
				if (candle.LowPrice <= _stopPrice)
				{
					SellMarket(Position);
					return;
				}
				if (candle.HighPrice >= _takeProfitPrice)
				{
					SellMarket(Position);
					return;
				}
			}
			else
			{
				var absPos = Math.Abs(Position);
				if (candle.HighPrice >= _stopPrice)
				{
					BuyMarket(absPos);
					return;
				}
				if (candle.LowPrice <= _takeProfitPrice)
				{
					BuyMarket(absPos);
					return;
				}
			}
			
			if (manualClose)
			{
				if (Position > 0)
				SellMarket(Position);
				else if (Position < 0)
				BuyMarket(Math.Abs(Position));
				return;
			}
		}
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (alreadyInTrade)
		return;
		
		var enterLong = isAnalysisBar && _lastBosDir == 1 && _lastTrendDir == 1;
		var enterShort = isAnalysisBar && _lastBosDir == -1 && _lastTrendDir == -1;
		var enterFallbackLong = isAnalysisBar && _lastBosDir == 1 && _lastTrendDir != 1;
		var enterFallbackShort = isAnalysisBar && _lastBosDir == -1 && _lastTrendDir != -1;
		
		var entryPrice = candle.ClosePrice;
		var riskUsd = AccountSizeUsd * (RiskPercent / 100m);
		
		if (enterLong || enterFallbackLong)
		{
			var fallbackSl = entryPrice - 5m * _pipSize;
			var slBase = SlProtectionMode switch
			{
				SlProtectionModeOption.FirstSwing => _lastSwingLow ?? fallbackSl,
				SlProtectionModeOption.SecondSwing => _prevLow ?? fallbackSl,
				SlProtectionModeOption.FullCoverage => (_olderSwingLow == null || _prevLow == null || _lastSwingLow == null)
				? fallbackSl
				: Math.Min((decimal)_olderSwingLow, Math.Min((decimal)_prevLow, (decimal)_lastSwingLow)) - FullCoveragePips * _pipSize,
				_ => fallbackSl
			};
			var slPrice = (entryPrice - slBase) < (10m * _pipSize) ? entryPrice - 10m * _pipSize : slBase;
			var tpPrice = entryPrice + RrMultiplier * (entryPrice - slPrice);
			var slPips = Math.Abs(entryPrice - slPrice) / _pipSize;
			var lotSize = slPips == 0 ? 0 : riskUsd / (slPips * 10m);
			if (lotSize > 0)
			{
				BuyLimit(entryPrice, lotSize);
				_stopPrice = slPrice;
				_takeProfitPrice = tpPrice;
			}
		}
		else if (enterShort || enterFallbackShort)
		{
			var fallbackSl = entryPrice + 5m * _pipSize;
			var slBase = SlProtectionMode switch
			{
				SlProtectionModeOption.FirstSwing => _lastSwingHigh ?? fallbackSl,
				SlProtectionModeOption.SecondSwing => _prevHigh ?? fallbackSl,
				SlProtectionModeOption.FullCoverage => (_olderSwingHigh == null || _prevHigh == null || _lastSwingHigh == null)
				? fallbackSl
				: Math.Max((decimal)_olderSwingHigh, Math.Max((decimal)_prevHigh, (decimal)_lastSwingHigh)) + FullCoveragePips * _pipSize,
				_ => fallbackSl
			};
			var slPrice = (slBase - entryPrice) < (10m * _pipSize) ? entryPrice + 10m * _pipSize : slBase;
			var tpPrice = entryPrice - RrMultiplier * (slPrice - entryPrice);
			var slPips = Math.Abs(entryPrice - slPrice) / _pipSize;
			var lotSize = slPips == 0 ? 0 : riskUsd / (slPips * 10m);
			if (lotSize > 0)
			{
				SellLimit(entryPrice, lotSize);
				_stopPrice = slPrice;
				_takeProfitPrice = tpPrice;
			}
		}
	}
}

/// <summary>
/// Modes of stop-loss protection.
/// </summary>
public enum SlProtectionModeOption
{
	/// <summary>Use last swing.</summary>
	FirstSwing,
	/// <summary>Use second swing.</summary>
	SecondSwing,
	/// <summary>Use full coverage of swings.</summary>
	FullCoverage
}

