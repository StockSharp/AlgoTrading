using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku cloud width mean reversion strategy.
/// Trades contractions and expansions of the Ichimoku cloud width around its recent average.
/// </summary>
public class IchimokuCloudWidthMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private Ichimoku _ichimoku;
	private decimal[] _cloudWidthHistory;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;

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
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period for cloud width statistics.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Deviation multiplier for mean reversion detection.
	/// </summary>
	public decimal DeviationMultiplier
	{
		get => _deviationMultiplier.Value;
		set => _deviationMultiplier.Value = value;
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
	/// Cooldown bars between orders.
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
	/// Initializes a new instance of <see cref="IchimokuCloudWidthMeanReversionStrategy"/>.
	/// </summary>
	public IchimokuCloudWidthMeanReversionStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku")
			.SetOptimize(5, 20, 1);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku")
			.SetOptimize(20, 40, 2);

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span B Period", "Senkou Span B period", "Ichimoku")
			.SetOptimize(40, 80, 4);

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Lookback period for cloud width statistics", "Strategy Parameters")
			.SetOptimize(10, 50, 5);

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Deviation multiplier for mean reversion detection", "Strategy Parameters")
			.SetOptimize(1m, 3m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 1200)
			.SetRange(1, 5000)
			.SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");
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
		_ichimoku = null;
		_currentIndex = default;
		_filledCount = default;
		_cooldown = default;
		_cloudWidthHistory = new decimal[LookbackPeriod];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod },
		};

		_cloudWidthHistory = new decimal[LookbackPeriod];
		_currentIndex = 0;
		_filledCount = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ichimoku, ProcessIchimoku)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ichimoku);
			DrawOwnTrades(area);
		}

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessIchimoku(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ichimoku.IsFormed)
			return;

		var typedValue = (IchimokuValue)ichimokuValue;
		if (typedValue.SenkouA is not decimal senkouA ||
			typedValue.SenkouB is not decimal senkouB)
			return;

		var cloudWidth = Math.Abs(senkouA - senkouB);

		_cloudWidthHistory[_currentIndex] = cloudWidth;
		_currentIndex = (_currentIndex + 1) % LookbackPeriod;

		if (_filledCount < LookbackPeriod)
			_filledCount++;

		if (_filledCount < LookbackPeriod)
			return;

		var avgWidth = 0m;
		var sumSq = 0m;

		for (var i = 0; i < LookbackPeriod; i++)
			avgWidth += _cloudWidthHistory[i];

		avgWidth /= LookbackPeriod;

		for (var i = 0; i < LookbackPeriod; i++)
		{
			var diff = _cloudWidthHistory[i] - avgWidth;
			sumSq += diff * diff;
		}

		var stdWidth = (decimal)Math.Sqrt((double)(sumSq / LookbackPeriod));

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var narrowThreshold = avgWidth - stdWidth * DeviationMultiplier;
		var wideThreshold = avgWidth + stdWidth * DeviationMultiplier;
		var upperCloud = Math.Max(senkouA, senkouB);
		var lowerCloud = Math.Min(senkouA, senkouB);
		var priceAboveCloud = candle.ClosePrice > upperCloud;
		var priceBelowCloud = candle.ClosePrice < lowerCloud;

		if (Position == 0)
		{
			if (cloudWidth < narrowThreshold)
			{
				if (priceAboveCloud)
				{
					BuyMarket();
					_cooldown = CooldownBars;
				}
				else if (priceBelowCloud)
				{
					SellMarket();
					_cooldown = CooldownBars;
				}
			}
			else if (cloudWidth > wideThreshold)
			{
				if (priceBelowCloud)
				{
					SellMarket();
					_cooldown = CooldownBars;
				}
				else if (priceAboveCloud)
				{
					BuyMarket();
					_cooldown = CooldownBars;
				}
			}
		}
		else if (Position > 0 && cloudWidth >= avgWidth)
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && cloudWidth <= avgWidth)
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
