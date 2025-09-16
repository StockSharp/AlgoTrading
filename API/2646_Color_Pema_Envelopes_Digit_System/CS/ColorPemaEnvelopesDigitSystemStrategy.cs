using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Color PEMA Envelopes Digit indicator.
/// A long position is opened when price breaks above the upper envelope and then returns inside it,
/// while a short position is opened on a mirror setup around the lower envelope.
/// Previous color codes from the original indicator are used to detect these transitions.
/// </summary>
public class ColorPemaEnvelopesDigitSystemStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _emaLength;
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<decimal> _deviationPercent;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<decimal> _priceShift;
	private readonly StrategyParam<int> _digit;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _tradeVolume;

	private readonly PemaIndicator _pema = new();

	private readonly Queue<decimal> _upperHistory = new();
	private readonly Queue<decimal> _lowerHistory = new();
	private readonly List<int> _colorHistory = new();

	/// <summary>
	/// Initializes a new instance of <see cref="ColorPemaEnvelopesDigitSystemStrategy"/>.
	/// </summary>
	public ColorPemaEnvelopesDigitSystemStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for calculations", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Order volume used for entries", "Trading");

		_emaLength = Param(nameof(EmaLength), 50.01m)
		.SetGreaterThanZero()
		.SetDisplay("PEMA Length", "Length of each EMA stage in PEMA", "Indicator");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPrice.Close)
		.SetDisplay("Applied Price", "Price source passed to PEMA", "Indicator");

		_deviationPercent = Param(nameof(DeviationPercent), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Envelope Deviation", "Percentage width of envelopes", "Indicator");

		_shift = Param(nameof(Shift), 1)
		.SetRange(0, 10)
		.SetDisplay("Shift", "Bars used to offset envelope comparison", "Indicator");

		_priceShift = Param(nameof(PriceShift), 0m)
		.SetDisplay("Price Shift", "Additional absolute shift applied to envelopes", "Indicator");

		_digit = Param(nameof(Digit), 2)
		.SetRange(0, 8)
		.SetDisplay("Rounding Digits", "Extra precision digits for rounding", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetRange(1, 10)
		.SetDisplay("Signal Bar", "How many completed bars back to check colors", "Logic");

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
		.SetDisplay("Allow Buy Open", "Enable new long entries", "Logic");

		_allowSellOpen = Param(nameof(AllowSellOpen), true)
		.SetDisplay("Allow Sell Open", "Enable new short entries", "Logic");

		_allowBuyClose = Param(nameof(AllowBuyClose), true)
		.SetDisplay("Allow Buy Close", "Allow closing long positions on opposite signal", "Logic");

		_allowSellClose = Param(nameof(AllowSellClose), true)
		.SetDisplay("Allow Sell Close", "Allow closing short positions on opposite signal", "Logic");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetRange(0m, 100000m)
		.SetDisplay("Stop Loss Points", "Distance for protective stop", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetRange(0m, 100000m)
		.SetDisplay("Take Profit Points", "Distance for profit target", "Risk");
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Length of each EMA layer inside PEMA.
	/// </summary>
	public decimal EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Price source passed to PEMA.
	/// </summary>
	public AppliedPrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Percentage width of the envelopes around PEMA.
	/// </summary>
	public decimal DeviationPercent
	{
		get => _deviationPercent.Value;
		set => _deviationPercent.Value = value;
	}

	/// <summary>
	/// Bars used to offset envelope comparison.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	/// <summary>
	/// Additional absolute shift applied to envelopes.
	/// </summary>
	public decimal PriceShift
	{
		get => _priceShift.Value;
		set => _priceShift.Value = value;
	}

	/// <summary>
	/// Extra precision digits for rounding PEMA.
	/// </summary>
	public int Digit
	{
		get => _digit.Value;
		set => _digit.Value = value;
	}

	/// <summary>
	/// Completed bars back to inspect for signals.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enable opening of long positions.
	/// </summary>
	public bool AllowBuyOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	/// <summary>
	/// Enable opening of short positions.
	/// </summary>
	public bool AllowSellOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing of long positions on opposite signal.
	/// </summary>
	public bool AllowBuyClose
	{
		get => _allowBuyClose.Value;
		set => _allowBuyClose.Value = value;
	}

	/// <summary>
	/// Allow closing of short positions on opposite signal.
	/// </summary>
	public bool AllowSellClose
	{
		get => _allowSellClose.Value;
		set => _allowSellClose.Value = value;
	}

	/// <summary>
	/// Distance to the protective stop in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Distance to the profit target in price points.
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

		_pema.Reset();
		_upperHistory.Clear();
		_lowerHistory.Clear();
		_colorHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pema.Length = EmaLength;
		_pema.Digit = Digit;
		_pema.PriceStep = Security?.PriceStep ?? 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _pema);
			DrawOwnTrades(area);
		}

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
		takeProfit: new Unit(TakeProfitPoints * step, UnitTypes.Point),
		stopLoss: new Unit(StopLossPoints * step, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		var price = GetAppliedPrice(candle);

		// Calculate PEMA base value for the current candle.
		var pemaValue = _pema.Process(new DecimalIndicatorValue(_pema, price, candle.OpenTime));
		if (!pemaValue.IsFinal)
		{
			return;
		}

		var pema = pemaValue.GetValue<decimal>();

		var upperCurrent = (1m + DeviationPercent / 100m) * pema + PriceShift;
		var lowerCurrent = (1m - DeviationPercent / 100m) * pema + PriceShift;

		var shift = Math.Max(0, Shift);

		decimal? upperForColor;
		decimal? lowerForColor;

		if (shift == 0)
		{
			upperForColor = upperCurrent;
			lowerForColor = lowerCurrent;
		}
		else
		{
			upperForColor = _upperHistory.Count >= shift ? _upperHistory.Peek() : (decimal?)null;
			lowerForColor = _lowerHistory.Count >= shift ? _lowerHistory.Peek() : (decimal?)null;
		}

		// Determine the color code based on envelope breakouts.
		var currentColor = CalculateColor(candle, upperForColor, lowerForColor);

		if (!_pema.IsFormed || !IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistories(currentColor, upperCurrent, lowerCurrent, shift);
			return;
		}

		var hasRecentColor = TryGetColor(SignalBar, out var recentColor);
		var hasOlderColor = TryGetColor(SignalBar + 1, out var olderColor);

		var buyOpenSignal = false;
		var sellOpenSignal = false;
		var buyCloseSignal = false;
		var sellCloseSignal = false;

		// Evaluate signals using stored color history to reproduce the MQL logic.
		if (hasOlderColor)
		{
			if (olderColor > 2)
			{
				if (AllowBuyOpen && hasRecentColor && recentColor < 3)
				buyOpenSignal = true;

				if (AllowSellClose)
				sellCloseSignal = true;
			}
			else if (olderColor < 2)
			{
				if (AllowSellOpen && hasRecentColor && recentColor > 1)
				sellOpenSignal = true;

				if (AllowBuyClose)
				buyCloseSignal = true;
			}
		}

		// Close positions according to signal permissions.
		if (buyCloseSignal && Position > 0)
		SellMarket(Position);

		if (sellCloseSignal && Position < 0)
		BuyMarket(-Position);

		// Open new trades after handling position closures.
		if (buyOpenSignal && Position <= 0)
		{
			if (Position < 0)
			BuyMarket(-Position);

			BuyMarket(TradeVolume);
		}
		else if (sellOpenSignal && Position >= 0)
		{
			if (Position > 0)
			SellMarket(Position);

			SellMarket(TradeVolume);
		}

		UpdateHistories(currentColor, upperCurrent, lowerCurrent, shift);
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		var open = candle.OpenPrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		return AppliedPrice switch
		{
			AppliedPrice.Close => close,
			AppliedPrice.Open => open,
			AppliedPrice.High => high,
			AppliedPrice.Low => low,
			AppliedPrice.Median => (high + low) / 2m,
			AppliedPrice.Typical => (close + high + low) / 3m,
			AppliedPrice.Weighted => (2m * close + high + low) / 4m,
			AppliedPrice.Simple => (open + close) / 2m,
			AppliedPrice.Quarter => (open + close + high + low) / 4m,
			AppliedPrice.TrendFollow0 => close > open ? high : close < open ? low : close,
			AppliedPrice.TrendFollow1 => close > open ? (high + close) / 2m : close < open ? (low + close) / 2m : close,
			AppliedPrice.Demark =>
			{
				var res = high + low + close;

				if (close < open)
				res = (res + low) / 2m;
				else if (close > open)
				res = (res + high) / 2m;
				else
				res = (res + close) / 2m;

				return ((res - low) + (res - high)) / 2m;
			},
			_ => close,
		};
	}

	private static int CalculateColor(ICandleMessage candle, decimal? upper, decimal? lower)
	{
		const int defaultColor = 2;
		var color = defaultColor;

		if (upper is decimal up)
		{
			if (candle.ClosePrice > up)
			color = candle.OpenPrice <= candle.ClosePrice ? 4 : 3;
		}

		if (lower is decimal down)
		{
			if (candle.ClosePrice < down)
			color = candle.OpenPrice > candle.ClosePrice ? 0 : 1;
		}

		return color;
	}

	private bool TryGetColor(int barsAgo, out int color)
	{
		if (barsAgo <= 0 || _colorHistory.Count < barsAgo)
		{
			color = default;
			return false;
		}

		color = _colorHistory[^barsAgo];
		return true;
	}

	private void UpdateHistories(int currentColor, decimal upperCurrent, decimal lowerCurrent, int shift)
	{
		_colorHistory.Add(currentColor);

		var maxColors = Math.Max(3, Math.Max(shift, SignalBar) + 3);
		if (_colorHistory.Count > maxColors)
		_colorHistory.RemoveRange(0, _colorHistory.Count - maxColors);

		if (shift > 0)
		{
			_upperHistory.Enqueue(upperCurrent);
			if (_upperHistory.Count > shift)
			_upperHistory.Dequeue();

			_lowerHistory.Enqueue(lowerCurrent);
			if (_lowerHistory.Count > shift)
			_lowerHistory.Dequeue();
		}
	}

	/// <summary>
	/// Price source options for PEMA calculation.
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
		Demark
	}

	private class PemaIndicator : Indicator<decimal>
	{
		public decimal Length { get; set; } = 50.01m;
		public int Digit { get; set; } = 2;
		public decimal PriceStep { get; set; } = 1m;

		private readonly decimal[] _emaValues = new decimal[8];
		private bool _hasHistory;
		private int _count;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var price = input.GetValue<decimal>();
			var length = Length <= 0m ? 1m : Length;
			var alpha = 2m / (length + 1m);
			var oneMinusAlpha = 1m - alpha;

			var current = price;
			for (var i = 0; i < _emaValues.Length; i++)
			{
				var prev = _hasHistory ? _emaValues[i] : current;
				var ema = alpha * current + oneMinusAlpha * prev;
				_emaValues[i] = ema;
				current = ema;
			}

			_hasHistory = true;
			_count++;

			var pema = 8m * _emaValues[0]
			- 28m * _emaValues[1]
			+ 56m * _emaValues[2]
			- 70m * _emaValues[3]
			+ 56m * _emaValues[4]
			- 28m * _emaValues[5]
			+ 8m * _emaValues[6]
			- _emaValues[7];

			var digits = Math.Max(0, Digit);
			var step = PriceStep > 0m ? PriceStep : 1m;
			var factor = step * (decimal)Math.Pow(10, digits);
			if (factor > 0m)
			pema = Math.Round(pema / factor, MidpointRounding.AwayFromZero) * factor;

			IsFormed = _count > 8;

			return new DecimalIndicatorValue(this, pema, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			Array.Clear(_emaValues, 0, _emaValues.Length);
			_hasHistory = false;
			_count = 0;
		}
	}
}
