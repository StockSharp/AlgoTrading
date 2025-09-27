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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean-reversion strategy that recreates the wajdyss Ichimoku candle coloring logic with adaptive money management.
/// </summary>
public class WajdyssIchimokuCandleMmrecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kijunLength;
	private readonly StrategyParam<int> _signalBarShift;
	private readonly StrategyParam<bool> _buyOpenEnabled;
	private readonly StrategyParam<bool> _sellOpenEnabled;
	private readonly StrategyParam<bool> _buyCloseEnabled;
	private readonly StrategyParam<bool> _sellCloseEnabled;
	private readonly StrategyParam<decimal> _normalVolume;
	private readonly StrategyParam<decimal> _reducedVolume;
	private readonly StrategyParam<int> _lossTriggerCount;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private Highest _highest;
	private Lowest _lowest;
	private readonly List<int> _colorHistory = new();
	private readonly Queue<bool> _buyLossHistory = new();
	private readonly Queue<bool> _sellLossHistory = new();
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int KijunLength
	{
		get => _kijunLength.Value;
		set => _kijunLength.Value = value;
	}

	public int SignalBarShift
	{
		get => _signalBarShift.Value;
		set => _signalBarShift.Value = value;
	}

	public bool BuyPosOpen
	{
		get => _buyOpenEnabled.Value;
		set => _buyOpenEnabled.Value = value;
	}

	public bool SellPosOpen
	{
		get => _sellOpenEnabled.Value;
		set => _sellOpenEnabled.Value = value;
	}

	public bool BuyPosClose
	{
		get => _buyCloseEnabled.Value;
		set => _buyCloseEnabled.Value = value;
	}

	public bool SellPosClose
	{
		get => _sellCloseEnabled.Value;
		set => _sellCloseEnabled.Value = value;
	}

	public decimal NormalVolume
	{
		get => _normalVolume.Value;
		set => _normalVolume.Value = value;
	}

	public decimal ReducedVolume
	{
		get => _reducedVolume.Value;
		set => _reducedVolume.Value = value;
	}

	public int LossTriggerCount
	{
		get => _lossTriggerCount.Value;
		set => _lossTriggerCount.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public WajdyssIchimokuCandleMmrecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for signals", "General");

		_kijunLength = Param(nameof(KijunLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("Kijun Length", "Lookback for the Ichimoku base line", "Indicator");

		_signalBarShift = Param(nameof(SignalBarShift), 1)
		.SetNotNegative()
		.SetDisplay("Signal Bar", "Shift applied to candle colors", "Indicator");

		_buyOpenEnabled = Param(nameof(BuyPosOpen), true)
		.SetDisplay("Enable Long Entries", "Allow the strategy to open long trades", "Trading");

		_sellOpenEnabled = Param(nameof(SellPosOpen), true)
		.SetDisplay("Enable Short Entries", "Allow the strategy to open short trades", "Trading");

		_buyCloseEnabled = Param(nameof(BuyPosClose), true)
		.SetDisplay("Close Longs", "Allow closing longs on opposite colors", "Trading");

		_sellCloseEnabled = Param(nameof(SellPosClose), true)
		.SetDisplay("Close Shorts", "Allow closing shorts on opposite colors", "Trading");

		_normalVolume = Param(nameof(NormalVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Normal Volume", "Default order size", "Risk");

		_reducedVolume = Param(nameof(ReducedVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Reduced Volume", "Order size after losses", "Risk");

		_lossTriggerCount = Param(nameof(LossTriggerCount), 2)
		.SetNotNegative()
		.SetDisplay("Loss Trigger", "Number of losses before reducing size", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetNotNegative()
		.SetDisplay("Stop Loss Points", "Protective stop distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetNotNegative()
		.SetDisplay("Take Profit Points", "Target distance in price steps", "Risk");
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

		_highest = new Highest { Length = KijunLength };
		_lowest = new Lowest { Length = KijunLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		var time = candle.CloseTime ?? candle.OpenTime;

		var highestValue = _highest.Process(new DecimalIndicatorValue(_highest, candle.HighPrice, time));
		var lowestValue = _lowest.Process(new DecimalIndicatorValue(_lowest, candle.LowPrice, time));

		if (!highestValue.IsFinal || !lowestValue.IsFinal)
		{
			return;
		}

		var highest = highestValue.ToDecimal();
		var lowest = lowestValue.ToDecimal();
		var kijun = (highest + lowest) / 2m;

		var color = CalculateColor(candle, kijun);
		UpdateColorHistory(color);

		HandleRiskManagement(candle);

		var signalColor = GetColorByOffset(SignalBarShift);
		var confirmColor = GetColorByOffset(SignalBarShift + 1);

		var buySignal = confirmColor.HasValue && confirmColor.Value > 1 && signalColor.HasValue && signalColor.Value < 2;
		var sellSignal = confirmColor.HasValue && confirmColor.Value < 2 && signalColor.HasValue && signalColor.Value > 1;

		if (buySignal)
		{
			if (SellPosClose)
			CloseShortIfNeeded(candle, candle.ClosePrice, "color reversal to bullish");

			if (BuyPosOpen)
			OpenLongIfNeeded(candle.ClosePrice);
		}

		if (sellSignal)
		{
			if (BuyPosClose)
			CloseLongIfNeeded(candle, candle.ClosePrice, "color reversal to bearish");

			if (SellPosOpen)
			OpenShortIfNeeded(candle.ClosePrice);
		}
	}

	private void HandleRiskManagement(ICandleMessage candle)
	{
		var stopDistance = GetPriceDistance(StopLossPoints);
		var takeDistance = GetPriceDistance(TakeProfitPoints);

		if (Position > 0 && _longEntryPrice.HasValue)
		{
			var entry = _longEntryPrice.Value;

			if (stopDistance > 0m && candle.LowPrice <= entry - stopDistance)
			{
				CloseLongIfNeeded(candle, entry - stopDistance, "stop loss");
			}
			else if (takeDistance > 0m && candle.HighPrice >= entry + takeDistance)
			{
				CloseLongIfNeeded(candle, entry + takeDistance, "take profit");
			}
		}
		else if (Position < 0 && _shortEntryPrice.HasValue)
		{
			var entry = _shortEntryPrice.Value;

			if (stopDistance > 0m && candle.HighPrice >= entry + stopDistance)
			{
				CloseShortIfNeeded(candle, entry + stopDistance, "stop loss");
			}
			else if (takeDistance > 0m && candle.LowPrice <= entry - takeDistance)
			{
				CloseShortIfNeeded(candle, entry - takeDistance, "take profit");
			}
		}
	}

	private void OpenLongIfNeeded(decimal price)
	{
		if (Position != 0)
		{
			return;
		}

		var volume = GetOrderVolume(true);
		if (volume <= 0m)
		{
			return;
		}

		BuyMarket(volume);
		_longEntryPrice = price;
		_shortEntryPrice = null;

		LogInfo($"Opened long at {price} with volume {volume}.");
	}

	private void OpenShortIfNeeded(decimal price)
	{
		if (Position != 0)
		{
			return;
		}

		var volume = GetOrderVolume(false);
		if (volume <= 0m)
		{
			return;
		}

		SellMarket(volume);
		_shortEntryPrice = price;
		_longEntryPrice = null;

		LogInfo($"Opened short at {price} with volume {volume}.");
	}

	private void CloseLongIfNeeded(ICandleMessage candle, decimal exitPrice, string reason)
	{
		if (Position <= 0)
		{
			return;
		}

		var volume = Position;
		if (volume <= 0m)
		{
			return;
		}

		SellMarket(volume);

		if (_longEntryPrice.HasValue)
		{
			RecordTradeResult(true, _longEntryPrice.Value, exitPrice);
		}

		_longEntryPrice = null;

		LogInfo($"Closed long at {exitPrice} due to {reason} on {candle.CloseTime ?? candle.OpenTime}.");
	}

	private void CloseShortIfNeeded(ICandleMessage candle, decimal exitPrice, string reason)
	{
		if (Position >= 0)
		{
			return;
		}

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		{
			return;
		}

		BuyMarket(volume);

		if (_shortEntryPrice.HasValue)
		{
			RecordTradeResult(false, _shortEntryPrice.Value, exitPrice);
		}

		_shortEntryPrice = null;

		LogInfo($"Closed short at {exitPrice} due to {reason} on {candle.CloseTime ?? candle.OpenTime}.");
	}

	private decimal GetOrderVolume(bool isBuy)
	{
		var normal = NormalVolume;
		if (normal <= 0m)
		{
			return 0m;
		}

		var reduced = ReducedVolume > 0m ? ReducedVolume : normal;

		if (LossTriggerCount <= 0)
		{
			return normal;
		}

		var queue = isBuy ? _buyLossHistory : _sellLossHistory;
		if (queue.Count < LossTriggerCount)
		{
			return normal;
		}

		foreach (var loss in queue)
		{
			if (!loss)
			{
				return normal;
			}
		}

		return reduced;
	}

	private void RecordTradeResult(bool isBuy, decimal entryPrice, decimal exitPrice)
	{
		var profit = isBuy ? exitPrice - entryPrice : entryPrice - exitPrice;
		var isLoss = profit < 0m;
		var queue = isBuy ? _buyLossHistory : _sellLossHistory;

		queue.Enqueue(isLoss);
		TrimQueue(queue);
	}

	private void TrimQueue(Queue<bool> queue)
	{
		if (LossTriggerCount <= 0)
		{
			queue.Clear();
			return;
		}

		while (queue.Count > LossTriggerCount)
		{
			queue.Dequeue();
		}
	}

	private int CalculateColor(ICandleMessage candle, decimal kijun)
	{
		if (candle.ClosePrice > kijun)
		return candle.ClosePrice >= candle.OpenPrice ? 3 : 2;

		if (candle.ClosePrice < kijun)
		return candle.ClosePrice <= candle.OpenPrice ? 0 : 1;

		if (_colorHistory.Count > 0)
		return _colorHistory[_colorHistory.Count - 1];

		return candle.ClosePrice >= candle.OpenPrice ? 3 : 0;
	}

	private void UpdateColorHistory(int color)
	{
		_colorHistory.Add(color);

		var maxHistory = Math.Max(10, SignalBarShift + 3);
		if (_colorHistory.Count > maxHistory)
		{
			_colorHistory.RemoveRange(0, _colorHistory.Count - maxHistory);
		}
	}

	private int? GetColorByOffset(int offset)
	{
		if (offset < 0)
		{
			return null;
		}

		var index = _colorHistory.Count - 1 - offset;
		if (index < 0)
		{
			return null;
		}

		return _colorHistory[index];
	}

	private decimal GetPriceDistance(int points)
	{
		if (points <= 0)
		{
			return 0m;
		}

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			return 0m;
		}

		return step * points;
	}
}