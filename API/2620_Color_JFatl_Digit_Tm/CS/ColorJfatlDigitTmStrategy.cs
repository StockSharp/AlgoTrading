using System;
using System.Collections.Generic;
using System.Reflection;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Color JFATL Digit indicator with optional trading window and money management controls.
/// Detects color transitions produced by the smoothed FATL curve and opens or closes positions accordingly.
/// </summary>
public class ColorJfatlDigitTmStrategy : Strategy
{
	private static readonly decimal[] FatlCoefficients =
	[
		0.4360409450m,
		0.3658689069m,
		0.2460452079m,
		0.1104506886m,
		-0.0054034585m,
		-0.0760367731m,
		-0.0933058722m,
		-0.0670110374m,
		-0.0190795053m,
		0.0259609206m,
		0.0502044896m,
		0.0477818607m,
		0.0249252327m,
		-0.0047706151m,
		-0.0272432537m,
		-0.0338917071m,
		-0.0244141482m,
		-0.0055774838m,
		0.0128149838m,
		0.0226522218m,
		0.0208778257m,
		0.0100299086m,
		-0.0036771622m,
		-0.0136744850m,
		-0.0160483392m,
		-0.0108597376m,
		-0.0016060704m,
		0.0069480557m,
		0.0110573605m,
		0.0095711419m,
		0.0040444064m,
		-0.0023824623m,
		-0.0067093714m,
		-0.0072003400m,
		-0.0047717710m,
		0.0005541115m,
		0.0007860160m,
		0.0130129076m,
		0.0040364019m,
	];

	private static readonly PropertyInfo? JurikPhaseProperty = typeof(JurikMovingAverage).GetProperty("Phase");

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _enableTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<int> _jmaPhase;
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<int> _digitRounding;
	private readonly StrategyParam<int> _signalBar;

	private readonly JurikMovingAverage _jma = new();
	private readonly List<decimal> _priceBuffer = new();
	private readonly List<int> _colorHistory = new();

	private decimal? _previousLine;
	private DateTimeOffset _nextBuyTime = DateTimeOffset.MinValue;
	private DateTimeOffset _nextSellTime = DateTimeOffset.MinValue;

	/// <summary>
	/// Trading volume per order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Enable or disable the session time filter.
	/// </summary>
	public bool EnableTimeFilter
	{
		get => _enableTimeFilter.Value;
		set => _enableTimeFilter.Value = value;
	}

	/// <summary>
	/// Session start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session start minute.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Session end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Session end minute.
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool BuyOpenEnabled
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool SellOpenEnabled
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Allow long exits.
	/// </summary>
	public bool BuyCloseEnabled
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Allow short exits.
	/// </summary>
	public bool SellCloseEnabled
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculation.
	/// </summary>
	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	/// <summary>
	/// Jurik moving average length.
	/// </summary>
	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	/// <summary>
	/// Jurik moving average phase parameter.
	/// </summary>
	public int JmaPhase
	{
		get => _jmaPhase.Value;
		set => _jmaPhase.Value = value;
	}

	/// <summary>
	/// Applied price mode.
	/// </summary>
	public AppliedPrice AppliedPriceMode
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
	/// Number of bars to shift when evaluating color transitions.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ColorJfatlDigitTmStrategy"/> class.
	/// </summary>
	public ColorJfatlDigitTmStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Trade volume per position", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_enableTimeFilter = Param(nameof(EnableTimeFilter), true)
			.SetDisplay("Enable Time Filter", "Restrict trading to session hours", "Session");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Session start hour", "Session");

		_startMinute = Param(nameof(StartMinute), 0)
			.SetDisplay("Start Minute", "Session start minute", "Session");

		_endHour = Param(nameof(EndHour), 23)
			.SetDisplay("End Hour", "Session end hour", "Session");

		_endMinute = Param(nameof(EndMinute), 59)
			.SetDisplay("End Minute", "Session end minute", "Session");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (points)", "Take profit in points", "Risk");

		_buyOpen = Param(nameof(BuyOpenEnabled), true)
			.SetDisplay("Enable Buy Open", "Allow opening long positions", "Signals");

		_sellOpen = Param(nameof(SellOpenEnabled), true)
			.SetDisplay("Enable Sell Open", "Allow opening short positions", "Signals");

		_buyClose = Param(nameof(BuyCloseEnabled), true)
			.SetDisplay("Enable Buy Close", "Allow closing long positions", "Signals");

		_sellClose = Param(nameof(SellCloseEnabled), true)
			.SetDisplay("Enable Sell Close", "Allow closing short positions", "Signals");

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Signal Candle Type", "Timeframe used for indicator", "Indicator");

