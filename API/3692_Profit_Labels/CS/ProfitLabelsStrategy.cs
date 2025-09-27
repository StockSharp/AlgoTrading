using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Profit Labels translation (54352).
/// Draws realized PnL labels after trades close and opens positions on TEMA trend changes.
/// </summary>
public class ProfitLabelsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _temaPeriod;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<bool> _placingTrade;
	private readonly StrategyParam<decimal> _labelOffset;

	private IChartArea? _chartArea;
	private DateTimeOffset? _lastSignalTime;
	private bool _previousTradeBuy;
	private bool _previousTradeSell;
	private decimal? _entryPrice;
	private Sides? _entrySide;
	private decimal _positionVolume;

	private decimal? _tema0;
	private decimal? _tema1;
	private decimal? _tema2;
	private decimal? _tema3;

	/// <summary>
	/// Initializes a new instance of <see cref="ProfitLabelsStrategy"/>.
	/// </summary>
	public ProfitLabelsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");

		_temaPeriod = Param(nameof(TemaPeriod), 6)
			.SetDisplay("TEMA Period", "Period used for the triple EMA", "Indicator");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Volume used for each position", "Trading");

		_placingTrade = Param(nameof(PlacingTrade), false)
			.SetDisplay("Enable Trading", "Place live orders on signals", "Trading")
			.SetCanOptimize(false);

		_labelOffset = Param(nameof(LabelOffset), 0m)
			.SetDisplay("Label Offset", "Vertical offset for profit labels", "Visualization")
			.SetCanOptimize(false);
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period used for the triple EMA calculation.
	/// </summary>
	public int TemaPeriod
	{
		get => _temaPeriod.Value;
		set => _temaPeriod.Value = value;
	}

	/// <summary>
	/// Volume used for each position.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Defines whether live orders should be placed.
	/// </summary>
	public bool PlacingTrade
	{
		get => _placingTrade.Value;
		set => _placingTrade.Value = value;
	}

	/// <summary>
	/// Vertical offset applied to profit labels.
	/// </summary>
	public decimal LabelOffset
	{
		get => _labelOffset.Value;
		set => _labelOffset.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		var tema = new TripleExponentialMovingAverage
		{
			Length = TemaPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(tema, ProcessCandle)
			.Start();

		_chartArea = CreateChartArea();
		if (_chartArea != null)
		{
			DrawCandles(_chartArea, subscription);
			DrawIndicator(_chartArea, tema, subscription);
		}
	}

	// Process each finished candle and react to TEMA trend flips.
	private void ProcessCandle(ICandleMessage candle, decimal temaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_tema3 = _tema2;
		_tema2 = _tema1;
		_tema1 = _tema0;
		_tema0 = temaValue;

		if (_tema3 is null || _tema2 is null || _tema1 is null || _tema0 is null)
			return;

		var trendUp = _tema2 < _tema3 && _tema0 > _tema1;
		var trendDown = _tema2 > _tema3 && _tema0 < _tema1;

		if (!PlacingTrade)
			return;

		if (trendUp)
		{
			HandleLongSignal(candle);
		}
		else if (trendDown)
		{
			HandleShortSignal(candle);
		}
	}

	// React to a bullish TEMA crossover.
	private void HandleLongSignal(ICandleMessage candle)
	{
		if (_lastSignalTime == candle.OpenTime)
			return;

		_lastSignalTime = candle.OpenTime;

		if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			_previousTradeBuy = false;
			_previousTradeSell = false;
			return;
		}

		if (Position != 0m || _previousTradeBuy)
			return;

		BuyMarket(TradeVolume);
		_previousTradeBuy = true;
		_previousTradeSell = false;
	}

	// React to a bearish TEMA crossover.
	private void HandleShortSignal(ICandleMessage candle)
	{
		if (_lastSignalTime == candle.OpenTime)
			return;

		_lastSignalTime = candle.OpenTime;

		if (Position > 0m)
		{
			SellMarket(Position);
			_previousTradeBuy = false;
			_previousTradeSell = false;
			return;
		}

		if (Position != 0m || _previousTradeSell)
			return;

		SellMarket(TradeVolume);
		_previousTradeBuy = false;
		_previousTradeSell = true;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position != 0m && _entrySide is null)
		{
			_entrySide = Position > 0m ? Sides.Buy : Sides.Sell;
			_entryPrice = trade.Trade.Price;
			_positionVolume = Math.Abs(Position);
			return;
		}

		if (Position == 0m && _entrySide != null && _entryPrice.HasValue)
		{
			var exitPrice = trade.Trade.Price;
			var profit = CalculateProfit(_entrySide.Value, _entryPrice.Value, exitPrice, _positionVolume);

			DrawProfitLabel(profit, trade.Trade.ServerTime, exitPrice);

			_entrySide = null;
			_entryPrice = null;
			_positionVolume = 0m;
		}
	}

	// Calculate realized PnL using instrument parameters when possible.
	private decimal CalculateProfit(Sides entrySide, decimal entryPrice, decimal exitPrice, decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep;
		var stepValue = Security?.StepValue;

		if (priceStep.HasValue && priceStep.Value > 0m && stepValue.HasValue && stepValue.Value > 0m)
		{
			var points = (exitPrice - entryPrice) / priceStep.Value;
			var direction = entrySide == Sides.Buy ? 1m : -1m;
			return points * stepValue.Value * volume * direction;
		}

		var rawDifference = entrySide == Sides.Buy
			? exitPrice - entryPrice
			: entryPrice - exitPrice;

		return rawDifference * volume;
	}

	// Draw a label with realized profit information.
	private void DrawProfitLabel(decimal profit, DateTimeOffset time, decimal price)
	{
		if (_chartArea is null)
			return;

		var labelText = $"{GetCurrencySymbol()}{profit:F2}";
		var targetPrice = price + LabelOffset;

		DrawText(_chartArea, time, targetPrice, labelText);
	}

	// Return a simple currency prefix based on the security currency.
	private string GetCurrencySymbol()
	{
		var currency = Security?.Currency?.ToUpperInvariant();
		return currency switch
		{
			"USD" => "$",
			"EUR" => "€",
			"GBP" => "£",
			"JPY" => "¥",
			"AUD" => "A$",
			"CAD" => "C$",
			"CHF" => "CHF",
			"CNY" => "¥",
			"HKD" => "HK$",
			"NZD" => "NZ$",
			"SEK" => "kr",
			"SGD" => "S$",
			"NOK" => "kr",
			"MXN" => "Mex$",
			"INR" => "₹",
			"RUB" => "₽",
			"BRL" => "R$",
			"ZAR" => "R",
			"TRY" => "₺",
			"KRW" => "₩",
			"THB" => "฿",
			"IDR" => "Rp",
			"MYR" => "RM",
			"PHP" => "₱",
			_ => currency ?? string.Empty
		};
	}
}

