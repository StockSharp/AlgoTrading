using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that opens positions when price crosses predefined synthetic lines.
/// It replicates the idea of the original MQL Cross Line Trader by checking finished candles.
/// </summary>
public class CrossLineTraderStrategy : Strategy
{
	public enum LineDirectionMode
	{
		FromLabel,
		ForceBuy,
		ForceSell,
	}

	private enum LineType
	{
		Horizontal,
		Trend,
		Vertical,
	}

	private enum TradeDirection
	{
		Buy,
		Sell,
	}

	private sealed class LineState
	{
		public LineState(string name, string label, LineType type, decimal basePrice, decimal slopePerBar, int length, bool ray)
		{
			Name = string.IsNullOrWhiteSpace(name) ? type.ToString() : name;
			Label = label ?? string.Empty;
			Type = type;
			BasePrice = basePrice;
			SlopePerBar = slopePerBar;
			Length = Math.Max(0, length);
			Ray = ray;
			IsActive = true;
			StepsProcessed = 0;
		}

		public string Name { get; }

		public string Label { get; }

		public LineType Type { get; }

		public decimal BasePrice { get; }

		public decimal SlopePerBar { get; }

		public int Length { get; }

		public bool Ray { get; }

		public bool IsActive { get; set; }

		public int StepsProcessed { get; set; }

		public decimal GetPrice(int index)
		{
			if (Type == LineType.Vertical)
				return 0m;

			var clampedIndex = Math.Max(0, index);

			if (!Ray && Length > 0)
				clampedIndex = Math.Min(clampedIndex, Length);

			return BasePrice + SlopePerBar * clampedIndex;
		}
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<LineDirectionMode> _directionMode;
	private readonly StrategyParam<string> _buyLabel;
	private readonly StrategyParam<string> _sellLabel;
	private readonly StrategyParam<string> _lineDefinitions;
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<decimal> _takeProfitOffset;

	private List<LineState> _lines = new();
	private decimal? _previousOpen;
	private decimal _entryPrice;

	/// <summary>
	/// Candle type used for line intersection checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume sent with market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Defines how the strategy resolves trade direction for every line.
	/// </summary>
	public LineDirectionMode DirectionMode
	{
		get => _directionMode.Value;
		set => _directionMode.Value = value;
	}

	/// <summary>
	/// Text label that identifies buy lines when DirectionMode is FromLabel.
	/// </summary>
	public string BuyLabel
	{
		get => _buyLabel.Value;
		set => _buyLabel.Value = value;
	}

	/// <summary>
	/// Text label that identifies sell lines when DirectionMode is FromLabel.
	/// </summary>
	public string SellLabel
	{
		get => _sellLabel.Value;
		set => _sellLabel.Value = value;
	}

	/// <summary>
	/// Raw definition of synthetic lines. Each line uses the format:
	/// Name|Type|Label|BasePrice|SlopePerBar|Length|Ray
	/// </summary>
	public string LineDefinitions
	{
		get => _lineDefinitions.Value;
		set => _lineDefinitions.Value = value;
	}

	/// <summary>
	/// Protective stop distance in price units.
	/// </summary>
	public decimal StopLossOffset
	{
		get => _stopLossOffset.Value;
		set => _stopLossOffset.Value = value;
	}

	/// <summary>
	/// Protective take profit distance in price units.
	/// </summary>
	public decimal TakeProfitOffset
	{
		get => _takeProfitOffset.Value;
		set => _takeProfitOffset.Value = value;
	}

	/// <summary>
	/// Constructor that configures strategy parameters.
	/// </summary>
	public CrossLineTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for intersections", "Data");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Order volume used for entries", "Trading")
			.SetGreaterThanZero();

		_directionMode = Param(nameof(DirectionMode), LineDirectionMode.FromLabel)
			.SetDisplay("Direction Mode", "How to pick trade direction for a line", "Trading");

		_buyLabel = Param(nameof(BuyLabel), "Buy")
			.SetDisplay("Buy Label", "Text that marks buy lines when mode uses labels", "Trading");

		_sellLabel = Param(nameof(SellLabel), "Sell")
			.SetDisplay("Sell Label", "Text that marks sell lines when mode uses labels", "Trading");

		_lineDefinitions = Param(nameof(LineDefinitions),
			"TrendLine|Trend|Buy|1.1000|0.0005|8|false;HorizontalSell|Horizontal|Sell|1.1050|0|0|true;VerticalImpulse|Vertical|Buy|0|0|1|false")
			.SetDisplay("Line Definitions", "Encoded collection of synthetic lines", "Lines")
			.SetCanOptimize(false);

		_stopLossOffset = Param(nameof(StopLossOffset), 0m)
			.SetDisplay("Stop Loss Offset", "Price distance for protective exit", "Risk")
			.SetGreaterOrEqualZero();

		_takeProfitOffset = Param(nameof(TakeProfitOffset), 0m)
			.SetDisplay("Take Profit Offset", "Price distance for profit taking", "Risk")
			.SetGreaterOrEqualZero();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lines = new List<LineState>();
		_previousOpen = null;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_lines = ParseLineDefinitions(LineDefinitions);
		_previousOpen = null;
		_entryPrice = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (TradeVolume <= 0)
			return;

		var currentOpen = candle.OpenPrice;

