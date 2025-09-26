using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Resonance Hunter strategy converted from the MetaTrader Exp_ResonanceHunter expert advisor.
/// The strategy monitors three correlated Stochastic oscillators and opens trades when all of them
/// point in the same direction while closing positions once the leading instrument flips.
/// </summary>
public class ResonanceHunterStrategy : Strategy
{
	private sealed class SlotInstrument
	{
		public SlotInstrument(StrategyParam<Security> param)
		{
			SecurityParam = param;
		}

		public StrategyParam<Security> SecurityParam { get; }

		public Security Security => SecurityParam.Value;

		public StochasticOscillator Stochastic { get; set; }
		public DateTimeOffset? LastTime { get; set; }
		public decimal? LastK { get; set; }
		public decimal? LastD { get; set; }
		public ICandleMessage LastCandle { get; set; }

		public void Reset()
		{
			LastTime = null;
			LastK = null;
			LastD = null;
			LastCandle = null;
		}
	}

	private sealed class ResonanceSlot
	{
		private readonly string _groupName;

		public ResonanceSlot(ResonanceHunterStrategy owner, int index, string defaultA, string defaultB, string defaultC)
		{
			_groupName = $"Slot {index + 1}";
			var enabledLabel = $"{_groupName} Enabled";
			var candleLabel = $"{_groupName} Candle Type";
			var primaryLabel = $"{_groupName} Primary";
			var secondaryLabel = $"{_groupName} Secondary";
			var confirmLabel = $"{_groupName} Confirmation";
			var volumeLabel = $"{_groupName} Volume";
			var stopLabel = $"{_groupName} Stop Loss";
			var kLabel = $"{_groupName} K Period";
			var dLabel = $"{_groupName} D Period";
			var slowingLabel = $"{_groupName} Slowing";

			EnabledParam = owner.Param($"Slot{index}Enabled", true)
				.SetDisplay(enabledLabel, "Allow trading for this slot", _groupName);

			PrimaryParam = owner.Param($"Slot{index}PrimarySecurity", new Security { Id = defaultA })
				.SetDisplay(primaryLabel, "Instrument used for trading and exit signals", _groupName);

			SecondaryParam = owner.Param($"Slot{index}SecondarySecurity", new Security { Id = defaultB })
				.SetDisplay(secondaryLabel, "Second instrument participating in the resonance calculation", _groupName);

			ConfirmParam = owner.Param($"Slot{index}ConfirmSecurity", new Security { Id = defaultC })
				.SetDisplay(confirmLabel, "Third instrument participating in the resonance calculation", _groupName);

			CandleTypeParam = owner.Param($"Slot{index}CandleType", TimeSpan.FromHours(1).TimeFrame())
				.SetDisplay(candleLabel, "Timeframe applied to all three instruments", _groupName);

			KPeriodParam = owner.Param($"Slot{index}KPeriod", 5)
				.SetGreaterThanZero()
				.SetDisplay(kLabel, "Base calculation window for the Stochastic %K", _groupName)
				.SetCanOptimize(true);

			DPeriodParam = owner.Param($"Slot{index}DPeriod", 3)
				.SetGreaterThanZero()
				.SetDisplay(dLabel, "Smoothing period for the Stochastic %D", _groupName)
				.SetCanOptimize(true);

			SlowingParam = owner.Param($"Slot{index}Slowing", 3)
				.SetGreaterThanZero()
				.SetDisplay(slowingLabel, "Additional smoothing for the %K line", _groupName)
				.SetCanOptimize(true);

			VolumeParam = owner.Param($"Slot{index}Volume", 0.1m)
				.SetGreaterThanZero()
				.SetDisplay(volumeLabel, "Order volume expressed in lots", _groupName);

			StopLossPointsParam = owner.Param($"Slot{index}StopLossPoints", 500m)
				.SetNotNegative()
				.SetDisplay(stopLabel, "Stop loss distance in MetaTrader points (0 disables the stop)", _groupName);

			Instruments =
			[
				new SlotInstrument(PrimaryParam),
				new SlotInstrument(SecondaryParam),
				new SlotInstrument(ConfirmParam)
			];
		}

		public StrategyParam<bool> EnabledParam { get; }
		public StrategyParam<Security> PrimaryParam { get; }
		public StrategyParam<Security> SecondaryParam { get; }
		public StrategyParam<Security> ConfirmParam { get; }
		public StrategyParam<DataType> CandleTypeParam { get; }
		public StrategyParam<int> KPeriodParam { get; }
		public StrategyParam<int> DPeriodParam { get; }
		public StrategyParam<int> SlowingParam { get; }
		public StrategyParam<decimal> VolumeParam { get; }
		public StrategyParam<decimal> StopLossPointsParam { get; }

