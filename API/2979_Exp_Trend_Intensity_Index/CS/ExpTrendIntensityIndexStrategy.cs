using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend Intensity Index strategy converted from MetaTrader expert.
/// </summary>
public class ExpTrendIntensityIndexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _priceMaLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<AppliedPriceOption> _appliedPrice;
	private readonly StrategyParam<MovingAverageMethod> _priceMaMethod;
	private readonly StrategyParam<MovingAverageMethod> _smoothingMethod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExits;
	private readonly StrategyParam<bool> _enableSellExits;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private IIndicator _priceMa;
	private IIndicator _positiveMa;
	private IIndicator _negativeMa;

	private readonly List<int> _colorHistory = new();

	/// <summary>
	/// Initializes a new instance of <see cref="ExpTrendIntensityIndexStrategy"/>.
	/// </summary>
	public ExpTrendIntensityIndexStrategy()
	{
		Volume = 1;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Indicator Timeframe", "Timeframe used to evaluate Trend Intensity Index", "General");

		_priceMaMethod = Param(nameof(PriceMaMethod), MovingAverageMethod.Simple)
		.SetDisplay("Price MA Method", "Smoothing method applied to the base price", "Indicators");

		_priceMaLength = Param(nameof(PriceMaLength), 60)
		.SetDisplay("Price MA Length", "Number of bars for the base smoothing", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 120, 10);

		_smoothingMethod = Param(nameof(SmoothingMethod), MovingAverageMethod.Simple)
		.SetDisplay("Signal MA Method", "Smoothing method for positive and negative flows", "Indicators");

		_smoothingLength = Param(nameof(SmoothingLength), 30)
		.SetDisplay("Signal MA Length", "Number of bars for the flow smoothing", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 90, 10);

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceOption.Close)
		.SetDisplay("Applied Price", "Price source used in calculations", "Indicators");

		_highLevel = Param(nameof(HighLevel), 80m)
		.SetDisplay("High Level", "Upper threshold for Trend Intensity Index", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(60m, 90m, 5m);

		_lowLevel = Param(nameof(LowLevel), 20m)
		.SetDisplay("Low Level", "Lower threshold for Trend Intensity Index", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Number of closed bars to look back for confirmation", "Signals");

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
		.SetDisplay("Enable Buy Entries", "Allow opening long positions", "Trading");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
		.SetDisplay("Enable Sell Entries", "Allow opening short positions", "Trading");

		_enableBuyExits = Param(nameof(EnableBuyExits), true)
		.SetDisplay("Enable Buy Exits", "Allow closing long positions on opposite signals", "Trading");

		_enableSellExits = Param(nameof(EnableSellExits), true)
		.SetDisplay("Enable Sell Exits", "Allow closing short positions on opposite signals", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
		.SetDisplay("Stop Loss (price)", "Protective stop distance expressed in price units", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
		.SetDisplay("Take Profit (price)", "Protective target distance expressed in price units", "Risk");
	}

	/// <summary>
	/// Timeframe used for the indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average method applied to the base price stream.
	/// </summary>
	public MovingAverageMethod PriceMaMethod
	{
		get => _priceMaMethod.Value;
		set => _priceMaMethod.Value = value;
	}

	/// <summary>
	/// Number of bars used for the base price smoothing.
	/// </summary>
	public int PriceMaLength
	{
		get => _priceMaLength.Value;
		set => _priceMaLength.Value = value;
	}

	/// <summary>
	/// Moving average method applied to positive and negative flows.
	/// </summary>
	public MovingAverageMethod SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Number of bars used for flow smoothing.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Price source used in Trend Intensity Index calculation.
	/// </summary>
	public AppliedPriceOption AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Upper threshold that marks a bullish trend zone.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold that marks a bearish trend zone.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Number of completed bars to look back for signal confirmation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enables opening of long positions.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Enables opening of short positions.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Enables automatic exit from long positions.
	/// </summary>
	public bool EnableBuyExits
	{
		get => _enableBuyExits.Value;
		set => _enableBuyExits.Value = value;
	}

	/// <summary>
	/// Enables automatic exit from short positions.
	/// </summary>
	public bool EnableSellExits
	{
		get => _enableSellExits.Value;
		set => _enableSellExits.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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

		_priceMa = null;
		_positiveMa = null;
		_negativeMa = null;
		_colorHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceMa = CreateMovingAverage(PriceMaMethod, PriceMaLength);
		_positiveMa = CreateMovingAverage(SmoothingMethod, SmoothingLength);
		_negativeMa = CreateMovingAverage(SmoothingMethod, SmoothingLength);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		ConfigureProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ConfigureProtection()
	{
		Unit? takeProfitUnit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Price) : null;
		Unit? stopLossUnit = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Price) : null;

		if (takeProfitUnit != null || stopLossUnit != null)
		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_priceMa == null || _positiveMa == null || _negativeMa == null)
		return;

		var price = GetAppliedPrice(candle, AppliedPrice);

		var maValue = _priceMa.Process(price, candle.OpenTime, true);
		if (!maValue.IsFinal)
		return;

		var baseMa = maValue.ToDecimal();
		var diff = price - baseMa;
		var positive = diff > 0 ? diff : 0m;
		var negative = diff < 0 ? -diff : 0m;

		var posValue = _positiveMa.Process(positive, candle.OpenTime, true);
		var negValue = _negativeMa.Process(negative, candle.OpenTime, true);

		if (!posValue.IsFinal || !negValue.IsFinal)
		return;

		var pos = posValue.ToDecimal();
		var neg = negValue.ToDecimal();
		var denominator = pos + neg;
		var tii = denominator > 0 ? 100m * pos / denominator : 100m;

		UpdateColorHistory(tii);

		var signalBar = Math.Max(0, SignalBar);
		var requiredCount = signalBar + 2;
		if (_colorHistory.Count < requiredCount)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var signalColor = _colorHistory[signalBar];
		var olderColor = _colorHistory[signalBar + 1];

		var shouldCloseLong = EnableBuyExits && olderColor == 2 && Position > 0;
		var shouldCloseShort = EnableSellExits && olderColor == 0 && Position < 0;
		var shouldOpenLong = EnableBuyEntries && olderColor == 0 && signalColor != 0 && Position <= 0;
		var shouldOpenShort = EnableSellEntries && olderColor == 2 && signalColor != 2 && Position >= 0;

		if (shouldCloseLong || shouldCloseShort || shouldOpenLong || shouldOpenShort)
		CancelActiveOrders();

		if (shouldCloseLong && Position > 0)
		SellMarket(Position);

		if (shouldCloseShort && Position < 0)
		BuyMarket(-Position);

		if (shouldOpenLong && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0)
			BuyMarket(volume);
		}
		else if (shouldOpenShort && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0)
			SellMarket(volume);
		}
	}

	private void UpdateColorHistory(decimal tii)
	{
		var color = tii > HighLevel ? 0 : tii < LowLevel ? 2 : 1;

		_colorHistory.Insert(0, color);

		var maxSize = Math.Max(2, SignalBar + 2);
		while (_colorHistory.Count > maxSize)
		_colorHistory.RemoveAt(_colorHistory.Count - 1);
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceOption priceType)
	{
		var open = candle.OpenPrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		return priceType switch
		{
			AppliedPriceOption.Open => open,
			AppliedPriceOption.High => high,
			AppliedPriceOption.Low => low,
			AppliedPriceOption.Median => (high + low) / 2m,
			AppliedPriceOption.Typical => (close + high + low) / 3m,
			AppliedPriceOption.Weighted => (2m * close + high + low) / 4m,
			AppliedPriceOption.Simple => (open + close) / 2m,
			AppliedPriceOption.Quarted => (open + close + high + low) / 4m,
			AppliedPriceOption.TrendFollow0 => close > open ? high : close < open ? low : close,
			AppliedPriceOption.TrendFollow1 => close > open ? (high + close) / 2m : close < open ? (low + close) / 2m : close,
			AppliedPriceOption.Demark => GetDemarkPrice(open, high, low, close),
			_ => close,
		};
	}

	private static decimal GetDemarkPrice(decimal open, decimal high, decimal low, decimal close)
	{
		var res = high + low + close;

		if (close < open)
		res = (res + low) / 2m;
		else if (close > open)
		res = (res + high) / 2m;
		else
		res = (res + close) / 2m;

		return ((res - low) + (res - high)) / 2m;
	}

	private static IIndicator CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Price sources available for Trend Intensity Index.
	/// </summary>
	public enum AppliedPriceOption
	{
		/// <summary>
		/// Close price.
		/// </summary>
		Close = 0,

		/// <summary>
		/// Open price.
		/// </summary>
		Open,

		/// <summary>
		/// High price.
		/// </summary>
		High,

		/// <summary>
		/// Low price.
		/// </summary>
		Low,

		/// <summary>
		/// Median price (high + low) / 2.
		/// </summary>
		Median,

		/// <summary>
		/// Typical price (high + low + close) / 3.
		/// </summary>
		Typical,

		/// <summary>
		/// Weighted close (2 * close + high + low) / 4.
		/// </summary>
		Weighted,

		/// <summary>
		/// Simple average of open and close.
		/// </summary>
		Simple,

		/// <summary>
		/// Quarted price (open + close + high + low) / 4.
		/// </summary>
		Quarted,

		/// <summary>
		/// Trend follow price using candle extremes.
		/// </summary>
		TrendFollow0,

		/// <summary>
		/// Trend follow price averaged with close.
		/// </summary>
		TrendFollow1,

		/// <summary>
		/// Demark price calculation.
		/// </summary>
		Demark
	}

	/// <summary>
	/// Moving average methods used by the strategy.
	/// </summary>
	public enum MovingAverageMethod
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Weighted
	}
}
