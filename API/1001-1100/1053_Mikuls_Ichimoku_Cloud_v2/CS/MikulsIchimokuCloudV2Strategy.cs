using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mikul's Ichimoku Cloud v2 strategy.
/// Goes long when Tenkan-sen crosses above Kijun-sen with the close above the cloud, or when the close breaks above a green cloud,
/// optionally only above a moving average. The long is protected by a trailing stop built from ATR or a percent below the recent
/// swing low (or the close), and can also exit on an Ichimoku reversal and a percent take profit.
/// </summary>
public class MikulsIchimokuCloudV2Strategy : Strategy
{
	/// <summary>
	/// Price the trailing stop is measured from.
	/// </summary>
	public enum TrailSources
	{
		/// <summary>
		/// Lowest low of the last SwingLookback candles.
		/// </summary>
		LowsHighs,

		/// <summary>
		/// Candle close.
		/// </summary>
		Close,
	}

	/// <summary>
	/// Distance of the trailing stop.
	/// </summary>
	public enum TrailMethods
	{
		/// <summary>
		/// ATR multiplied by AtrMultiplier.
		/// </summary>
		Atr,

		/// <summary>
		/// TrailPercent of the source price.
		/// </summary>
		Percent,
	}

	/// <summary>
	/// Moving average types for the filter.
	/// </summary>
	public enum MaTypes
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Sma,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Ema,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Wma,
	}

	private readonly StrategyParam<TrailSources> _trailSource;
	private readonly StrategyParam<TrailMethods> _trailMethod;
	private readonly StrategyParam<decimal> _trailPercent;
	private readonly StrategyParam<int> _swingLookback;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<bool> _addIchiExit;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _useMaFilter;
	private readonly StrategyParam<MaTypes> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouBPeriod;
	private readonly StrategyParam<int> _displacement;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = [];
	private readonly List<decimal> _lows = [];
	private readonly List<decimal> _senkouA = [];
	private readonly List<decimal> _senkouB = [];
	private decimal? _prevTenkan;
	private decimal? _prevKijun;
	private decimal? _prevClose;
	private decimal? _prevCloudTop;
	private decimal? _trailStop;
	private decimal _entryPrice;

	/// <summary>
	/// Price the trailing stop is measured from.
	/// </summary>
	public TrailSources TrailSource
	{
		get => _trailSource.Value;
		set => _trailSource.Value = value;
	}

	/// <summary>
	/// Trailing stop method.
	/// </summary>
	public TrailMethods TrailMethod
	{
		get => _trailMethod.Value;
		set => _trailMethod.Value = value;
	}

	/// <summary>
	/// Trailing distance in percent for the Percent method.
	/// </summary>
	public decimal TrailPercent
	{
		get => _trailPercent.Value;
		set => _trailPercent.Value = value;
	}

	/// <summary>
	/// Candles used for the swing low.
	/// </summary>
	public int SwingLookback
	{
		get => _swingLookback.Value;
		set => _swingLookback.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the trailing stop.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Exit on an Ichimoku reversal.
	/// </summary>
	public bool AddIchiExit
	{
		get => _addIchiExit.Value;
		set => _addIchiExit.Value = value;
	}

	/// <summary>
	/// Use the percent take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Take profit in percent from the entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Require the close above the moving average for entries.
	/// </summary>
	public bool UseMaFilter
	{
		get => _useMaFilter.Value;
		set => _useMaFilter.Value = value;
	}

	/// <summary>
	/// Moving average type of the filter.
	/// </summary>
	public MaTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Moving average length of the filter.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Tenkan-sen period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Senkou Span B period.
	/// </summary>
	public int SenkouBPeriod
	{
		get => _senkouBPeriod.Value;
		set => _senkouBPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift of the cloud in candles.
	/// </summary>
	public int Displacement
	{
		get => _displacement.Value;
		set => _displacement.Value = value;
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
	public MikulsIchimokuCloudV2Strategy()
	{
		_trailSource = Param(nameof(TrailSource), TrailSources.LowsHighs)
			.SetDisplay("Trail Source", "Price the trailing stop is measured from", "Risk");

		_trailMethod = Param(nameof(TrailMethod), TrailMethods.Atr)
			.SetDisplay("Trail Method", "Trailing stop method", "Risk");

		_trailPercent = Param(nameof(TrailPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trail %", "Trailing distance in percent for the Percent method", "Risk");

		_swingLookback = Param(nameof(SwingLookback), 7)
			.SetGreaterThanZero()
			.SetDisplay("Swing Lookback", "Candles used for the swing low", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "ATR multiplier for the trailing stop", "Risk");

		_addIchiExit = Param(nameof(AddIchiExit), false)
			.SetDisplay("Ichimoku Exit", "Exit on an Ichimoku reversal", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), false)
			.SetDisplay("Use Take Profit", "Use the percent take profit", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 25m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit in percent from the entry price", "Risk");

		_useMaFilter = Param(nameof(UseMaFilter), false)
			.SetDisplay("Use MA Filter", "Require the close above the moving average", "Filters");

		_maType = Param(nameof(MaType), MaTypes.Ema)
			.SetDisplay("MA Type", "Moving average type of the filter", "Filters");

		_maLength = Param(nameof(MaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length of the filter", "Filters");

		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku");

		_senkouBPeriod = Param(nameof(SenkouBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou B Period", "Senkou Span B period", "Ichimoku");

		_displacement = Param(nameof(Displacement), 26)
			.SetGreaterThanZero()
			.SetDisplay("Displacement", "Forward shift of the cloud in candles", "Ichimoku");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_highs.Clear();
		_lows.Clear();
		_senkouA.Clear();
		_senkouB.Clear();
		_prevTenkan = null;
		_prevKijun = null;
		_prevClose = null;
		_prevCloudTop = null;
		_trailStop = null;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = AtrPeriod };
		IIndicator ma = MaType switch
		{
			MaTypes.Sma => new SimpleMovingAverage { Length = MaLength },
			MaTypes.Wma => new WeightedMovingAverage { Length = MaLength },
			_ => new ExponentialMovingAverage { Length = MaLength },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (UseMaFilter)
				DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private static decimal Midpoint(List<decimal> highs, List<decimal> lows, int length)
	{
		var start = highs.Count - length;
		return (highs.Skip(start).Max() + lows.Skip(start).Min()) / 2m;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var maxLength = Math.Max(SwingLookback, Math.Max(TenkanPeriod, Math.Max(KijunPeriod, SenkouBPeriod)));

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		if (_highs.Count > maxLength)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < maxLength)
			return;

		var close = candle.ClosePrice;
		var tenkan = Midpoint(_highs, _lows, TenkanPeriod);
		var kijun = Midpoint(_highs, _lows, KijunPeriod);

		_senkouA.Add((tenkan + kijun) / 2m);
		_senkouB.Add(Midpoint(_highs, _lows, SenkouBPeriod));
		if (_senkouA.Count > Displacement)
		{
			_senkouA.RemoveAt(0);
			_senkouB.RemoveAt(0);
		}

		var prevTenkan = _prevTenkan;
		var prevKijun = _prevKijun;
		var prevClose = _prevClose;
		var prevCloudTop = _prevCloudTop;
		_prevTenkan = tenkan;
		_prevKijun = kijun;
		_prevClose = close;

		// The cloud plotted on this candle was calculated Displacement - 1 candles earlier.
		if (_senkouA.Count < Displacement || !atrValue.IsFormed || !maValue.IsFormed)
			return;

		var cloudA = _senkouA[0];
		var cloudB = _senkouB[0];
		var cloudTop = Math.Max(cloudA, cloudB);
		var cloudBottom = Math.Min(cloudA, cloudB);
		_prevCloudTop = cloudTop;

		var atr = atrValue.GetValue<decimal>();
		var ma = maValue.GetValue<decimal>();

		if (Position > 0)
		{
			var exit = _trailStop is decimal stop && candle.LowPrice <= stop;

			if (!exit && UseTakeProfit && candle.HighPrice >= _entryPrice * (1m + TakeProfitPercent / 100m))
				exit = true;

			if (!exit && AddIchiExit)
			{
				var bearishCross = prevTenkan is decimal pt && prevKijun is decimal pk && pt >= pk && tenkan < kijun;
				exit = bearishCross || close < cloudBottom;
			}

			if (exit)
			{
				SellMarket(Position);
				_trailStop = null;
				return;
			}

			var candidate = GetTrailCandidate(close, atr);
			if (_trailStop is not decimal current || candidate > current)
				_trailStop = candidate;

			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var tkCross = prevTenkan is decimal t && prevKijun is decimal k && t <= k && tenkan > kijun && close > cloudTop;
		var breakout = prevClose is decimal pc && prevCloudTop is decimal pct && pc <= pct && close > cloudTop && cloudA > cloudB;
		var maOk = !UseMaFilter || close > ma;

		if (Position == 0 && (tkCross || breakout) && maOk)
		{
			BuyMarket(Volume);
			_entryPrice = close;
			_trailStop = GetTrailCandidate(close, atr);
		}
	}

	private decimal GetTrailCandidate(decimal close, decimal atr)
	{
		var source = TrailSource == TrailSources.LowsHighs
			? _lows.Skip(_lows.Count - SwingLookback).Min()
			: close;

		return TrailMethod == TrailMethods.Atr
			? source - atr * AtrMultiplier
			: source * (1m - TrailPercent / 100m);
	}
}
