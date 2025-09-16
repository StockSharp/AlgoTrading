using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Charting;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Displays open position profit in pips, currency, percentage and spread information.
/// Mirrors the behaviour of the MetaTrader "Show Pips" indicator in StockSharp.
/// </summary>
public class ShowPipsStrategy : Strategy
{
	private readonly StrategyParam<DisplayMode> _displayMode;
	private readonly StrategyParam<bool> _showProfit;
	private readonly StrategyParam<bool> _showPercent;
	private readonly StrategyParam<bool> _showSpread;
	private readonly StrategyParam<bool> _showTime;
	private readonly StrategyParam<string> _separator;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _pipSize;

	private IChartArea? _chartArea;
	private DateTimeOffset? _currentBarOpenTime;
	private decimal? _lastPrice;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private string? _lastLoggedText;

	/// <summary>
	/// Available output modes for the status text.
	/// </summary>
	public enum DisplayMode
	{
		/// <summary>
		/// Draw text near the current price on the chart.
		/// </summary>
		FollowPrice,

		/// <summary>
		/// Write the status line into the strategy log.
		/// </summary>
		AsComment,

		/// <summary>
		/// Draw text near the lower-right corner of the chart.
		/// </summary>
		CornerLabel,
	}

	/// <summary>
	/// Chooses how the status information is displayed.
	/// </summary>
	public DisplayMode ShowType
	{
		get => _displayMode.Value;
		set => _displayMode.Value = value;
	}

	/// <summary>
	/// Enables the currency profit output.
	/// </summary>
	public bool ShowProfit
	{
		get => _showProfit.Value;
		set => _showProfit.Value = value;
	}

	/// <summary>
	/// Enables the percentage profit output.
	/// </summary>
	public bool ShowPercent
	{
		get => _showPercent.Value;
		set => _showPercent.Value = value;
	}

	/// <summary>
	/// Enables the spread output.
	/// </summary>
	public bool ShowSpread
	{
		get => _showSpread.Value;
		set => _showSpread.Value = value;
	}

	/// <summary>
	/// Enables the countdown until the next bar closes.
	/// </summary>
	public bool ShowTime
	{
		get => _showTime.Value;
		set => _showTime.Value = value;
	}

	/// <summary>
	/// Separator string between blocks of text.
	/// </summary>
	public string Separator
	{
		get => _separator.Value;
		set => _separator.Value = value;
	}

