using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-currency Bollinger Band breakout strategy converted from the MQL MultiCurrEA expert advisor.
/// For each configured symbol the strategy looks for touches of the Bollinger Band envelopes
/// calculated on high and low prices and scales volume based on free margin usage.
/// </summary>
public class MultiCurrEAStrategy : Strategy
{
	private sealed class SymbolSlot
	{
		private readonly string _groupName;

		public SymbolSlot(MultiCurrEAStrategy owner, int index, string defaultId, int defaultPeriod, decimal defaultDeviation)
		{
			_groupName = $"Symbol {index + 1}";
			var symbolLabel = $"{_groupName} Security";
			var enabledLabel = $"{_groupName} Enabled";
			var periodLabel = $"{_groupName} Bollinger Period";
			var shiftLabel = $"{_groupName} Bollinger Shift";
			var deviationLabel = $"{_groupName} Bollinger Deviation";
			var timeframeLabel = $"{_groupName} Timeframe";

			SecurityParam = owner.Param<Security>($"Symbol{index}Security", new Security { Id = defaultId })
			.SetDisplay(symbolLabel, "Trading instrument for this slot", _groupName);

			IsEnabledParam = owner.Param($"Symbol{index}Enabled", true)
			.SetDisplay(enabledLabel, "Allow trading for this symbol", _groupName);

			CandleTypeParam = owner.Param($"Symbol{index}CandleType", TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay(timeframeLabel, "Candle timeframe used for indicator calculations", _groupName);

			BollingerPeriodParam = owner.Param($"Symbol{index}BollingerPeriod", defaultPeriod)
			.SetGreaterThanZero()
			.SetDisplay(periodLabel, "Number of candles used by Bollinger Bands", _groupName)
			.SetCanOptimize(true);

			BollingerShiftParam = owner.Param($"Symbol{index}BollingerShift", 0)
			.SetGreaterOrEqualZero()
			.SetDisplay(shiftLabel, "Number of completed bars to offset indicator values", _groupName);

			BollingerDeviationParam = owner.Param($"Symbol{index}BollingerDeviation", defaultDeviation)
			.SetGreaterThanZero()
			.SetDisplay(deviationLabel, "Standard deviation multiplier for Bollinger Bands", _groupName)
			.SetCanOptimize(true);
		}

		public StrategyParam<Security> SecurityParam { get; }
		public StrategyParam<bool> IsEnabledParam { get; }
		public StrategyParam<DataType> CandleTypeParam { get; }
		public StrategyParam<int> BollingerPeriodParam { get; }
		public StrategyParam<int> BollingerShiftParam { get; }
		public StrategyParam<decimal> BollingerDeviationParam { get; }

		public Security Security => SecurityParam.Value;
		public bool IsEnabled => IsEnabledParam.Value;
		public DataType CandleType => CandleTypeParam.Value;
		public int BollingerPeriod => BollingerPeriodParam.Value;
		public int BollingerShift => BollingerShiftParam.Value;
		public decimal BollingerDeviation => BollingerDeviationParam.Value;

		public BollingerBands BollingerHigh { get; private set; } = null!;
		public BollingerBands BollingerLow { get; private set; } = null!;

		public readonly List<decimal> UpperHighBuffer = new();
		public readonly List<decimal> LowerHighBuffer = new();
		public readonly List<decimal> UpperLowBuffer = new();
		public readonly List<decimal> LowerLowBuffer = new();

		public decimal LastBid;
		public decimal LastAsk;
		public decimal LastPrice;
		public int DealNumber;
		public DateTimeOffset LockedBarTime;

		public void ResetRuntime()
		{
			UpperHighBuffer.Clear();
			LowerHighBuffer.Clear();
			UpperLowBuffer.Clear();
			LowerLowBuffer.Clear();
			LastBid = 0m;
			LastAsk = 0m;
			LastPrice = 0m;
			DealNumber = 0;
			LockedBarTime = DateTimeOffset.MinValue;
		}

		public void CreateIndicators()
		{
			BollingerHigh = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerDeviation
			};

			BollingerLow = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerDeviation
			};
		}
	}

	private readonly SymbolSlot[] _symbols;
	private readonly StrategyParam<decimal> _dealOfFreeMargin;
	private readonly StrategyParam<decimal> _lotIncrease;

	/// <summary>
	/// Percentage of portfolio equity to allocate for each new deal.
	/// </summary>
	public decimal DealOfFreeMargin
	{
		get => _dealOfFreeMargin.Value;
		set => _dealOfFreeMargin.Value = value;
	}

	/// <summary>
	/// Multiplier applied when increasing lot size after the first deal.
	/// </summary>
	public decimal LotIncrease
	{
		get => _lotIncrease.Value;
		set => _lotIncrease.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MultiCurrEAStrategy"/>.
	/// </summary>
	public MultiCurrEAStrategy()
	{
		_symbols =
		[
		new SymbolSlot(this, 0, "EURUSD", 20, 2.0m),
		new SymbolSlot(this, 1, "GBPUSD", 18, 1.8m),
		new SymbolSlot(this, 2, "USDJPY", 21, 2.1m)
		];

		_dealOfFreeMargin = Param(nameof(DealOfFreeMargin), 1.0m)
		.SetGreaterThanZero()
		.SetDisplay("Deal % of Equity", "Percent of equity allocated per entry", "Risk Management");

		_lotIncrease = Param(nameof(LotIncrease), 0.9m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Increase", "Multiplier applied to additional deals", "Risk Management");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var slot in _symbols)
		{
			if (!slot.IsEnabled || slot.Security == null)
			continue;

			yield return (slot.Security, slot.CandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		foreach (var slot in _symbols)
		{
			slot.ResetRuntime();
		}
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		foreach (var slot in _symbols)
		{
			slot.ResetRuntime();

			if (!slot.IsEnabled)
			continue;

			if (slot.Security == null)
			throw new InvalidOperationException($"Security is not set for {slot.SecurityParam.Name}.");

			slot.CreateIndicators();

			SubscribeCandles(slot.CandleType, false, slot.Security)
			.Bind(candle => ProcessSymbolCandle(candle, slot))
			.Start();

			SubscribeLevel1(slot.Security)
			.Bind(message => UpdateBidAsk(slot, message))
			.Start();
		}
	}

	private void UpdateBidAsk(SymbolSlot slot, Level1ChangeMessage message)
	{
		var bid = message.TryGetDecimal(Level1Fields.BestBidPrice);
		if (bid.HasValue)
		slot.LastBid = bid.Value;

		var ask = message.TryGetDecimal(Level1Fields.BestAskPrice);
		if (ask.HasValue)
		slot.LastAsk = ask.Value;

		var last = message.TryGetDecimal(Level1Fields.LastTradePrice);
		if (last.HasValue)
		slot.LastPrice = last.Value;
	}

	private void ProcessSymbolCandle(ICandleMessage candle, SymbolSlot slot)
	{
		if (candle.State != CandleStates.Finished)
		return;

		slot.LastPrice = candle.ClosePrice;

		var time = candle.OpenTime;

		var highValue = slot.BollingerHigh.Process(candle.HighPrice, time, true);
		var lowValue = slot.BollingerLow.Process(candle.LowPrice, time, true);

		if (!slot.BollingerHigh.IsFormed || !slot.BollingerLow.IsFormed)
		return;

		if (highValue is not BollingerBandsValue highBands)
		return;

		if (lowValue is not BollingerBandsValue lowBands)
		return;

		if (highBands.UpBand is not decimal upperHigh || highBands.LowBand is not decimal lowerHigh)
		return;

		if (lowBands.UpBand is not decimal upperLow || lowBands.LowBand is not decimal lowerLow)
		return;

		UpdateBuffer(slot.UpperHighBuffer, upperHigh, slot.BollingerShift + 1);
		UpdateBuffer(slot.LowerHighBuffer, lowerHigh, slot.BollingerShift + 1);
		UpdateBuffer(slot.UpperLowBuffer, upperLow, slot.BollingerShift + 1);
		UpdateBuffer(slot.LowerLowBuffer, lowerLow, slot.BollingerShift + 1);

		if (!HasEnoughData(slot.UpperHighBuffer, slot.BollingerShift) ||
		!HasEnoughData(slot.LowerHighBuffer, slot.BollingerShift) ||
		!HasEnoughData(slot.UpperLowBuffer, slot.BollingerShift) ||
		!HasEnoughData(slot.LowerLowBuffer, slot.BollingerShift))
		{
			return;
		}

		var shiftedUpperHigh = GetShiftedValue(slot.UpperHighBuffer, slot.BollingerShift);
		var shiftedLowerHigh = GetShiftedValue(slot.LowerHighBuffer, slot.BollingerShift);
		var shiftedUpperLow = GetShiftedValue(slot.UpperLowBuffer, slot.BollingerShift);
		var shiftedLowerLow = GetShiftedValue(slot.LowerLowBuffer, slot.BollingerShift);

		var bid = slot.LastBid > 0m ? slot.LastBid : slot.LastPrice;
		var ask = slot.LastAsk > 0m ? slot.LastAsk : slot.LastPrice;

		if (bid <= 0m || ask <= 0m)
		return;

		var position = GetPositionValue(slot.Security, Portfolio) ?? 0m;

		if (position > 0m)
		{
			if (bid >= shiftedLowerHigh || slot.DealNumber == 0)
			{
				SendMarketOrder(slot.Security, Sides.Sell, position, "MultiCurrEA Exit Long");
				slot.DealNumber = 0;
				return;
			}
		}
		else if (position < 0m)
		{
			if (ask <= shiftedUpperLow || slot.DealNumber == 0)
			{
				SendMarketOrder(slot.Security, Sides.Buy, Math.Abs(position), "MultiCurrEA Exit Short");
				slot.DealNumber = 0;
				return;
			}
		}

		if (bid >= shiftedLowerHigh && ask <= shiftedUpperLow)
		{
			slot.DealNumber = 0;
			return;
		}

		if (slot.LockedBarTime >= candle.OpenTime)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (ask <= shiftedLowerLow)
		{
			slot.DealNumber++;
			var volume = CalculateOrderVolume(slot, ask);
			if (volume <= 0m)
			{
				slot.DealNumber--;
				return;
			}

			SendMarketOrder(slot.Security, Sides.Buy, volume, "MultiCurrEA Buy");
			slot.LockedBarTime = candle.OpenTime;
			return;
		}

		if (bid >= shiftedUpperHigh)
		{
			slot.DealNumber++;
			var volume = CalculateOrderVolume(slot, bid);
			if (volume <= 0m)
			{
				slot.DealNumber--;
				return;
			}

			SendMarketOrder(slot.Security, Sides.Sell, volume, "MultiCurrEA Sell");
			slot.LockedBarTime = candle.OpenTime;
		}
	}

	private static void UpdateBuffer(List<decimal> buffer, decimal value, int maxCount)
	{
		buffer.Add(value);

		var limit = Math.Max(1, maxCount);
		while (buffer.Count > limit)
		{
			buffer.RemoveAt(0);
		}
	}

	private static bool HasEnoughData(List<decimal> buffer, int shift)
	{
		return buffer.Count > shift;
	}

	private static decimal GetShiftedValue(List<decimal> buffer, int shift)
	{
		var index = buffer.Count - 1 - shift;
		return buffer[index];
	}

	private decimal CalculateOrderVolume(SymbolSlot slot, decimal price)
	{
		if (price <= 0m)
		return 0m;

		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		if (portfolioValue <= 0m)
		portfolioValue = Portfolio?.BeginValue ?? 0m;

		if (portfolioValue <= 0m)
		return Volume;

		var baseVolume = portfolioValue * (DealOfFreeMargin * 0.01m) / price;
		if (baseVolume <= 0m)
		return 0m;

		if (slot.DealNumber > 1)
		{
			baseVolume *= LotIncrease * (slot.DealNumber - 1);
		}

		var security = slot.Security;
		if (security == null)
		return 0m;

		var minVolume = security.MinVolume ?? security.StepVolume ?? 0m;
		var maxVolume = security.MaxVolume ?? decimal.MaxValue;
		var step = security.StepVolume ?? 0m;

		if (step > 0m)
		{
			baseVolume = Math.Round(baseVolume / step, MidpointRounding.AwayFromZero) * step;
		}
		else
		{
			baseVolume = Math.Round(baseVolume, 2, MidpointRounding.AwayFromZero);
		}

		if (minVolume > 0m && baseVolume < minVolume)
		baseVolume = minVolume;

		if (baseVolume > maxVolume)
		baseVolume = maxVolume;

		return baseVolume;
	}

	private void SendMarketOrder(Security security, Sides side, decimal volume, string comment)
	{
		if (volume <= 0m)
		return;

		RegisterOrder(new Order
		{
			Security = security,
			Portfolio = Portfolio,
			Side = side,
			Volume = volume,
			Type = OrderTypes.Market,
			Comment = comment
		});
	}
}