		foreach (var line in _lines)
		{
			if (!line.IsActive)
				continue;

			var previousIndex = line.StepsProcessed;
			var currentIndex = previousIndex + 1;

			if (line.Type == LineType.Vertical)
			{
				if (currentIndex >= Math.Max(1, line.Length))
				{
					var direction = ResolveDirection(line);

					if (direction != null && TryOpenPosition(direction.Value, line, candle))
						line.IsActive = false;
				}

				line.StepsProcessed = currentIndex;
				continue;
			}

			if (!line.Ray && line.Length > 0 && previousIndex >= line.Length)
			{
				line.IsActive = false;
				continue;
			}

			if (_previousOpen is null)
			{
				line.StepsProcessed = currentIndex;
				continue;
			}

			var previousLinePrice = line.GetPrice(previousIndex);
			var currentLinePrice = line.GetPrice(currentIndex);
			var directionForLine = ResolveDirection(line);

			if (directionForLine is null)
			{
				line.StepsProcessed = currentIndex;
				continue;
			}

			var crossUp = line.Type == LineType.Horizontal
				? _previousOpen.Value <= previousLinePrice && currentOpen > previousLinePrice
				: _previousOpen.Value <= previousLinePrice && currentOpen > currentLinePrice;

			var crossDown = line.Type == LineType.Horizontal
				? _previousOpen.Value >= previousLinePrice && currentOpen < previousLinePrice
				: _previousOpen.Value >= previousLinePrice && currentOpen < currentLinePrice;

			if (crossUp && directionForLine == TradeDirection.Buy && Position <= 0)
			{
				if (TryOpenPosition(TradeDirection.Buy, line, candle))
					line.IsActive = false;
			}
			else if (crossDown && directionForLine == TradeDirection.Sell && Position >= 0)
			{
				if (TryOpenPosition(TradeDirection.Sell, line, candle))
					line.IsActive = false;
			}

			line.StepsProcessed = currentIndex;

			if (!line.Ray && line.Length > 0 && currentIndex >= line.Length)
				line.IsActive = false;
		}

		ManageProtectiveExits(candle);

		_previousOpen = currentOpen;
	}

	private TradeDirection? ResolveDirection(LineState line)
	{
		switch (DirectionMode)
		{
			case LineDirectionMode.ForceBuy:
				return TradeDirection.Buy;
			case LineDirectionMode.ForceSell:
				return TradeDirection.Sell;
			case LineDirectionMode.FromLabel:
				if (!string.IsNullOrWhiteSpace(BuyLabel) &&
					string.Equals(line.Label, BuyLabel, StringComparison.OrdinalIgnoreCase))
					return TradeDirection.Buy;

				if (!string.IsNullOrWhiteSpace(SellLabel) &&
					string.Equals(line.Label, SellLabel, StringComparison.OrdinalIgnoreCase))
					return TradeDirection.Sell;
				break;
		}

		return null;
	}

	private bool TryOpenPosition(TradeDirection direction, LineState line, ICandleMessage candle)
	{
		if (direction == TradeDirection.Buy)
		{
			if (Position > 0)
				return false;

			BuyMarket(TradeVolume);
			_entryPrice = candle.OpenPrice;
			AddInfoLog($"Line '{line.Name}' triggered BUY at {candle.OpenPrice:0.#####}.");
		}
		else
		{
			if (Position < 0)
				return false;

			SellMarket(TradeVolume);
			_entryPrice = candle.OpenPrice;
			AddInfoLog($"Line '{line.Name}' triggered SELL at {candle.OpenPrice:0.#####}.");
		}

		return true;
	}

	private void ManageProtectiveExits(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var volume = Math.Abs(Position);

			if (StopLossOffset > 0m && candle.LowPrice <= _entryPrice - StopLossOffset)
			{
				SellMarket(volume);
				return;
			}

			if (TakeProfitOffset > 0m && candle.HighPrice >= _entryPrice + TakeProfitOffset)
			{
				SellMarket(volume);
			}
		}
		else if (Position < 0)
		{
			var volume = Math.Abs(Position);

			if (StopLossOffset > 0m && candle.HighPrice >= _entryPrice + StopLossOffset)
			{
				BuyMarket(volume);
				return;
			}

			if (TakeProfitOffset > 0m && candle.LowPrice <= _entryPrice - TakeProfitOffset)
			{
				BuyMarket(volume);
			}
		}
	}

	private List<LineState> ParseLineDefinitions(string raw)
	{
		var result = new List<LineState>();

		if (string.IsNullOrWhiteSpace(raw))
			return result;

		var entries = raw.Split(new[] { '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries);

		foreach (var entry in entries)
		{
			var parts = entry.Split('|');

			if (parts.Length < 7)
				continue;

			var name = parts[0].Trim();
			var typeText = parts[1].Trim();
			var label = parts[2].Trim();
			var basePriceText = parts[3].Trim();
			var slopeText = parts[4].Trim();
			var lengthText = parts[5].Trim();
			var rayText = parts[6].Trim();

			if (!Enum.TryParse<LineType>(typeText, true, out var type))
				continue;

			if (!decimal.TryParse(basePriceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var basePrice))
				continue;

			if (!decimal.TryParse(slopeText, NumberStyles.Number, CultureInfo.InvariantCulture, out var slope))
				slope = 0m;

			if (!int.TryParse(lengthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var length))
				length = 0;

			if (!bool.TryParse(rayText, out var ray))
				ray = false;

			if (type == LineType.Vertical && length <= 0)
				length = 1;

			result.Add(new LineState(name, label, type, basePrice, slope, length, ray));
		}

		return result;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position == 0)
		{
			_entryPrice = 0m;
			return;
		}

		_entryPrice = trade.Trade.Price;
	}
}