		public bool Enabled => EnabledParam.Value;
		public SlotInstrument PrimaryInstrument => Instruments[0];
		public IEnumerable<SlotInstrument> AllInstruments => Instruments;
		public DataType CandleType => CandleTypeParam.Value;
		public int KPeriod => KPeriodParam.Value;
		public int DPeriod => DPeriodParam.Value;
		public int Slowing => SlowingParam.Value;
		public decimal Volume => VolumeParam.Value;
		public decimal StopLossPoints => StopLossPointsParam.Value;

		public SlotInstrument[] Instruments { get; }
		public DateTimeOffset? LastProcessedTime { get; set; }
		public bool LongSignal { get; set; }
		public bool ShortSignal { get; set; }
		public bool LongExit { get; set; }
		public bool ShortExit { get; set; }
		public decimal? ActiveStopPrice { get; set; }
		public Sides? ActiveSide { get; set; }

		public void Reset()
		{
			foreach (var instrument in Instruments)
			{
				instrument.Reset();
			}

			LastProcessedTime = null;
			LongSignal = false;
			ShortSignal = false;
			LongExit = false;
			ShortExit = false;
			ActiveStopPrice = null;
			ActiveSide = null;
		}

		public bool HasAlignedData()
		{
			if (Instruments.Any(i => i.LastTime == null || i.LastK == null || i.LastD == null))
				return false;

			var reference = Instruments[0].LastTime;

			return Instruments.All(i => i.LastTime == reference);
		}

		public void ClearSignals()
		{
			LongSignal = false;
			ShortSignal = false;
			LongExit = false;
			ShortExit = false;
		}
	}

	private readonly ResonanceSlot[] _slots;

	/// <summary>
	/// Initializes a new instance of <see cref="ResonanceHunterStrategy"/>.
	/// </summary>
	public ResonanceHunterStrategy()
	{
		_slots =
		[
			new ResonanceSlot(this, 0, "EURUSD", "EURJPY", "USDJPY"),
			new ResonanceSlot(this, 1, "GBPUSD", "GBPJPY", "USDJPY"),
			new ResonanceSlot(this, 2, "AUDUSD", "AUDJPY", "USDJPY")
		];
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var slot in _slots)
		{
			if (!slot.Enabled)
				continue;

			foreach (var instrument in slot.AllInstruments)
			{
				if (instrument.Security != null)
					yield return (instrument.Security, slot.CandleType);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		foreach (var slot in _slots)
		{
			slot.Reset();
		}
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		foreach (var slot in _slots)
		{
			slot.Reset();

			if (!slot.Enabled)
				continue;

			if (slot.AllInstruments.Any(i => i.Security == null))
				throw new InvalidOperationException($"Security is not set for group {slot.PrimaryParam.Name}.");

			for (var index = 0; index < slot.Instruments.Length; index++)
			{
				var instrument = slot.Instruments[index];

				instrument.Stochastic = new StochasticOscillator
				{
					KPeriod = slot.KPeriod,
					DPeriod = slot.DPeriod,
					Slowing = slot.Slowing
				};

				var subscription = SubscribeCandles(slot.CandleType, false, instrument.Security);

				var capturedSlot = slot;
				var capturedIndex = index;

				subscription
					.BindEx(instrument.Stochastic, (candle, value) => ProcessSlotCandle(capturedSlot, capturedIndex, candle, value))
					.Start();
			}
		}
	}

	private void ProcessSlotCandle(ResonanceSlot slot, int instrumentIndex, ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!indicatorValue.IsFinal)
			return;

		var instrument = slot.Instruments[instrumentIndex];
		instrument.LastTime = candle.OpenTime;
		instrument.LastCandle = candle;

		var stochasticValue = indicatorValue as StochasticOscillatorValue;
		if (stochasticValue?.K is not decimal k || stochasticValue.D is not decimal d)
			return;

		instrument.LastK = k;
		instrument.LastD = d;

		if (instrumentIndex == 0)
			TryApplyManualStop(slot, candle);

		if (!slot.HasAlignedData())
			return;

		var time = instrument.LastTime!.Value;

		if (slot.LastProcessedTime.HasValue && slot.LastProcessedTime.Value == time)
			return;

		EvaluateSignals(slot);
		slot.LastProcessedTime = time;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ExecuteSlotSignals(slot);
	}