		_jmaLength = Param(nameof(JmaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "Period for Jurik moving average", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(3, 30, 1);

		_jmaPhase = Param(nameof(JmaPhase), -100)
			.SetDisplay("JMA Phase", "Phase shift for Jurik moving average", "Indicator");

		_appliedPrice = Param(nameof(AppliedPriceMode), AppliedPrice.Close)
			.SetDisplay("Applied Price", "Price source for calculations", "Indicator");

		_digitRounding = Param(nameof(DigitRounding), 2)
			.SetGreaterOrEqualZero()
			.SetDisplay("Digit Rounding", "Rounding precision multiplier", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Signal Bar", "Shift for analyzing colors", "Signals");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, SignalCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_jma.Reset();
		_priceBuffer.Clear();
		_colorHistory.Clear();
		_previousLine = null;
		_nextBuyTime = DateTimeOffset.MinValue;
		_nextSellTime = DateTimeOffset.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;
		ConfigureJma();

		var subscription = SubscribeCandles(SignalCandleType);
		subscription.Bind(ProcessCandle).Start();

		var priceStep = Security?.PriceStep ?? 0m;
		Unit? takeProfitUnit = null;
		Unit? stopLossUnit = null;

		if (TakeProfitPoints > 0 && priceStep > 0m)
			takeProfitUnit = new Unit(TakeProfitPoints * priceStep, UnitTypes.Price);

		if (StopLossPoints > 0 && priceStep > 0m)
			stopLossUnit = new Unit(StopLossPoints * priceStep, UnitTypes.Price);

		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit);
	}

	private void ConfigureJma()
	{
		_jma.Length = JmaLength;
		JurikPhaseProperty?.SetValue(_jma, JmaPhase);
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
		{
			var value = _priceBuffer[_priceBuffer.Count - 1 - i];
			fatl += FatlCoefficients[i] * value;
		}

		var jmaValue = _jma.Process(new DecimalIndicatorValue(_jma, fatl, candle.OpenTime));
		if (!jmaValue.IsFinal || !_jma.IsFormed)
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
		var now = candle.CloseTime;

		var inSession = !EnableTimeFilter || IsWithinTradingWindow(now);
		if (EnableTimeFilter && !inSession)
		{
			ClosePositions();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var buyOpenSignal = BuyOpenEnabled && previousColor == 2 && currentColor < 2;
		var sellCloseSignal = SellCloseEnabled && previousColor == 2;
		var sellOpenSignal = SellOpenEnabled && previousColor == 0 && currentColor > 0;
		var buyCloseSignal = BuyCloseEnabled && previousColor == 0;

		if (buyCloseSignal && Position > 0)
			SellMarket(Position);

		if (sellCloseSignal && Position < 0)
			BuyMarket(-Position);

		if (buyOpenSignal && Position == 0 && now >= _nextBuyTime)
		{
			BuyMarket(OrderVolume);
			_nextBuyTime = now;
		}

		if (sellOpenSignal && Position == 0 && now >= _nextSellTime)
		{
			SellMarket(OrderVolume);
			_nextSellTime = now;
		}
	}

	private void ClosePositions()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPriceMode switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice
				? candle.HighPrice
				: candle.ClosePrice < candle.OpenPrice
					? candle.LowPrice
					: candle.ClosePrice,
			AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
				? (candle.HighPrice + candle.ClosePrice) / 2m
				: candle.ClosePrice < candle.OpenPrice
					? (candle.LowPrice + candle.ClosePrice) / 2m
					: candle.ClosePrice,
			AppliedPrice.Demark => CalculateDemarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal CalculateDemarkPrice(ICandleMessage candle)
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

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var hour = time.Hour;
		var minute = time.Minute;

		if (StartHour < EndHour)
		{
			if (hour == StartHour && minute >= StartMinute)
				return true;
			if (hour > StartHour && hour < EndHour)
				return true;
			if (hour > StartHour && hour == EndHour && minute < EndMinute)
				return true;
		}
		else if (StartHour == EndHour)
		{
			if (hour == StartHour && minute >= StartMinute && minute < EndMinute)
				return true;
		}
		else
		{
			if (hour > StartHour || (hour == StartHour && minute >= StartMinute))
				return true;
			if (hour < EndHour)
				return true;
			if (hour == EndHour && minute < EndMinute)
				return true;
		}

		return false;
	}

	/// <summary>
	/// Applied price options replicated from the original MQL implementation.
	/// </summary>
	public enum AppliedPrice
	{
		Close = 1,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		Simple,
		Quarter,
		TrendFollow0,
		TrendFollow1,
		Demark,
	}
}