	/// <summary>
	/// Candle type used to determine the bar duration.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Size of one pip for the traded symbol.
	/// </summary>
	public decimal PipSize
	{
		get => _pipSize.Value;
		set => _pipSize.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="ShowPipsStrategy"/> with default parameters.
	/// </summary>
	public ShowPipsStrategy()
	{
		_displayMode = Param(nameof(ShowType), DisplayMode.FollowPrice)
			.SetDisplay("Display Mode", "How to present status information", "Visualization");

		_showProfit = Param(nameof(ShowProfit), false)
			.SetDisplay("Show Profit", "Toggle currency profit display", "Output");

		_showPercent = Param(nameof(ShowPercent), false)
			.SetDisplay("Show Percent", "Toggle percentage profit display", "Output");

		_showSpread = Param(nameof(ShowSpread), true)
			.SetDisplay("Show Spread", "Toggle spread display", "Output");

		_showTime = Param(nameof(ShowTime), true)
			.SetDisplay("Show Time", "Toggle countdown to bar close", "Output");

		_separator = Param(nameof(Separator), " | ")
			.SetDisplay("Separator", "Separator between text blocks", "Output");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for timing information", "General");

		_pipSize = Param(nameof(PipSize), 0.0001m)
			.SetGreaterThanZero()
			.SetDisplay("Pip Size", "Price change represented by one pip", "Calculation");
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

		_chartArea = null;
		_currentBarOpenTime = null;
		_lastPrice = null;
		_bestBid = null;
		_bestAsk = null;
		_lastLoggedText = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
			.Bind(ProcessCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		_chartArea = CreateChartArea();
		if (_chartArea != null)
		{
			DrawCandles(_chartArea, candleSubscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.ClosePrice != default)
		{
			// Track the latest closing price for PnL estimation when no trades arrive.
			_lastPrice = candle.ClosePrice;
		}

		if (candle.State == CandleStates.Finished)
		{
			// Calculate the start time of the currently forming bar.
			var tf = GetTimeFrame();
			if (tf > TimeSpan.Zero)
			{
				_currentBarOpenTime = candle.OpenTime + tf;
			}
		}

		UpdateDisplay();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		var last = message.TryGetDecimal(Level1Fields.LastTradePrice);
		if (last != null)
		{
			_lastPrice = last.Value;
		}

		var bid = message.TryGetDecimal(Level1Fields.BestBidPrice);
		if (bid != null)
		{
			_bestBid = bid.Value;
		}

		var ask = message.TryGetDecimal(Level1Fields.BestAskPrice);
		if (ask != null)
		{
			_bestAsk = ask.Value;
		}

		UpdateDisplay();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		var text = BuildStatusText();
		if (string.IsNullOrEmpty(text))
			return;

		switch (ShowType)
		{
			case DisplayMode.AsComment:
			{
				if (text != _lastLoggedText)
				{
					AddInfo(text);
					_lastLoggedText = text;
				}

				break;
			}
			case DisplayMode.FollowPrice:
			{
				if (_chartArea == null)
					return;

				var price = GetPriceForDisplay();
				DrawText(_chartArea, CurrentTime, price, text);
				break;
			}
			case DisplayMode.CornerLabel:
			{
				if (_chartArea == null)
					return;

				var price = GetPriceForDisplay();
				var offset = Security.PriceStep ?? 0.0001m;
				DrawText(_chartArea, CurrentTime, price + offset * 5m, text);
				break;
			}
		}
	}

	private string BuildStatusText()
	{
		var pipInfo = CalculatePipInfo(out var profit, out var percent);
		if (pipInfo == null)
			return string.Empty;

		var blocks = new List<string> { pipInfo }; // Always show pip count first.

		if (ShowProfit)
		{
			blocks.Add(profit.ToString("0.00"));
		}

		if (ShowPercent && percent != null)
		{
			blocks.Add($"{percent.Value:0.0}%");
		}

		if (ShowSpread)
		{
			var spreadText = BuildSpreadText();
			if (!string.IsNullOrEmpty(spreadText))
			{
				blocks.Add(spreadText);
			}
		}

		if (ShowTime)
		{
			var timeText = BuildCountdownText();
			if (!string.IsNullOrEmpty(timeText))
			{
				blocks.Add(timeText);
			}
		}

		return string.Join(Separator, blocks);
	}

	private string? CalculatePipInfo(out decimal profit, out decimal? percent)
	{
		profit = 0m;
		percent = null;

		var price = GetPriceForDisplay();
		if (Position == 0m || price == 0m)
		{
			return "0.0 pips";
		}

		var entryPrice = PositionPrice;
		var priceStep = Security.PriceStep ?? 0m;
		var priceStepValue = Security.PriceStepValue ?? 0m;
		var volumeStep = Security.VolumeStep ?? 1m;
		var pipSize = PipSize;

		var priceDiff = price - entryPrice;
		var ticks = priceStep > 0m ? priceDiff / priceStep : 0m;
		var contracts = volumeStep > 0m ? Position / volumeStep : Position;

		if (priceStep > 0m && priceStepValue > 0m)
		{
			profit = ticks * priceStepValue * contracts;
		}
		else
		{
			profit = priceDiff * Position;
		}

		var pipMultiplier = Position == 0m ? 0m : Math.Sign(Position);
		var pips = pipSize > 0m
			? priceDiff / pipSize * pipMultiplier
			: ticks * pipMultiplier;

		var balance = Portfolio?.CurrentValue ?? 0m;
		if (balance > 0m)
		{
			percent = profit / balance * 100m;
		}

		return $"{pips:0.0} pips";
	}

	private string BuildSpreadText()
	{
		if (_bestBid == null || _bestAsk == null)
			return string.Empty;

		var priceStep = Security.PriceStep ?? 0m;
		var spread = _bestAsk.Value - _bestBid.Value;
		if (spread <= 0m)
			return string.Empty;

		if (priceStep > 0m)
		{
			var ticks = spread / priceStep;
			return $"Spread {ticks:0.##}";
		}

		return $"Spread {spread:0.#####}";
	}

	private string BuildCountdownText()
	{
		var tf = GetTimeFrame();
		if (_currentBarOpenTime == null || tf <= TimeSpan.Zero)
			return string.Empty;

		var closeTime = _currentBarOpenTime.Value + tf;
		var timeLeft = closeTime - CurrentTime;
		if (timeLeft < TimeSpan.Zero)
			timeLeft = TimeSpan.Zero;

		return $"Next bar in {timeLeft:mm\\:ss}";
	}

	private TimeSpan GetTimeFrame()
	{
		return CandleType.Arg is TimeSpan span ? span : TimeSpan.Zero;
	}

	private decimal GetPriceForDisplay()
	{
		if (_lastPrice != null)
		{
			return _lastPrice.Value;
		}

		if (_bestBid != null && _bestAsk != null)
		{
			return (_bestBid.Value + _bestAsk.Value) / 2m;
		}

		return Security.LastPrice ?? 0m;
	}
}
