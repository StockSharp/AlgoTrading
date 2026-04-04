using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ColorJFatl Digit TM Plus strategy converted from MQL.
/// Trades slope changes of a Jurik smoothed FATL digital filter and
/// optionally applies time based and price based exits.
/// </summary>
public class ColorJfatlDigitTmPlusStrategy : Strategy
{
	private static readonly decimal[] FatlCoefficients =
	[
		0.4360409450m, 0.3658689069m, 0.2460452079m, 0.1104506886m,
		-0.0054034585m, -0.0760367731m, -0.0933058722m, -0.0670110374m,
		-0.0190795053m, 0.0259609206m, 0.0502044896m, 0.0477818607m,
		0.0249252327m, -0.0047706151m, -0.0272432537m, -0.0338917071m,
		-0.0244141482m, -0.0055774838m, 0.0128149838m, 0.0226522218m,
		0.0208778257m, 0.0100299086m, -0.0036771622m, -0.0136744850m,
		-0.0160483392m, -0.0108597376m, -0.0016060704m, 0.0069480557m,
		0.0110573605m, 0.0095711419m, 0.0040444064m, -0.0023824623m,
		-0.0067093714m, -0.0072003400m, -0.0047717710m, 0.0005541115m,
		0.0007860160m, 0.0130129076m, 0.0040364019m,
	];

	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExits;
	private readonly StrategyParam<bool> _enableSellExits;
	private readonly StrategyParam<bool> _useTimeExit;
	private readonly StrategyParam<int> _holdingMinutes;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<AppliedPrices> _appliedPrice;
	private readonly StrategyParam<int> _digitRounding;
	private readonly StrategyParam<int> _signalBar;

	private ExponentialMovingAverage _jma;
	private readonly List<decimal> _priceBuffer = new();
	private readonly List<int> _colorHistory = new();

	private decimal? _previousLine;
	private DateTimeOffset? _entryTime;

	/// <summary>
	/// Trading volume used for market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables long entries when an up-slope appears.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Enables short entries when a down-slope appears.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Enables long exit when the filter turns bearish.
	/// </summary>
	public bool EnableBuyExits
	{
		get => _enableBuyExits.Value;
		set => _enableBuyExits.Value = value;
	}

	/// <summary>
	/// Enables short exit when the filter turns bullish.
	/// </summary>
	public bool EnableSellExits
	{
		get => _enableSellExits.Value;
		set => _enableSellExits.Value = value;
	}

	/// <summary>
	/// Enables the time based exit.
	/// </summary>
	public bool UseTimeExit
	{
		get => _useTimeExit.Value;
		set => _useTimeExit.Value = value;
	}

	/// <summary>
	/// Holding period in minutes for the time based exit.
	/// </summary>
	public int HoldingMinutes
	{
		get => _holdingMinutes.Value;
		set => _holdingMinutes.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Jurik smoothing length applied to the digital filter.
	/// </summary>
	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	/// <summary>
	/// Applied price option for the digital filter input.
	/// </summary>
	public AppliedPrices AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Precision multiplier used for rounding indicator values.
	/// </summary>
	public int DigitRounding
	{
		get => _digitRounding.Value;
		set => _digitRounding.Value = value;
	}

	/// <summary>
	/// Number of finished bars to look back for signals.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Applied price options matching the original MQL enumeration.
	/// </summary>
	public enum AppliedPrices
	{
		Close = 1,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		AverageOC,
		AverageOHLC,
		TrendFollow1,
		TrendFollow2,
		Demark,
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ColorJfatlDigitTmPlusStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss distance in price steps (0=disabled)", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit distance in price steps (0=disabled)", "Risk");

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
			.SetDisplay("Enable Long Entry", "Allow opening long positions", "Signals");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
			.SetDisplay("Enable Short Entry", "Allow opening short positions", "Signals");

		_enableBuyExits = Param(nameof(EnableBuyExits), true)
			.SetDisplay("Enable Long Exit", "Allow closing long positions", "Signals");

		_enableSellExits = Param(nameof(EnableSellExits), true)
			.SetDisplay("Enable Short Exit", "Allow closing short positions", "Signals");

		_useTimeExit = Param(nameof(UseTimeExit), false)
			.SetDisplay("Use Time Exit", "Enable time based exit", "Exits");

		_holdingMinutes = Param(nameof(HoldingMinutes), 240)
			.SetGreaterThanZero()
			.SetDisplay("Holding Minutes", "Exit after holding for N minutes", "Exits");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "General");

		_jmaLength = Param(nameof(JmaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "Jurik smoothing length", "Indicator")
			.SetOptimize(3, 30, 1);

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPrices.Close)
			.SetDisplay("Applied Price", "Price source for the filter", "Indicator");

