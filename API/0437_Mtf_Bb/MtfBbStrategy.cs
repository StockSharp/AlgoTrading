namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Multi-Timeframe Bollinger Bands Strategy
/// </summary>
public class MtfBbStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<DataType> _mtfCandleTypeParam;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<bool> _useMaFilter;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<bool> _showLong;
	private readonly StrategyParam<bool> _showShort;
	private readonly StrategyParam<bool> _useSL;
	private readonly StrategyParam<decimal> _slPercent;

	private BollingerBands _bollinger;
	private BollingerBands _mtfBollinger;
	private ExponentialMovingAverage _ma;

	public MtfBbStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Main timeframe", "General");

		_mtfCandleTypeParam = Param(nameof(MtfCandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("MTF Candle type", "Multi-timeframe for BB", "MTF Bollinger Bands");

		_bbLength = Param(nameof(BBLength), 20)
			.SetDisplay("BB Length", "Bollinger Bands period", "MTF Bollinger Bands");

		_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
			.SetDisplay("BB StdDev", "Standard deviation multiplier", "MTF Bollinger Bands");

		_useMaFilter = Param(nameof(UseMaFilter), false)
			.SetDisplay("Use MA Filter", "Enable Moving Average filter", "MTF Moving Average Filter");

		_maLength = Param(nameof(MaLength), 200)
			.SetDisplay("MA Length", "Moving Average period", "MTF Moving Average Filter");

		_showLong = Param(nameof(ShowLong), true)
			.SetDisplay("Long entries", "Enable long positions", "Strategy");

		_showShort = Param(nameof(ShowShort), false)
			.SetDisplay("Short entries", "Enable short positions", "Strategy");

		_useSL = Param(nameof(UseSL), true)
			.SetDisplay("Enable SL", "Enable Stop Loss", "Stop Loss");

		_slPercent = Param(nameof(SLPercent), 2m)
			.SetDisplay("SL Percent", "Stop loss percentage", "Stop Loss");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public DataType MtfCandleType
	{
		get => _mtfCandleTypeParam.Value;
		set => _mtfCandleTypeParam.Value = value;
	}

	public int BBLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	public decimal BBMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	public bool UseMaFilter
	{
		get => _useMaFilter.Value;
		set => _useMaFilter.Value = value;
	}

	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public bool ShowLong
	{
		get => _showLong.Value;
		set => _showLong.Value = value;
	}

	public bool ShowShort
	{
		get => _showShort.Value;
		set => _showShort.Value = value;
	}

	public bool UseSL
	{
		get => _useSL.Value;
		set => _useSL.Value = value;
	}

	public decimal SLPercent
	{
		get => _slPercent.Value;
		set => _slPercent.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_bollinger = new BollingerBands
		{
			Length = BBLength,
			Width = BBMultiplier
		};

		_mtfBollinger = new BollingerBands
		{
			Length = BBLength,
			Width = BBMultiplier
		};

		if (UseMaFilter)
		{
			_ma = new ExponentialMovingAverage { Length = MaLength };
		}

		// Subscribe to main timeframe
		var subscription = SubscribeCandles(CandleType);
		
		// Subscribe to MTF candles
		var mtfSubscription = SubscribeCandles(MtfCandleType);
		
		// Process MTF candles for indicator
		mtfSubscription
			.BindEx(_mtfBollinger, OnProcess)
			.Start();

		// Process main timeframe
		if (UseMaFilter)
		{
			mtfSubscription
				.WhenCandlesFinished()
				.Do(candle => _ma.Process(candle))
				.Apply(this);

			subscription
				.Bind(_bollinger, OnProcess)
				.Start();
		}
		else
		{
			subscription
				.Bind(_bollinger, OnProcess)
				.Start();
		}

		mtfSubscription.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			if (UseMaFilter && _ma != null)
				DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}

		// Enable protection
		if (UseSL)
		{
			StartProtection(null, new Unit(SLPercent, UnitTypes.Percent));
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue bbValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_bollinger.IsFormed || !_mtfBollinger.IsFormed)
			return;

		if (UseMaFilter && (_ma == null || !_ma.IsFormed))
			return;

		// Get current timeframe BB values
		var bb = bbValue.GetValue<BollingerBand>();
		var upper = bb.UpperBand;
		var lower = bb.LowerBand;

		// Get MTF BB values
		var mtfBb = _mtfBollinger.GetCurrentValue<BollingerBand>();
		var mtfUpper = mtfBb.UpperBand;
		var mtfLower = mtfBb.LowerBand;

		// MA filter
		var buyMaFilter = true;
		var sellMaFilter = true;
		
		if (UseMaFilter && _ma != null)
		{
			var maValue = _ma.GetCurrentValue();
			buyMaFilter = candle.ClosePrice > maValue;
			sellMaFilter = candle.ClosePrice < maValue;
		}

		// Entry conditions
		var buy = candle.ClosePrice < mtfLower && buyMaFilter;
		var sell = candle.ClosePrice > mtfUpper && sellMaFilter;

		// Execute trades
		if (ShowLong && buy && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (ShowLong && Position > 0 && candle.ClosePrice > upper)
		{
			ClosePosition();
		}

		if (ShowShort && sell && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (ShowShort && Position < 0 && candle.ClosePrice < lower)
		{
			ClosePosition();
		}
	}
}