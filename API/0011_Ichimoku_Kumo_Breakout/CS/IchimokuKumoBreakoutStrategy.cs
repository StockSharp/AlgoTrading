using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Ichimoku Kumo (cloud) breakout.
/// It enters long position when price breaks above the cloud and Tenkan-sen crosses above Kijun-sen.
/// It enters short position when price breaks below the cloud and Tenkan-sen crosses below Kijun-sen.
/// </summary>
public class IchimokuKumoBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanPeriod;
	private readonly StrategyParam<DataType> _candleType;

	// Current state tracking
	private decimal _prevTenkanValue;
	private decimal _prevKijunValue;
	private bool _prevIsTenkanAboveKijun;
	private bool _prevIsPriceAboveCloud;

	/// <summary>
	/// Period for Tenkan-sen line.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Period for Kijun-sen line.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Period for Senkou Span B (used with Senkou Span A to form the cloud).
	/// </summary>
	public int SenkouSpanPeriod
	{
		get => _senkouSpanPeriod.Value;
		set => _senkouSpanPeriod.Value = value;
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
	/// Initialize the Ichimoku Kumo Breakout strategy.
	/// </summary>
	public IchimokuKumoBreakoutStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetDisplay("Tenkan-sen Period", "Period for Tenkan-sen line (faster)", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 13, 2);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetDisplay("Kijun-sen Period", "Period for Kijun-sen line (slower)", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 30, 2);

		_senkouSpanPeriod = Param(nameof(SenkouSpanPeriod), 52)
			.SetDisplay("Senkou Span B Period", "Period for Senkou Span B calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(40, 60, 4);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_prevTenkanValue = default;
		_prevKijunValue = default;
		_prevIsTenkanAboveKijun = default;
		_prevIsPriceAboveCloud = default;

	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create Ichimoku indicator
		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanPeriod }
		};

		// Create subscription and bind indicator
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ichimoku, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ichimokuTyped = (IchimokuValue)ichimokuValue;

		if (ichimokuTyped.Tenkan is not decimal tenkan)
			return;

		if (ichimokuTyped.Kijun is not decimal kijun)
			return;

		if (ichimokuTyped.SenkouA is not decimal senkouA)
			return;

		if (ichimokuTyped.SenkouB is not decimal senkouB)
			return;

		// Skip if any value is zero (indicator initializing)
		if (tenkan == 0 || kijun == 0 || senkouA == 0 || senkouB == 0)
		{
			_prevTenkanValue = tenkan;
			_prevKijunValue = kijun;
			return;
		}

		// Determine cloud boundaries
		var upperCloud = Math.Max(senkouA, senkouB);
		var lowerCloud = Math.Min(senkouA, senkouB);

		// Check price position relative to cloud
		var isPriceAboveCloud = candle.ClosePrice > upperCloud;
		var isPriceBelowCloud = candle.ClosePrice < lowerCloud;
		var isPriceInCloud = !isPriceAboveCloud && !isPriceBelowCloud;

		// Check Tenkan/Kijun cross
		var isTenkanAboveKijun = tenkan > kijun;
		var isTenkanKijunCross = isTenkanAboveKijun != _prevIsTenkanAboveKijun && _prevTenkanValue != 0;

		// Check cloud breakout
		var isBreakingAboveCloud = isPriceAboveCloud && !_prevIsPriceAboveCloud;
		var isBreakingBelowCloud = isPriceBelowCloud && (_prevIsPriceAboveCloud || !_prevIsPriceAboveCloud && !isPriceBelowCloud);

		// Trading logic
		if (isPriceAboveCloud && isTenkanAboveKijun && Position <= 0)
		{
			// Bullish conditions met - Buy signal
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Buy signal: Price ({candle.ClosePrice}) above cloud ({upperCloud}) and Tenkan ({tenkan}) > Kijun ({kijun})");
		}
		else if (isPriceBelowCloud && !isTenkanAboveKijun && Position >= 0)
		{
			// Bearish conditions met - Sell signal
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Sell signal: Price ({candle.ClosePrice}) below cloud ({lowerCloud}) and Tenkan ({tenkan}) < Kijun ({kijun})");
		}
		// Exit logic
		else if (Position > 0 && isPriceBelowCloud)
		{
			// Exit long position
			SellMarket(Position);
			LogInfo($"Exit long: Price ({candle.ClosePrice}) broke below cloud ({lowerCloud})");
		}
		else if (Position < 0 && isPriceAboveCloud)
		{
			// Exit short position
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit short: Price ({candle.ClosePrice}) broke above cloud ({upperCloud})");
		}

		// Update state for the next candle
		_prevTenkanValue = tenkan;
		_prevKijunValue = kijun;
		_prevIsTenkanAboveKijun = isTenkanAboveKijun;
		_prevIsPriceAboveCloud = isPriceAboveCloud;
	}
}