using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader strategy Exp_SilverTrend_Duplex.
/// The strategy evaluates a SilverTrend-like color indicator on separate long and short feeds.
/// </summary>
public class SilverTrendDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<int> _longSsp;
	private readonly StrategyParam<int> _longRisk;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableLongExits;

	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<int> _shortSsp;
	private readonly StrategyParam<int> _shortRisk;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableShortExits;

	private readonly StrategyParam<decimal> _volume;

	private SilverTrendIndicator _longIndicator;
	private SilverTrendIndicator _shortIndicator;

	private readonly List<decimal> _longColorHistory = new();
	private readonly List<decimal> _shortColorHistory = new();

	/// <summary>
	/// Candle type for the long-side SilverTrend evaluation.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// SilverTrend length for the long side.
	/// </summary>
	public int LongSsp
	{
		get => _longSsp.Value;
		set => _longSsp.Value = value;
	}

	/// <summary>
	/// Risk parameter for the long-side SilverTrend filter.
	/// </summary>
	public int LongRisk
	{
		get => _longRisk.Value;
		set => _longRisk.Value = value;
	}

	/// <summary>
	/// Number of finished candles to look back for long-side signals.
	/// </summary>
	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	/// <summary>
	/// Enables opening long positions.
	/// </summary>
	public bool EnableLongEntries
	{
		get => _enableLongEntries.Value;
		set => _enableLongEntries.Value = value;
	}

	/// <summary>
	/// Enables closing long positions.
	/// </summary>
	public bool EnableLongExits
	{
		get => _enableLongExits.Value;
		set => _enableLongExits.Value = value;
	}

	/// <summary>
	/// Candle type for the short-side SilverTrend evaluation.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// SilverTrend length for the short side.
	/// </summary>
	public int ShortSsp
	{
		get => _shortSsp.Value;
		set => _shortSsp.Value = value;
	}

	/// <summary>
	/// Risk parameter for the short-side SilverTrend filter.
	/// </summary>
	public int ShortRisk
	{
		get => _shortRisk.Value;
		set => _shortRisk.Value = value;
	}

	/// <summary>
	/// Number of finished candles to look back for short-side signals.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}

	/// <summary>
	/// Enables opening short positions.
	/// </summary>
	public bool EnableShortEntries
	{
		get => _enableShortEntries.Value;
		set => _enableShortEntries.Value = value;
	}

	/// <summary>
	/// Enables closing short positions.
	/// </summary>
	public bool EnableShortExits
	{
		get => _enableShortExits.Value;
		set => _enableShortExits.Value = value;
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="SilverTrendDuplexStrategy"/>.
	/// </summary>
	public SilverTrendDuplexStrategy()
	{
		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Long Candle Type", "Candle type for the long-side SilverTrend", "Long SilverTrend");

		_longSsp = Param(nameof(LongSsp), 9)
			.SetGreaterThanZero()
			.SetDisplay("Long SSP", "SilverTrend lookback for long entries", "Long SilverTrend")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_longRisk = Param(nameof(LongRisk), 3)
			.SetGreaterThanZero()
			.SetDisplay("Long Risk", "Risk parameter for long SilverTrend", "Long SilverTrend")
			.SetCanOptimize(true)
			.SetOptimize(1, 15, 1);

		_longSignalBar = Param(nameof(LongSignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Long Signal Bar", "Offset for long-side signal evaluation", "Long SilverTrend");

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Long SilverTrend");

		_enableLongExits = Param(nameof(EnableLongExits), true)
			.SetDisplay("Enable Long Exits", "Allow closing long positions", "Long SilverTrend");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Short Candle Type", "Candle type for the short-side SilverTrend", "Short SilverTrend");

		_shortSsp = Param(nameof(ShortSsp), 9)
			.SetGreaterThanZero()
			.SetDisplay("Short SSP", "SilverTrend lookback for short entries", "Short SilverTrend")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_shortRisk = Param(nameof(ShortRisk), 3)
			.SetGreaterThanZero()
			.SetDisplay("Short Risk", "Risk parameter for short SilverTrend", "Short SilverTrend")
			.SetCanOptimize(true)
			.SetOptimize(1, 15, 1);

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Short Signal Bar", "Offset for short-side signal evaluation", "Short SilverTrend");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Short SilverTrend");

		_enableShortExits = Param(nameof(EnableShortExits), true)
			.SetDisplay("Enable Short Exits", "Allow closing short positions", "Short SilverTrend");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base order volume", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, LongCandleType);
		if (!Equals(LongCandleType, ShortCandleType))
			yield return (Security, ShortCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_longColorHistory.Clear();
		_shortColorHistory.Clear();
		_longIndicator?.Reset();
		_shortIndicator?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_longIndicator = new SilverTrendIndicator
		{
			Length = LongSsp,
			Risk = LongRisk
		};

		_shortIndicator = new SilverTrendIndicator
		{
			Length = ShortSsp,
			Risk = ShortRisk
		};

		var longSubscription = SubscribeCandles(LongCandleType);
		longSubscription
			.Bind(_longIndicator, ProcessLongCandle)
			.Start();

		var shortSubscription = SubscribeCandles(ShortCandleType);
		shortSubscription
			.Bind(_shortIndicator, ProcessShortCandle)
			.Start();

		var longArea = CreateChartArea("Long SilverTrend");
		if (longArea != null)
		{
			DrawCandles(longArea, longSubscription);
			DrawIndicator(longArea, _longIndicator);
			DrawOwnTrades(longArea);
		}

		if (!Equals(LongCandleType, ShortCandleType))
		{
			var shortArea = CreateChartArea("Short SilverTrend");
			if (shortArea != null)
			{
				DrawCandles(shortArea, shortSubscription);
				DrawIndicator(shortArea, _shortIndicator);
				DrawOwnTrades(shortArea);
			}
		}
	}

	private void ProcessLongCandle(ICandleMessage candle, decimal color)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track SilverTrend color history for long signals.
		_longColorHistory.Insert(0, color);
		var maxHistory = Math.Max(1, LongSignalBar) + 1;
		if (_longColorHistory.Count > maxHistory)
			_longColorHistory.RemoveAt(_longColorHistory.Count - 1);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_longIndicator?.IsFormed != true)
			return;

		var offset = Math.Max(1, LongSignalBar);
		if (_longColorHistory.Count <= offset)
			return;

		var currentColor = _longColorHistory[offset - 1];
		var previousColor = _longColorHistory[offset];

		var shouldClose = EnableLongExits && currentColor > 2m && Position > 0;
		if (shouldClose)
		{
			SellMarket(Position);
			LogInfo($"Exit long due to bearish SilverTrend at {candle.OpenTime:O}.");
		}

		var shouldOpen = EnableLongEntries && currentColor < 2m && previousColor > 1m && Position <= 0;
		if (shouldOpen)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Enter long due to bullish SilverTrend switch at {candle.OpenTime:O}.");
		}
	}

	private void ProcessShortCandle(ICandleMessage candle, decimal color)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track SilverTrend color history for short signals.
		_shortColorHistory.Insert(0, color);
		var maxHistory = Math.Max(1, ShortSignalBar) + 1;
		if (_shortColorHistory.Count > maxHistory)
			_shortColorHistory.RemoveAt(_shortColorHistory.Count - 1);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_shortIndicator?.IsFormed != true)
			return;

		var offset = Math.Max(1, ShortSignalBar);
		if (_shortColorHistory.Count <= offset)
			return;

		var currentColor = _shortColorHistory[offset - 1];
		var previousColor = _shortColorHistory[offset];

		var shouldClose = EnableShortExits && currentColor < 2m && Position < 0;
		if (shouldClose)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit short due to bullish SilverTrend at {candle.OpenTime:O}.");
		}

		var shouldOpen = EnableShortEntries && currentColor > 2m && previousColor > 0m && Position >= 0;
		if (shouldOpen)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Enter short due to bearish SilverTrend switch at {candle.OpenTime:O}.");
		}
	}

	private sealed class SilverTrendIndicator : Indicator<ICandleMessage>
	{
		public int Length { get; set; } = 9;
		public int Risk { get; set; } = 3;

		private readonly DonchianChannels _donchian = new();
		private int _trend;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			_donchian.Length = Length;
			var donchianValue = _donchian.Process(input);
			var bands = donchianValue as DonchianChannelsValue;

			if (bands?.UpperBand is not decimal upper || bands.LowerBand is not decimal lower)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 2m, input.Time);
			}

			if (!donchianValue.IsFormed)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 2m, input.Time);
			}

			var range = upper - lower;
			var k = 33 - Risk;
			var factor = k / 100m;
			var smin = lower + range * factor;
			var smax = upper - range * factor;

			if (candle.ClosePrice < smin)
				_trend = -1;
			else if (candle.ClosePrice > smax)
				_trend = 1;

			decimal color;
			if (_trend > 0)
				color = candle.OpenPrice <= candle.ClosePrice ? 0m : 1m;
			else if (_trend < 0)
				color = candle.OpenPrice >= candle.ClosePrice ? 4m : 3m;
			else
				color = 2m;

			IsFormed = true;
			return new DecimalIndicatorValue(this, color, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_donchian.Reset();
			_trend = 0;
		}
	}
}
