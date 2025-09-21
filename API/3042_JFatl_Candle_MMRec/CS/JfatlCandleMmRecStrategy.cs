using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the JFatl candle colour sequence with MMRec money management.
/// Filters OHLC prices with the original FATL coefficients and enters trades on colour transitions.
/// </summary>
public class JfatlCandleMmRecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<decimal> _normalVolume;
	private readonly StrategyParam<decimal> _reducedVolume;
	private readonly StrategyParam<int> _buyTotalTrigger;
	private readonly StrategyParam<int> _buyLossTrigger;
	private readonly StrategyParam<int> _sellTotalTrigger;
	private readonly StrategyParam<int> _sellLossTrigger;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExits;
	private readonly StrategyParam<bool> _enableSellExits;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private readonly FatlFilter _openFilter = new();
	private readonly FatlFilter _highFilter = new();
	private readonly FatlFilter _lowFilter = new();
	private readonly FatlFilter _closeFilter = new();

	private EmaFilter _openSmooth = new(5);
	private EmaFilter _highSmooth = new(5);
	private EmaFilter _lowSmooth = new(5);
	private EmaFilter _closeSmooth = new(5);
	private int _lastSmoothLength;

	private readonly List<int> _colorHistory = new();
	private readonly List<decimal> _buyResults = new();
	private readonly List<decimal> _sellResults = new();

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal _longVolume;
	private decimal _shortVolume;

	/// <summary>
	/// Candle type used for the indicator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of completed bars used as the signal offset.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Exponential smoothing length applied after the FATL kernel.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Normal trade volume when the track record is positive.
	/// </summary>
	public decimal NormalVolume
	{
		get => _normalVolume.Value;
		set => _normalVolume.Value = value;
	}

	/// <summary>
	/// Reduced trade volume after the loss trigger is hit.
	/// </summary>
	public decimal ReducedVolume
	{
		get => _reducedVolume.Value;
		set => _reducedVolume.Value = value;
	}

	/// <summary>
	/// Number of trades checked by the MMRecounter for long positions.
	/// </summary>
	public int BuyTotalTrigger
	{
		get => _buyTotalTrigger.Value;
		set => _buyTotalTrigger.Value = value;
	}

	/// <summary>
	/// Loss threshold for long positions inside the MMRecounter window.
	/// </summary>
	public int BuyLossTrigger
	{
		get => _buyLossTrigger.Value;
		set => _buyLossTrigger.Value = value;
	}

	/// <summary>
	/// Number of trades checked by the MMRecounter for short positions.
	/// </summary>
	public int SellTotalTrigger
	{
		get => _sellTotalTrigger.Value;
		set => _sellTotalTrigger.Value = value;
	}

	/// <summary>
	/// Loss threshold for short positions inside the MMRecounter window.
	/// </summary>
	public int SellLossTrigger
	{
		get => _sellLossTrigger.Value;
		set => _sellLossTrigger.Value = value;
	}

	/// <summary>
	/// Enables long entries.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Enables short entries.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Enables closing of long positions on opposite signals.
	/// </summary>
	public bool EnableBuyExits
	{
		get => _enableBuyExits.Value;
		set => _enableBuyExits.Value = value;
	}

	/// <summary>
	/// Enables closing of short positions on opposite signals.
	/// </summary>
	public bool EnableSellExits
	{
		get => _enableSellExits.Value;
		set => _enableSellExits.Value = value;
	}

	/// <summary>
	/// Optional stop-loss in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Optional take-profit in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="JfatlCandleMmRecStrategy"/>.
	/// </summary>
	public JfatlCandleMmRecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(12).TimeFrame())
		.SetDisplay("Candle Type", "Time-frame for FATL input", "General");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Shift used when checking candle colours", "General");

		_smoothingLength = Param(nameof(SmoothingLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing Length", "EMA length applied after the FATL kernel", "General");

		_normalVolume = Param(nameof(NormalVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Normal Volume", "Default trade volume", "Money Management");

		_reducedVolume = Param(nameof(ReducedVolume), 0.01m)
		.SetDisplay("Reduced Volume", "Volume used after losses", "Money Management");

		_buyTotalTrigger = Param(nameof(BuyTotalTrigger), 3)
		.SetGreaterThanZero()
		.SetDisplay("Buy Total Trigger", "Trades inspected for long MM", "Money Management");

		_buyLossTrigger = Param(nameof(BuyLossTrigger), 2)
		.SetNotNegative()
		.SetDisplay("Buy Loss Trigger", "Losses that enable reduced long volume", "Money Management");

		_sellTotalTrigger = Param(nameof(SellTotalTrigger), 3)
		.SetGreaterThanZero()
		.SetDisplay("Sell Total Trigger", "Trades inspected for short MM", "Money Management");

		_sellLossTrigger = Param(nameof(SellLossTrigger), 2)
		.SetNotNegative()
		.SetDisplay("Sell Loss Trigger", "Losses that enable reduced short volume", "Money Management");

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
		.SetDisplay("Enable Long Entries", "Allow opening long positions", "Permissions");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
		.SetDisplay("Enable Short Entries", "Allow opening short positions", "Permissions");

		_enableBuyExits = Param(nameof(EnableBuyExits), true)
		.SetDisplay("Enable Long Exits", "Allow closing longs on opposite signals", "Permissions");

		_enableSellExits = Param(nameof(EnableSellExits), true)
		.SetDisplay("Enable Short Exits", "Allow closing shorts on opposite signals", "Permissions");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetNotNegative()
		.SetDisplay("Stop Loss Points", "Protective stop measured in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetNotNegative()
		.SetDisplay("Take Profit Points", "Profit target measured in price steps", "Risk");
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

		_colorHistory.Clear();
		_buyResults.Clear();
		_sellResults.Clear();

		_openFilter.Reset();
		_highFilter.Reset();
		_lowFilter.Reset();
		_closeFilter.Reset();

		_openSmooth = new EmaFilter(Math.Max(1, SmoothingLength));
		_highSmooth = new EmaFilter(Math.Max(1, SmoothingLength));
		_lowSmooth = new EmaFilter(Math.Max(1, SmoothingLength));
		_closeSmooth = new EmaFilter(Math.Max(1, SmoothingLength));
		_lastSmoothLength = SmoothingLength;

		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longVolume = 0m;
		_shortVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdateSmoothersIfNeeded();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		UpdateSmoothersIfNeeded();

		var openValue = _openFilter.Process(candle.OpenPrice);
		var highValue = _highFilter.Process(candle.HighPrice);
		var lowValue = _lowFilter.Process(candle.LowPrice);
		var closeValue = _closeFilter.Process(candle.ClosePrice);

		if (openValue is null || highValue is null || lowValue is null || closeValue is null)
		return;

		var smoothOpen = _openSmooth.Process(openValue.Value);
		var smoothHigh = _highSmooth.Process(highValue.Value);
		var smoothLow = _lowSmooth.Process(lowValue.Value);
		var smoothClose = _closeSmooth.Process(closeValue.Value);

		if (smoothOpen is null || smoothHigh is null || smoothLow is null || smoothClose is null)
		return;

		var currentColor = DetermineColor(smoothOpen.Value, smoothClose.Value);
		var (col0, col1) = GetSignalColors(currentColor);

		if (col1 is null)
		{
			AddColorToHistory(currentColor);
			return;
		}

		var priceStep = Security?.PriceStep ?? 1m;

		var closeLong = false;
		var closeShort = false;
		decimal? longExitPrice = null;
		decimal? shortExitPrice = null;

		if (_longEntryPrice is decimal longEntry && _longVolume > 0m)
		{
			if (TakeProfitPoints > 0)
			{
				var target = longEntry + TakeProfitPoints * priceStep;
				if (candle.HighPrice >= target)
				{
					closeLong = true;
					longExitPrice = target;
				}
			}

			if (!closeLong && StopLossPoints > 0)
			{
				var stop = longEntry - StopLossPoints * priceStep;
				if (candle.LowPrice <= stop)
				{
					closeLong = true;
					longExitPrice = stop;
				}
			}
		}

		if (_shortEntryPrice is decimal shortEntry && _shortVolume > 0m)
		{
			if (TakeProfitPoints > 0)
			{
				var target = shortEntry - TakeProfitPoints * priceStep;
				if (candle.LowPrice <= target)
				{
					closeShort = true;
					shortExitPrice = target;
				}
			}

			if (!closeShort && StopLossPoints > 0)
			{
				var stop = shortEntry + StopLossPoints * priceStep;
				if (candle.HighPrice >= stop)
				{
					closeShort = true;
					shortExitPrice = stop;
				}
			}
		}

		if (EnableSellExits && col1 == 2)
		{
			closeShort = closeShort || _shortVolume > 0m;
			shortExitPrice ??= candle.ClosePrice;
		}

		if (EnableBuyExits && col1 == 0)
		{
			closeLong = closeLong || _longVolume > 0m;
			longExitPrice ??= candle.ClosePrice;
		}

		if (closeLong)
		{
			CloseLongPosition(longExitPrice ?? candle.ClosePrice);
		}

		if (closeShort)
		{
			CloseShortPosition(shortExitPrice ?? candle.ClosePrice);
		}

		var allowBuy = EnableBuyEntries && col1 == 2 && col0 != 2;
		var allowSell = EnableSellEntries && col1 == 0 && col0 != 0;

		if (allowBuy && _longVolume <= 0m && Position <= 0m)
		{
			var volume = CalculateBuyVolume();
			if (volume > 0m)
			{
				BuyMarket(volume);
				_longEntryPrice = candle.ClosePrice;
				_longVolume = volume;
			}
		}
		else if (allowSell && _shortVolume <= 0m && Position >= 0m)
		{
			var volume = CalculateSellVolume();
			if (volume > 0m)
			{
				SellMarket(volume);
				_shortEntryPrice = candle.ClosePrice;
				_shortVolume = volume;
			}
		}

		AddColorToHistory(currentColor);
	}

	private void CloseLongPosition(decimal exitPrice)
	{
		if (_longEntryPrice is not decimal entry || _longVolume <= 0m)
		return;

		var quantity = Position > 0m ? Position : _longVolume;
		if (quantity <= 0m)
		return;

		SellMarket(quantity);

		var profit = (exitPrice - entry) * quantity;
		_buyResults.Add(profit);
		TrimList(_buyResults);

		_longEntryPrice = null;
		_longVolume = 0m;
	}

	private void CloseShortPosition(decimal exitPrice)
	{
		if (_shortEntryPrice is not decimal entry || _shortVolume <= 0m)
		return;

		var quantity = Position < 0m ? -Position : _shortVolume;
		if (quantity <= 0m)
		return;

		BuyMarket(quantity);

		var profit = (entry - exitPrice) * quantity;
		_sellResults.Add(profit);
		TrimList(_sellResults);

		_shortEntryPrice = null;
		_shortVolume = 0m;
	}

	private decimal CalculateBuyVolume()
	{
		var totalLimit = Math.Max(1, BuyTotalTrigger);
		var lossLimit = Math.Max(0, BuyLossTrigger);
		var losses = 0;
		var checkedTrades = 0;

		for (var i = _buyResults.Count - 1; i >= 0 && checkedTrades < totalLimit; i--)
		{
			if (_buyResults[i] < 0m)
			losses++;

			checkedTrades++;
		}

		if (lossLimit > 0 && losses >= lossLimit)
		return ReducedVolume;

		return NormalVolume;
	}

	private decimal CalculateSellVolume()
	{
		var totalLimit = Math.Max(1, SellTotalTrigger);
		var lossLimit = Math.Max(0, SellLossTrigger);
		var losses = 0;
		var checkedTrades = 0;

		for (var i = _sellResults.Count - 1; i >= 0 && checkedTrades < totalLimit; i--)
		{
			if (_sellResults[i] < 0m)
			losses++;

			checkedTrades++;
		}

		if (lossLimit > 0 && losses >= lossLimit)
		return ReducedVolume;

		return NormalVolume;
	}

	private void UpdateSmoothersIfNeeded()
	{
		var length = Math.Max(1, SmoothingLength);
		if (length == _lastSmoothLength)
		return;

		_openSmooth = new EmaFilter(length);
		_highSmooth = new EmaFilter(length);
		_lowSmooth = new EmaFilter(length);
		_closeSmooth = new EmaFilter(length);
		_lastSmoothLength = length;
	}

	private static int DetermineColor(decimal openValue, decimal closeValue)
	{
		if (openValue < closeValue)
		return 2;

		if (openValue > closeValue)
		return 0;

		return 1;
	}

	private (int? col0, int? col1) GetSignalColors(int currentColor)
	{
		if (SignalBar <= 0)
		{
			var previous = _colorHistory.Count > 0 ? _colorHistory[^1] : (int?)null;
			return (currentColor, previous);
		}

		if (_colorHistory.Count <= SignalBar)
		return (null, null);

		var index = _colorHistory.Count - SignalBar;
		if (index >= _colorHistory.Count)
		return (null, null);

		var col0 = _colorHistory[index];
		var col1Index = index - 1;
		if (col1Index < 0)
		return (null, null);

		var col1 = _colorHistory[col1Index];
		return (col0, col1);
	}

	private void AddColorToHistory(int color)
	{
		_colorHistory.Add(color);
		const int maxHistory = 1000;
		if (_colorHistory.Count > maxHistory)
		_colorHistory.RemoveRange(0, _colorHistory.Count - maxHistory);
	}

	private static void TrimList(List<decimal> list)
	{
		const int maxItems = 100;
		if (list.Count > maxItems)
		list.RemoveRange(0, list.Count - maxItems);
	}

	private sealed class FatlFilter
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
			0.0007860160m, 0.0130129076m, 0.0040364019m
		};

		private readonly decimal[] _buffer = new decimal[_coefficients.Length];
		private int _filled;

		public void Reset()
		{
			Array.Clear(_buffer, 0, _buffer.Length);
			_filled = 0;
		}

		public decimal? Process(decimal value)
		{
			for (var i = _buffer.Length - 1; i > 0; i--)
			_buffer[i] = _buffer[i - 1];

			_buffer[0] = value;

			if (_filled < _buffer.Length)
			_filled++;

			if (_filled < _buffer.Length)
			return null;

			decimal sum = 0m;
			for (var i = 0; i < _coefficients.Length; i++)
			sum += _coefficients[i] * _buffer[i];

			return sum;
		}
	}

	private sealed class EmaFilter
	{
		private readonly decimal _alpha;
		private decimal? _value;

		public EmaFilter(int length)
		{
			if (length <= 1)
			_alpha = 1m;
			else
			_alpha = 2m / (length + 1m);
		}

		public decimal? Process(decimal input)
		{
			if (_value is null)
			{
				_value = input;
			}
			else
			{
				_value += _alpha * (input - _value.Value);
			}

			return _value;
		}
	}
}