		_digitRounding = Param(nameof(DigitRounding), 0)
			.SetNotNegative()
			.SetDisplay("Digit Rounding", "Rounding precision multiplier", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Signal Bar", "Number of finished bars used for signals", "Indicator");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_jma = null;
		_priceBuffer.Clear();
		_colorHistory.Clear();
		_previousLine = null;
		_entryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		Volume = TradeVolume;
		_jma = new ExponentialMovingAverage { Length = JmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var priceStep = Security?.PriceStep ?? 0m;
		Unit takeProfitUnit = null;
		Unit stopLossUnit = null;

		if (TakeProfitPoints > 0 && priceStep > 0m)
			takeProfitUnit = new Unit(TakeProfitPoints * priceStep, UnitTypes.Absolute);

		if (StopLossPoints > 0 && priceStep > 0m)
			stopLossUnit = new Unit(StopLossPoints * priceStep, UnitTypes.Absolute);

		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = GetAppliedPrice(candle);
		_priceBuffer.Add(price);
		if (_priceBuffer.Count > FatlCoefficients.Length)
			_priceBuffer.RemoveAt(0);

		if (_priceBuffer.Count < FatlCoefficients.Length)
			return;

		var fatl = 0m;
		for (var i = 0; i < FatlCoefficients.Length; i++)
			fatl += FatlCoefficients[i] * _priceBuffer[_priceBuffer.Count - 1 - i];

		var jmaValue = _jma.Process(new DecimalIndicatorValue(_jma, fatl, candle.OpenTime) { IsFinal = true });
		if (!_jma.IsFormed)
			return;

		var roundedLine = RoundToStep(jmaValue.ToDecimal(), GetRoundingStep());

		var color = 1;
		if (_previousLine.HasValue)
		{
			var diff = roundedLine - _previousLine.Value;
			if (diff > 0m)
				color = 2;
			else if (diff < 0m)
				color = 0;
			else if (_colorHistory.Count > 0)
				color = _colorHistory[0];
		}

		_previousLine = roundedLine;
		_colorHistory.Insert(0, color);
		if (_colorHistory.Count > 100)
			_colorHistory.RemoveAt(_colorHistory.Count - 1);

		if (_colorHistory.Count <= SignalBar)
			return;

		var currentColor = _colorHistory[SignalBar - 1];
		var previousColor = _colorHistory[SignalBar];

		// Time-based exit
		if (UseTimeExit && Position != 0 && _entryTime is not null && HoldingMinutes > 0)
		{
			var elapsed = candle.CloseTime - _entryTime.Value;
			if (elapsed >= TimeSpan.FromMinutes(HoldingMinutes))
			{
				if (Position > 0)
					SellMarket();
				else if (Position < 0)
					BuyMarket();

				_entryTime = null;
			}
		}

		var buyOpenSignal = EnableBuyEntries && currentColor == 2 && previousColor != 2;
		var sellCloseSignal = EnableSellExits && currentColor == 2;
		var sellOpenSignal = EnableSellEntries && currentColor == 0 && previousColor != 0;
		var buyCloseSignal = EnableBuyExits && currentColor == 0;

		if (buyCloseSignal && Position > 0)
		{
			SellMarket();
			_entryTime = null;
		}

		if (sellCloseSignal && Position < 0)
		{
			BuyMarket();
			_entryTime = null;
		}

		if (buyOpenSignal && Position == 0)
		{
			BuyMarket();
			_entryTime = candle.CloseTime;
		}

		if (sellOpenSignal && Position == 0)
		{
			SellMarket();
			_entryTime = candle.CloseTime;
		}
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
		=> AppliedPrice switch
		{
			AppliedPrices.Close => candle.ClosePrice,
			AppliedPrices.Open => candle.OpenPrice,
			AppliedPrices.High => candle.HighPrice,
			AppliedPrices.Low => candle.LowPrice,
			AppliedPrices.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrices.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPrices.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrices.AverageOC => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrices.AverageOHLC => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrices.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
				? candle.HighPrice
				: candle.ClosePrice < candle.OpenPrice
					? candle.LowPrice
					: candle.ClosePrice,
			AppliedPrices.TrendFollow2 => candle.ClosePrice > candle.OpenPrice
				? (candle.HighPrice + candle.ClosePrice) / 2m
				: candle.ClosePrice < candle.OpenPrice
					? (candle.LowPrice + candle.ClosePrice) / 2m
					: candle.ClosePrice,
			AppliedPrices.Demark => GetDemarkPrice(candle),
			_ => candle.ClosePrice,
		};

	private static decimal GetDemarkPrice(ICandleMessage candle)
	{
		var res = candle.HighPrice + candle.LowPrice + candle.ClosePrice;
		if (candle.ClosePrice < candle.OpenPrice)
			res = (res + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
			res = (res + candle.HighPrice) / 2m;
		else
			res = (res + candle.ClosePrice) / 2m;

		return ((res - candle.LowPrice) + (res - candle.HighPrice)) / 2m;
	}

	private decimal GetRoundingStep()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var multiplier = (decimal)Math.Pow(10, DigitRounding);
		return step * multiplier;
	}

	private static decimal RoundToStep(decimal value, decimal step)
	{
		if (step <= 0m)
			return value;

		return Math.Round(value / step, MidpointRounding.AwayFromZero) * step;
	}
}