	private void EvaluateSignals(ResonanceSlot slot)
	{
		slot.ClearSignals();

		var diffs = new decimal[3];
		var ups = new bool[3];
		var dns = new bool[3];

		for (var i = 0; i < slot.Instruments.Length; i++)
		{
			var instrument = slot.Instruments[i];
			var diff = (instrument.LastK ?? 0m) - (instrument.LastD ?? 0m);
			diff = Math.Round(diff, 4, MidpointRounding.AwayFromZero);

			diffs[i] = diff;
			ups[i] = diff > 0m;
			dns[i] = diff < 0m;
		}

		if (ups[0] && ups[1] && dns[2] && Math.Abs(diffs[1]) > Math.Abs(diffs[2]))
			ups[2] = true;
		if (ups[0] && ups[2] && dns[1] && Math.Abs(diffs[2]) > Math.Abs(diffs[1]))
			ups[1] = true;

		if (dns[0] && dns[1] && ups[2] && Math.Abs(diffs[1]) > Math.Abs(diffs[2]))
			dns[2] = true;
		if (dns[0] && dns[2] && ups[1] && Math.Abs(diffs[2]) > Math.Abs(diffs[1]))
			dns[1] = true;

		if (ups[0] && ups[1] && !dns[2])
			ups[2] = true;
		if (ups[0] && ups[2] && !dns[1])
			ups[1] = true;

		if (dns[0] && dns[1] && !ups[2])
			dns[2] = true;
		if (dns[0] && dns[2] && !ups[1])
			dns[1] = true;

		if (ups[1] && ups[2] && !dns[0])
			ups[0] = true;
		if (dns[1] && dns[2] && !ups[0])
			dns[0] = true;

		if (ups[0] && ups[1] && ups[2])
			slot.LongSignal = true;

		if (dns[0] && dns[1] && dns[2])
			slot.ShortSignal = true;

		slot.LongExit = dns[0];
		slot.ShortExit = ups[0];
	}

	private void ExecuteSlotSignals(ResonanceSlot slot)
	{
		var security = slot.PrimaryInstrument.Security;
		if (security == null)
			return;

		var position = GetPositionValue(security, Portfolio) ?? 0m;

		if (slot.LongExit && position > 0m)
		{
			SellMarket(position, security);
			slot.ActiveSide = null;
			slot.ActiveStopPrice = null;
		}

		if (slot.ShortExit && position < 0m)
		{
			BuyMarket(Math.Abs(position), security);
			slot.ActiveSide = null;
			slot.ActiveStopPrice = null;
		}

		if (slot.LongSignal)
		{
			var volume = slot.Volume;
			if (position < 0m)
				volume += Math.Abs(position);

			if (volume > 0m && position <= 0m)
			{
				BuyMarket(volume, security);
				ApplyStopTemplate(slot, Sides.Buy);
			}
		}

		if (slot.ShortSignal)
		{
			var volume = slot.Volume;
			if (position > 0m)
				volume += position;

			if (volume > 0m && position >= 0m)
			{
				SellMarket(volume, security);
				ApplyStopTemplate(slot, Sides.Sell);
			}
		}

		slot.ClearSignals();
	}

	private void ApplyStopTemplate(ResonanceSlot slot, Sides side)
	{
		var security = slot.PrimaryInstrument.Security;
		var candle = slot.PrimaryInstrument.LastCandle;
		if (security == null || candle == null)
			return;

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			slot.ActiveSide = side;
			slot.ActiveStopPrice = null;
			return;
		}

		var distance = slot.StopLossPoints * priceStep;
		if (distance <= 0m)
		{
			slot.ActiveSide = side;
			slot.ActiveStopPrice = null;
			return;
		}

		decimal stopPrice;

		if (side == Sides.Buy)
			stopPrice = candle.ClosePrice - distance;
		else
			stopPrice = candle.ClosePrice + distance;

		slot.ActiveSide = side;
		slot.ActiveStopPrice = stopPrice;
	}

	private void TryApplyManualStop(ResonanceSlot slot, ICandleMessage candle)
	{
		var security = slot.PrimaryInstrument.Security;
		if (security == null)
			return;

		var stopPrice = slot.ActiveStopPrice;
		var side = slot.ActiveSide;
		if (stopPrice == null || side == null)
			return;

		var position = GetPositionValue(security, Portfolio) ?? 0m;

		if (position == 0m)
		{
			slot.ActiveSide = null;
			slot.ActiveStopPrice = null;
			return;
		}

		if (side == Sides.Buy)
		{
			if (position <= 0m)
			{
				slot.ActiveSide = null;
				slot.ActiveStopPrice = null;
				return;
			}

			if (candle.LowPrice <= stopPrice)
			{
				SellMarket(position, security);
				slot.ActiveSide = null;
				slot.ActiveStopPrice = null;
			}
		}
		else if (side == Sides.Sell)
		{
			if (position >= 0m)
			{
				slot.ActiveSide = null;
				slot.ActiveStopPrice = null;
				return;
			}

			if (candle.HighPrice >= stopPrice)
			{
				BuyMarket(Math.Abs(position), security);
				slot.ActiveSide = null;
				slot.ActiveStopPrice = null;
			}
		}
	}
}
