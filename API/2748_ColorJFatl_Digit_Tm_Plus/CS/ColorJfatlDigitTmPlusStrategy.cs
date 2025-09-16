using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<int> _roundingDigits;
	private readonly StrategyParam<int> _signalBar;

	private ColorJfatlDigitIndicator _indicator;
	private readonly List<int> _colorHistory = new();

	private decimal? _entryPrice;
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
	public AppliedPrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Number of rounding digits applied to the filter line.
	/// </summary>
	public int RoundingDigits
	{
		get => _roundingDigits.Value;
		set => _roundingDigits.Value = value;
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
	public enum AppliedPrice
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

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetDisplay("Take Profit", "Take profit distance in price steps", "Risk");

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
		.SetDisplay("Enable Long Entry", "Allow opening long positions", "Signals");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
		.SetDisplay("Enable Short Entry", "Allow opening short positions", "Signals");

		_enableBuyExits = Param(nameof(EnableBuyExits), true)
		.SetDisplay("Enable Long Exit", "Allow closing long positions", "Signals");

		_enableSellExits = Param(nameof(EnableSellExits), true)
		.SetDisplay("Enable Short Exit", "Allow closing short positions", "Signals");

		_useTimeExit = Param(nameof(UseTimeExit), true)
		.SetDisplay("Use Time Exit", "Enable time based exit", "Exits");

		_holdingMinutes = Param(nameof(HoldingMinutes), 240)
		.SetGreaterThanZero()
		.SetDisplay("Holding Minutes", "Exit after holding for N minutes", "Exits");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Source candles", "General");

		_jmaLength = Param(nameof(JmaLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("JMA Length", "Jurik smoothing length", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(3, 15, 1);

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPrice.Close)
		.SetDisplay("Applied Price", "Price source for the filter", "Indicator");

		_roundingDigits = Param(nameof(RoundingDigits), 2)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Rounding Digits", "Digits for rounding the filter", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Signal Bar", "Number of finished bars used for signals", "Indicator");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_colorHistory.Clear();
		_entryPrice = null;
		_entryTime = null;
		_indicator?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_indicator = new ColorJfatlDigitIndicator
		{
			AppliedPrice = AppliedPrice,
			RoundingDigits = RoundingDigits,
			JmaLength = JmaLength,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

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

		var indicatorValue = (ColorJfatlDigitValue)_indicator.Process(candle);
		if (!_indicator.IsFormed || indicatorValue.Color is null)
		return;

		_colorHistory.Add(indicatorValue.Color.Value);

		var maxHistory = Math.Max(SignalBar + 2, 2);
		if (_colorHistory.Count > maxHistory)
		_colorHistory.RemoveAt(0);

		if (!TryGetColors(out var currentColor, out var previousColor))
		return;

		var buyOpen = false;
		var sellOpen = false;
		var buyClose = false;
		var sellClose = false;

		if (currentColor == 2)
		{
			if (EnableBuyEntries && previousColor < 2)
			buyOpen = true;

			if (EnableSellExits)
			sellClose = true;
		}
		else if (currentColor == 0)
		{
			if (EnableSellEntries && previousColor > 0)
			sellOpen = true;

			if (EnableBuyExits)
			buyClose = true;
		}

		HandleTimeExit(candle);
		if (HandleStops(candle))
		return;

		if (buyClose && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			_entryPrice = null;
			_entryTime = null;
		}

		if (sellClose && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = null;
			_entryTime = null;
		}

		if (buyOpen && Position == 0 && IsFormedAndOnlineAndAllowTrading())
		{
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_entryTime = candle.CloseTime;
		}
		else if (sellOpen && Position == 0 && IsFormedAndOnlineAndAllowTrading())
		{
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_entryTime = candle.CloseTime;
		}

		if (Position == 0)
		{
			_entryPrice = null;
			_entryTime = null;
		}
	}

	private bool TryGetColors(out int currentColor, out int previousColor)
	{
		currentColor = 0;
		previousColor = 0;

		var offset = Math.Max(SignalBar, 1);
		if (_colorHistory.Count < offset + 1)
		return false;

		currentColor = _colorHistory[^offset];
		previousColor = _colorHistory[^(offset + 1)];
		return true;
	}

	private void HandleTimeExit(ICandleMessage candle)
	{
		if (!UseTimeExit || Position == 0 || _entryTime is null || HoldingMinutes <= 0)
		return;

		var elapsed = candle.CloseTime - _entryTime.Value;
		if (elapsed < TimeSpan.FromMinutes(HoldingMinutes))
		return;

		if (Position > 0)
		SellMarket(Math.Abs(Position));
		else if (Position < 0)
		BuyMarket(Math.Abs(Position));

		_entryPrice = null;
		_entryTime = null;
	}

	private bool HandleStops(ICandleMessage candle)
	{
		if (Position == 0 || _entryPrice is null)
		return false;

		var step = Security?.PriceStep ?? 1m;
		var stopOffset = StopLossPoints > 0 ? StopLossPoints * step : 0m;
		var takeOffset = TakeProfitPoints > 0 ? TakeProfitPoints * step : 0m;

		if (Position > 0)
		{
			if (stopOffset > 0m && candle.LowPrice <= _entryPrice.Value - stopOffset)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = null;
				_entryTime = null;
				return true;
			}

			if (takeOffset > 0m && candle.HighPrice >= _entryPrice.Value + takeOffset)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = null;
				_entryTime = null;
				return true;
			}
		}
		else if (Position < 0)
		{
			if (stopOffset > 0m && candle.HighPrice >= _entryPrice.Value + stopOffset)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = null;
				_entryTime = null;
				return true;
			}

			if (takeOffset > 0m && candle.LowPrice <= _entryPrice.Value - takeOffset)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = null;
				_entryTime = null;
				return true;
			}
		}

		return false;
	}

	private sealed class ColorJfatlDigitIndicator : Indicator<ICandleMessage>
	{
		private static readonly decimal[] _coefficients =
		{
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
		};

		private readonly decimal[] _buffer = new decimal[_coefficients.Length];
		private int _bufferCount;
		private int _bufferIndex;
		private decimal? _previousLine;
		private int? _previousColor;
		private readonly JurikMovingAverage _jma = new();

		public AppliedPrice AppliedPrice { get; set; } = AppliedPrice.Close;

		public int RoundingDigits { get; set; } = 2;

		public int JmaLength
		{
			get => _jma.Length;
			set => _jma.Length = Math.Max(1, value);
		}

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			if (!input.IsFinal)
			return new ColorJfatlDigitValue(this, input, null, null, false);

			var price = GetPrice(candle);
			_buffer[_bufferIndex] = price;
			_bufferIndex = (_bufferIndex + 1) % _buffer.Length;
			if (_bufferCount < _buffer.Length)
			_bufferCount++;

			if (_bufferCount < _buffer.Length)
			{
				IsFormed = false;
				return new ColorJfatlDigitValue(this, input, null, null, false);
			}

			decimal fatl = 0m;
			var index = _bufferIndex;
			for (var i = 0; i < _coefficients.Length; i++)
			{
				index = (index - 1 + _buffer.Length) % _buffer.Length;
				fatl += _coefficients[i] * _buffer[index];
			}

			var jmaValue = _jma.Process(new DecimalIndicatorValue(_jma, fatl));
			if (!_jma.IsFormed)
			{
				IsFormed = false;
				return new ColorJfatlDigitValue(this, input, null, null, false);
			}

			var smoothed = Math.Round(jmaValue.ToDecimal(), RoundingDigits, MidpointRounding.AwayFromZero);

			var color = 1;
			if (_previousLine is decimal prev)
			{
				var diff = smoothed - prev;
				if (diff > 0m)
				color = 2;
				else if (diff < 0m)
				color = 0;
				else if (_previousColor.HasValue)
				color = _previousColor.Value;
			}

			_previousLine = smoothed;
			_previousColor = color;
			IsFormed = true;

			return new ColorJfatlDigitValue(this, input, smoothed, color, true);
		}

		public override void Reset()
		{
			base.Reset();

			Array.Clear(_buffer, 0, _buffer.Length);
			_bufferCount = 0;
			_bufferIndex = 0;
			_previousLine = null;
			_previousColor = null;
			_jma.Reset();
		}

		private decimal GetPrice(ICandleMessage candle)
		=> AppliedPrice switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.AverageOC => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.AverageOHLC => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			AppliedPrice.TrendFollow2 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
			AppliedPrice.Demark => GetDemarkPrice(candle),
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
	}

	private sealed class ColorJfatlDigitValue : ComplexIndicatorValue
	{
		public ColorJfatlDigitValue(IIndicator indicator, IIndicatorValue input, decimal? line, int? color, bool isFormed)
		: base(indicator, input, (nameof(Line), line), (nameof(Color), color))
		{
			IsFormed = isFormed;
		}

		public decimal? Line => (decimal?)GetValue(nameof(Line));

		public int? Color => (int?)GetValue(nameof(Color));
	}
}
