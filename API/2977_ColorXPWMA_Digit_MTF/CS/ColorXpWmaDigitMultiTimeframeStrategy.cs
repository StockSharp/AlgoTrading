using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MT5 expert "Exp_ColorXPWMA_Digit_NN3_MMRec" with three ColorXPWMA modules.
/// </summary>
public class ColorXpWmaDigitMultiTimeframeStrategy : Strategy
{
	private readonly TimeframeSettings _timeframeA;
	private readonly TimeframeSettings _timeframeB;
	private readonly TimeframeSettings _timeframeC;

	private readonly Dictionary<string, TimeframeProcessor> _processorsByKey = new();
	private PendingOrderContext? _pendingOrder;

	public ColorXpWmaDigitMultiTimeframeStrategy()
	{
		_timeframeA = new TimeframeSettings(this, "A",
		TimeSpan.FromHours(8).TimeFrame(),
		period: 1,
		power: 2.00001m,
		smoothMethod: SmoothMethod.Sma,
		smoothLength: 5,
		smoothPhase: 15,
		appliedPrice: AppliedPrice.Close,
		digit: 2,
		signalBar: 1,
		buyMagic: 777,
		sellMagic: 888,
		buyTotalTrigger: 5,
		buyLossTrigger: 3,
		sellTotalTrigger: 5,
		sellLossTrigger: 3,
		smallMoneyManagement: 0.01m,
		normalMoneyManagement: 0.1m,
		marginMode: MarginMode.Lot,
		stopLossTicks: 3000m,
		takeProfitTicks: 10000m,
		deviationTicks: 10m,
		buyOpenAllowed: true,
		sellOpenAllowed: true,
		sellCloseAllowed: true,
		buyCloseAllowed: true);

		_timeframeB = new TimeframeSettings(this, "B",
		TimeSpan.FromHours(4).TimeFrame(),
		period: 1,
		power: 2.00001m,
		smoothMethod: SmoothMethod.Sma,
		smoothLength: 5,
		smoothPhase: 15,
		appliedPrice: AppliedPrice.Close,
		digit: 2,
		signalBar: 1,
		buyMagic: 555,
		sellMagic: 444,
		buyTotalTrigger: 5,
		buyLossTrigger: 3,
		sellTotalTrigger: 5,
		sellLossTrigger: 3,
		smallMoneyManagement: 0.01m,
		normalMoneyManagement: 0.1m,
		marginMode: MarginMode.Lot,
		stopLossTicks: 2000m,
		takeProfitTicks: 6000m,
		deviationTicks: 10m,
		buyOpenAllowed: true,
		sellOpenAllowed: true,
		sellCloseAllowed: true,
		buyCloseAllowed: true);

		_timeframeC = new TimeframeSettings(this, "C",
		TimeSpan.FromHours(1).TimeFrame(),
		period: 1,
		power: 2.00001m,
		smoothMethod: SmoothMethod.Sma,
		smoothLength: 5,
		smoothPhase: 15,
		appliedPrice: AppliedPrice.Close,
		digit: 2,
		signalBar: 1,
		buyMagic: 222,
		sellMagic: 111,
		buyTotalTrigger: 5,
		buyLossTrigger: 3,
		sellTotalTrigger: 5,
		sellLossTrigger: 3,
		smallMoneyManagement: 0.01m,
		normalMoneyManagement: 0.1m,
		marginMode: MarginMode.Lot,
		stopLossTicks: 1000m,
		takeProfitTicks: 3000m,
		deviationTicks: 10m,
		buyOpenAllowed: true,
		sellOpenAllowed: true,
		sellCloseAllowed: true,
		buyCloseAllowed: true);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var seen = new HashSet<DataType>();

		foreach (var dataType in new[]
		{
			_timeframeA.CandleType,
			_timeframeB.CandleType,
			_timeframeC.CandleType
		})
		{
			if (seen.Add(dataType))
			yield return (Security, dataType);
		}
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_processorsByKey.Clear();
		_pendingOrder = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializeProcessor(_timeframeA);
		InitializeProcessor(_timeframeB);
		InitializeProcessor(_timeframeC);
	}

	private void InitializeProcessor(TimeframeSettings settings)
	{
		var processor = new TimeframeProcessor(this, settings);
		_processorsByKey[settings.Key] = processor;

		var subscription = SubscribeCandles(settings.CandleType);
		subscription
		.Bind(processor.ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	protected override void OnOrderRegistering(Order order)
	{
		if (_pendingOrder is { } context)
		{
			order.Comment = context.Comment;
		}

		base.OnOrderRegistering(order);
	}

	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var comment = trade.Order.Comment;
		if (comment.IsEmpty())
		return;

		if (!TryParseComment(comment, out var processor, out var action))
		return;

		processor.ProcessTrade(trade, action);
	}

	private bool TryParseComment(string comment, out TimeframeProcessor processor, out TradeAction action)
	{
		processor = default!;
		action = default;

		var parts = comment.Split('|');
		if (parts.Length != 2)
		return false;

		if (!_processorsByKey.TryGetValue(parts[0], out processor))
		return false;

		return Enum.TryParse(parts[1], out action);
	}

	private void PlaceOrder(TimeframeProcessor processor, TradeAction action, decimal volume)
	{
		if (volume <= 0m)
		return;

		_pendingOrder = new PendingOrderContext(processor.Settings.Key, action);

		try
		{
			switch (action)
			{
				case TradeAction.BuyOpen:
					BuyMarket(volume);
					break;
				case TradeAction.BuyClose:
					SellMarket(volume);
					break;
				case TradeAction.SellOpen:
					SellMarket(volume);
					break;
				case TradeAction.SellClose:
					BuyMarket(volume);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown trade action.");
				}
			}
			finally
			{
				_pendingOrder = null;
			}
		}

		private void HandleRiskManagement(TimeframeProcessor processor, ICandleMessage candle)
		{
			var step = Security?.PriceStep ?? 1m;

			if (processor.CanEvaluateLongRisk)
			{
				var stopDistance = processor.Settings.StopLossTicks * step;
				var takeDistance = processor.Settings.TakeProfitTicks * step;
				var entryPrice = processor.LongEntryPrice;

				if (processor.Settings.StopLossTicks > 0m && entryPrice.HasValue && candle.LowPrice <= entryPrice.Value - stopDistance)
				{
					CloseLong(processor);
				}
				else if (processor.Settings.TakeProfitTicks > 0m && entryPrice.HasValue && candle.HighPrice >= entryPrice.Value + takeDistance)
				{
					CloseLong(processor);
				}
			}

			if (processor.CanEvaluateShortRisk)
			{
				var stopDistance = processor.Settings.StopLossTicks * step;
				var takeDistance = processor.Settings.TakeProfitTicks * step;
				var entryPrice = processor.ShortEntryPrice;

				if (processor.Settings.StopLossTicks > 0m && entryPrice.HasValue && candle.HighPrice >= entryPrice.Value + stopDistance)
				{
					CloseShort(processor);
				}
				else if (processor.Settings.TakeProfitTicks > 0m && entryPrice.HasValue && candle.LowPrice <= entryPrice.Value - takeDistance)
				{
					CloseShort(processor);
				}
			}
		}

		private void HandleSignal(TimeframeProcessor processor, TimeframeSignal signal)
		{
			if (signal.CloseLong)
			CloseLong(processor);

			if (signal.CloseShort)
			CloseShort(processor);

			if (signal.OpenLong)
			OpenLong(processor);

			if (signal.OpenShort)
			OpenShort(processor);
		}

		private void OpenLong(TimeframeProcessor processor)
		{
			if (!processor.CanOpenLong)
			return;

			var volume = processor.CalculateBuyVolume();
			if (volume <= 0m)
			return;

			processor.RegisterPendingLongOpen(volume);
			PlaceOrder(processor, TradeAction.BuyOpen, volume);
		}

		private void CloseLong(TimeframeProcessor processor)
		{
			var volume = processor.GetLongCloseVolume();
			if (volume <= 0m)
			return;

			processor.RegisterPendingLongClose(volume);
			PlaceOrder(processor, TradeAction.BuyClose, volume);
		}

		private void OpenShort(TimeframeProcessor processor)
		{
			if (!processor.CanOpenShort)
			return;

			var volume = processor.CalculateSellVolume();
			if (volume <= 0m)
			return;

			processor.RegisterPendingShortOpen(volume);
			PlaceOrder(processor, TradeAction.SellOpen, volume);
		}

		private void CloseShort(TimeframeProcessor processor)
		{
			var volume = processor.GetShortCloseVolume();
			if (volume <= 0m)
			return;

			processor.RegisterPendingShortClose(volume);
			PlaceOrder(processor, TradeAction.SellClose, volume);
		}

		private readonly struct PendingOrderContext
		{
			public PendingOrderContext(string key, TradeAction action)
			{
				Comment = key + "|" + action;
			}

			public string Comment { get; }
		}

		private enum TradeAction
		{
			BuyOpen,
			BuyClose,
			SellOpen,
			SellClose
		}

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
			DeMark
		}

		public enum SmoothMethod
		{
			Sma,
			Ema,
			Smma,
			Lwma,
			Jjma,
			JurX,
			Parabolic,
			T3,
			Vidya,
			Ama
		}

		public enum MarginMode
		{
			FreeMargin,
			Balance,
			LossFreeMargin,
			LossBalance,
			Lot
		}

		private readonly struct TimeframeSignal
		{
			public TimeframeSignal(bool openLong, bool closeLong, bool openShort, bool closeShort)
			{
				OpenLong = openLong;
				CloseLong = closeLong;
				OpenShort = openShort;
				CloseShort = closeShort;
			}

			public bool OpenLong { get; }
			public bool CloseLong { get; }
			public bool OpenShort { get; }
			public bool CloseShort { get; }
		}

		private sealed class TimeframeProcessor
		{
			private readonly ColorXpWmaDigitMultiTimeframeStrategy _strategy;

			private readonly List<decimal> _priceBuffer = new();
			private readonly List<int> _colorHistory = new();
			private readonly List<bool> _buyResults = new();
			private readonly List<bool> _sellResults = new();

			private readonly IIndicator _smoothingIndicator;
			private decimal[] _weights;

			private decimal? _previousValue;
			private int? _previousColor;

			private decimal _pendingLongOpen;
			private decimal _pendingLongClose;
			private decimal _pendingShortOpen;
			private decimal _pendingShortClose;

			private decimal _longEntryVolume;
			private decimal _longEntryValue;
			private decimal _longExitVolume;
			private decimal _longExitValue;

			private decimal _shortEntryVolume;
			private decimal _shortEntryValue;
			private decimal _shortExitVolume;
			private decimal _shortExitValue;

			public TimeframeProcessor(ColorXpWmaDigitMultiTimeframeStrategy strategy, TimeframeSettings settings)
			{
				_strategy = strategy;
				Settings = settings;

				_weights = CreateWeights(Settings.Period, Settings.Power);
				_smoothingIndicator = CreateSmoothingIndicator(settings);
			}

			public TimeframeSettings Settings { get; }

			public void ProcessCandle(ICandleMessage candle)
			{
				if (candle.State != CandleStates.Finished)
				return;

				var price = GetAppliedPrice(candle, Settings.AppliedPrice);
				UpdatePriceBuffer(price);

				if (_priceBuffer.Count < Settings.Period)
				return;

				var pwma = CalculateWeightedAverage();
				var indicatorValue = _smoothingIndicator.Process(new DecimalIndicatorValue(_smoothingIndicator, pwma, candle.OpenTime));

				if (!indicatorValue.IsFormed)
				return;

				var currentValue = RoundValue(indicatorValue.ToDecimal(), Settings.Digit);
				var color = DetermineColor(currentValue);

				_previousValue = currentValue;
				_previousColor = color;

				_colorHistory.Insert(0, color);
				TrimHistory();

				_strategy.HandleRiskManagement(this, candle);

				var signal = CreateSignal();
				if (signal is not null)
				_strategy.HandleSignal(this, signal.Value);
			}

			public void ProcessTrade(MyTrade trade, TradeAction action)
			{
				var volume = trade.Trade?.Volume ?? trade.Order.Volume ?? 0m;
				if (volume <= 0m)
				return;

				switch (action)
				{
					case TradeAction.BuyOpen:
						_pendingLongOpen = Math.Max(0m, _pendingLongOpen - volume);
						_longEntryVolume += volume;
						_longEntryValue += (trade.Trade?.Price ?? trade.Order!.Price ?? 0m) * volume;
						break;

					case TradeAction.BuyClose:
						_pendingLongClose = Math.Max(0m, _pendingLongClose - volume);
						_longExitVolume += volume;
						_longExitValue += (trade.Trade?.Price ?? trade.Order!.Price ?? 0m) * volume;
						FinalizeLongIfNeeded();
						break;

					case TradeAction.SellOpen:
						_pendingShortOpen = Math.Max(0m, _pendingShortOpen - volume);
						_shortEntryVolume += volume;
						_shortEntryValue += (trade.Trade?.Price ?? trade.Order!.Price ?? 0m) * volume;
						break;

					case TradeAction.SellClose:
						_pendingShortClose = Math.Max(0m, _pendingShortClose - volume);
						_shortExitVolume += volume;
						_shortExitValue += (trade.Trade?.Price ?? trade.Order!.Price ?? 0m) * volume;
						FinalizeShortIfNeeded();
						break;
				}
			}

			public bool CanOpenLong => GetCurrentLongVolume() + _pendingLongOpen <= 0m && GetCurrentShortVolume() + _pendingShortClose <= 0m && Settings.BuyOpenAllowed;

			public bool CanOpenShort => GetCurrentShortVolume() + _pendingShortOpen <= 0m && GetCurrentLongVolume() + _pendingLongClose <= 0m && Settings.SellOpenAllowed;

			public bool CanEvaluateLongRisk => GetCurrentLongVolume() > 0m && Settings.BuyCloseAllowed;

			public bool CanEvaluateShortRisk => GetCurrentShortVolume() > 0m && Settings.SellCloseAllowed;

			public decimal? LongEntryPrice => _longEntryVolume > 0m ? _longEntryValue / _longEntryVolume : null;

			public decimal? ShortEntryPrice => _shortEntryVolume > 0m ? _shortEntryValue / _shortEntryVolume : null;

			public void RegisterPendingLongOpen(decimal volume)
			{
				_pendingLongOpen += volume;
			}

			public void RegisterPendingLongClose(decimal volume)
			{
				_pendingLongClose += volume;
			}

			public void RegisterPendingShortOpen(decimal volume)
			{
				_pendingShortOpen += volume;
			}

			public void RegisterPendingShortClose(decimal volume)
			{
				_pendingShortClose += volume;
			}

			public decimal GetLongCloseVolume()
			{
				var volume = GetCurrentLongVolume();
				return volume - _pendingLongClose;
			}

			public decimal GetShortCloseVolume()
			{
				var volume = GetCurrentShortVolume();
				return volume - _pendingShortClose;
			}

			public decimal CalculateBuyVolume()
			{
				return DetermineVolume(_buyResults, Settings.BuyTotalTrigger, Settings.BuyLossTrigger);
			}

			public decimal CalculateSellVolume()
			{
				return DetermineVolume(_sellResults, Settings.SellTotalTrigger, Settings.SellLossTrigger);
			}

			private decimal DetermineVolume(List<bool> history, int totalTrigger, int lossTrigger)
			{
				if (totalTrigger <= 0)
				return Settings.NormalMoneyManagement;

				var losses = 0;
				var count = 0;

				foreach (var isLoss in history)
				{
					count++;
					if (isLoss)
					losses++;

					if (count >= totalTrigger)
					break;
			}

			return losses >= lossTrigger ? Settings.SmallMoneyManagement : Settings.NormalMoneyManagement;
		}

		private void FinalizeLongIfNeeded()
		{
			var remaining = GetCurrentLongVolume();
			if (remaining > 0m)
			return;

			if (_longEntryVolume <= 0m || _longExitVolume <= 0m)
			{
				ResetLongAccumulators();
				return;
			}

			var entryPrice = _longEntryValue / _longEntryVolume;
			var exitPrice = _longExitValue / _longExitVolume;
			var isLoss = exitPrice < entryPrice;
			RecordBuyResult(isLoss);
			ResetLongAccumulators();
		}

		private void FinalizeShortIfNeeded()
		{
			var remaining = GetCurrentShortVolume();
			if (remaining > 0m)
			return;

			if (_shortEntryVolume <= 0m || _shortExitVolume <= 0m)
			{
				ResetShortAccumulators();
				return;
			}

			var entryPrice = _shortEntryValue / _shortEntryVolume;
			var exitPrice = _shortExitValue / _shortExitVolume;
			var isLoss = exitPrice > entryPrice;
			RecordSellResult(isLoss);
			ResetShortAccumulators();
		}

		private void ResetLongAccumulators()
		{
			_longEntryVolume = 0m;
			_longEntryValue = 0m;
			_longExitVolume = 0m;
			_longExitValue = 0m;
			_pendingLongOpen = 0m;
			_pendingLongClose = 0m;
		}

		private void ResetShortAccumulators()
		{
			_shortEntryVolume = 0m;
			_shortEntryValue = 0m;
			_shortExitVolume = 0m;
			_shortExitValue = 0m;
			_pendingShortOpen = 0m;
			_pendingShortClose = 0m;
		}

		private void RecordBuyResult(bool isLoss)
		{
			_buyResults.Insert(0, isLoss);
			TrimResultHistory(_buyResults, Settings.BuyTotalTrigger);
		}

		private void RecordSellResult(bool isLoss)
		{
			_sellResults.Insert(0, isLoss);
			TrimResultHistory(_sellResults, Settings.SellTotalTrigger);
		}

		private static void TrimResultHistory(List<bool> history, int maxLength)
		{
			if (maxLength <= 0)
			maxLength = 10;

			while (history.Count > Math.Max(maxLength * 2, 10))
			history.RemoveAt(history.Count - 1);
		}

		private decimal GetCurrentLongVolume()
		{
			return Math.Max(0m, _longEntryVolume - _longExitVolume);
		}

		private decimal GetCurrentShortVolume()
		{
			return Math.Max(0m, _shortEntryVolume - _shortExitVolume);
		}

		private TimeframeSignal? CreateSignal()
		{
			if (_colorHistory.Count <= Settings.SignalBar)
			return null;

			var currentColor = _colorHistory[Settings.SignalBar];
			var previousIndex = Settings.SignalBar + 1;
			var previousColor = previousIndex < _colorHistory.Count ? _colorHistory[previousIndex] : (int?)null;

			var openLong = Settings.BuyOpenAllowed && currentColor == 2 && previousColor != 2;
			var closeLong = Settings.BuyCloseAllowed && currentColor == 0;
			var openShort = Settings.SellOpenAllowed && currentColor == 0 && previousColor != 0;
			var closeShort = Settings.SellCloseAllowed && currentColor == 2;

			return new TimeframeSignal(openLong, closeLong, openShort, closeShort);
		}

		private void TrimHistory()
		{
			var required = Settings.SignalBar + 5;
			while (_colorHistory.Count > required)
			_colorHistory.RemoveAt(_colorHistory.Count - 1);
		}

		private void UpdatePriceBuffer(decimal price)
		{
			_priceBuffer.Insert(0, price);
			if (_priceBuffer.Count > Settings.Period)
			_priceBuffer.RemoveAt(_priceBuffer.Count - 1);
		}

		private decimal CalculateWeightedAverage()
		{
			if (_weights.Length != Settings.Period)
			_weights = CreateWeights(Settings.Period, Settings.Power);

			decimal sum = 0m;
			decimal weightSum = 0m;

			for (var i = 0; i < Settings.Period; i++)
			{
				var weight = _weights[i];
				var price = _priceBuffer[i];
				sum += price * weight;
				weightSum += weight;
			}

			return weightSum > 0m ? sum / weightSum : sum;
		}

		private int DetermineColor(decimal value)
		{
			if (_previousValue is decimal prev)
			{
				if (value > prev)
				return 2;

				if (value < prev)
				return 0;

				return _previousColor ?? 1;
			}

			return 1;
		}

		private static decimal RoundValue(decimal value, int digits)
		{
			if (digits <= 0)
			return value;

			var factor = (decimal)Math.Pow(10, digits);
			return Math.Round(value * factor, MidpointRounding.AwayFromZero) / factor;
		}

		private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrice price)
		{
			return price switch
			{
				AppliedPrice.Close => candle.ClosePrice,
				AppliedPrice.Open => candle.OpenPrice,
				AppliedPrice.High => candle.HighPrice,
				AppliedPrice.Low => candle.LowPrice,
				AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
				AppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
				AppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
				AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
				AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
				AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
				AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
				AppliedPrice.DeMark =>
				CalculateDeMarkPrice(candle),
				_ => candle.ClosePrice
			};
		}

		private static decimal CalculateDeMarkPrice(ICandleMessage candle)
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

		private static decimal[] CreateWeights(int period, decimal power)
		{
			var weights = new decimal[Math.Max(1, period)];

			for (var i = 0; i < weights.Length; i++)
			{
				var exponent = (double)power;
				var distance = period - i;
				weights[i] = (decimal)Math.Pow(distance, exponent);
			}

			return weights;
		}

		private static IIndicator CreateSmoothingIndicator(TimeframeSettings settings)
		{
			return settings.SmoothMethod switch
			{
				SmoothMethod.Sma => new SimpleMovingAverage { Length = settings.SmoothLength },
				SmoothMethod.Ema => new ExponentialMovingAverage { Length = settings.SmoothLength },
				SmoothMethod.Smma => new SmoothedMovingAverage { Length = settings.SmoothLength },
				SmoothMethod.Lwma => new WeightedMovingAverage { Length = settings.SmoothLength },
				SmoothMethod.Jjma => new JurikMovingAverage { Length = settings.SmoothLength },
				SmoothMethod.T3 => new TillsonMovingAverage { Length = settings.SmoothLength },
				SmoothMethod.Ama => new KaufmanAdaptiveMovingAverage { Length = settings.SmoothLength },
				SmoothMethod.Vidya => new VidyaIndicator { Length = settings.SmoothLength },
				_ => new SimpleMovingAverage { Length = settings.SmoothLength }
			};
		}
	}

	private sealed class VidyaIndicator : Indicator<decimal>
	{
		public int Length { get; set; } = 10;
		public int Momentum { get; set; } = 20;

		private decimal? _prev;
		private readonly ChandeMomentumOscillator _cmo = new();

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = input.ToDecimal();
			_cmo.Length = Momentum;
			var cmoVal = _cmo.Process(input);
			if (!cmoVal.IsFormed)
			return new DecimalIndicatorValue(this, 0m, input.Time) { IsFinal = false };

			var alpha = 2m / (Length + 1m);
			var weight = Math.Abs(cmoVal.ToDecimal()) / 100m;
			_prev = _prev ?? value;
			_prev = alpha * weight * value + (1m - alpha * weight) * _prev.Value;
			return new DecimalIndicatorValue(this, _prev.Value, input.Time);
		}
	}

	private sealed class TillsonMovingAverage : Indicator<decimal>
	{
		public int Length { get; set; } = 5;
		public decimal VolumeFactor { get; set; } = 0.7m;

		private readonly ExponentialMovingAverage _ema1 = new();
		private readonly ExponentialMovingAverage _ema2 = new();
		private readonly ExponentialMovingAverage _ema3 = new();
		private readonly ExponentialMovingAverage _ema4 = new();

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			_ema1.Length = Length;
			_ema2.Length = Length;
			_ema3.Length = Length;
			_ema4.Length = Length;

			var ema1 = _ema1.Process(input);
			var ema2 = _ema2.Process(ema1);
			var ema3 = _ema3.Process(ema2);
			var ema4 = _ema4.Process(ema3);

			if (!ema4.IsFormed)
			return new DecimalIndicatorValue(this, 0m, input.Time) { IsFinal = false };

			var e1 = ema1.ToDecimal();
			var e2 = ema2.ToDecimal();
			var e3 = ema3.ToDecimal();
			var e4 = ema4.ToDecimal();

			var c1 = -VolumeFactor * VolumeFactor * VolumeFactor;
			var c2 = 3m * VolumeFactor * VolumeFactor + 3m * VolumeFactor * VolumeFactor * VolumeFactor;
			var c3 = -6m * VolumeFactor * VolumeFactor - 3m * VolumeFactor - 3m * VolumeFactor * VolumeFactor * VolumeFactor;
			var c4 = 1m + 3m * VolumeFactor + VolumeFactor * VolumeFactor * VolumeFactor + 3m * VolumeFactor * VolumeFactor;

			var t3 = c1 * e4 + c2 * e3 + c3 * e2 + c4 * e1;
			return new DecimalIndicatorValue(this, t3, input.Time);
		}
	}

	private sealed class TimeframeSettings
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _period;
		private readonly StrategyParam<decimal> _power;
		private readonly StrategyParam<SmoothMethod> _smoothMethod;
		private readonly StrategyParam<int> _smoothLength;
		private readonly StrategyParam<int> _smoothPhase;
		private readonly StrategyParam<AppliedPrice> _appliedPrice;
		private readonly StrategyParam<int> _digit;
		private readonly StrategyParam<int> _signalBar;
		private readonly StrategyParam<int> _buyMagic;
		private readonly StrategyParam<int> _sellMagic;
		private readonly StrategyParam<int> _buyTotalTrigger;
		private readonly StrategyParam<int> _buyLossTrigger;
		private readonly StrategyParam<int> _sellTotalTrigger;
		private readonly StrategyParam<int> _sellLossTrigger;
		private readonly StrategyParam<decimal> _smallMoneyManagement;
		private readonly StrategyParam<decimal> _normalMoneyManagement;
		private readonly StrategyParam<MarginMode> _marginMode;
		private readonly StrategyParam<decimal> _stopLossTicks;
		private readonly StrategyParam<decimal> _takeProfitTicks;
		private readonly StrategyParam<decimal> _deviationTicks;
		private readonly StrategyParam<bool> _buyOpenAllowed;
		private readonly StrategyParam<bool> _sellOpenAllowed;
		private readonly StrategyParam<bool> _sellCloseAllowed;
		private readonly StrategyParam<bool> _buyCloseAllowed;

		public TimeframeSettings(ColorXpWmaDigitMultiTimeframeStrategy strategy, string key, DataType defaultType, int period, decimal power, SmoothMethod smoothMethod,
		int smoothLength, int smoothPhase, AppliedPrice appliedPrice, int digit, int signalBar, int buyMagic, int sellMagic,
		int buyTotalTrigger, int buyLossTrigger, int sellTotalTrigger, int sellLossTrigger, decimal smallMoneyManagement,
		decimal normalMoneyManagement, MarginMode marginMode, decimal stopLossTicks, decimal takeProfitTicks, decimal deviationTicks,
		bool buyOpenAllowed, bool sellOpenAllowed, bool sellCloseAllowed, bool buyCloseAllowed)
		{
			Key = key;

			_candleType = strategy.Param(key + "_CandleType", defaultType)
			.SetDisplay(key + " Candle Type", "Timeframe for module " + key, "Module " + key);

			_period = strategy.Param(key + "_Period", period)
			.SetDisplay(key + " iPeriod", "Price weighting depth", "Module " + key)
			.SetGreaterThanZero();

			_power = strategy.Param(key + "_Power", power)
			.SetDisplay(key + " Power", "Exponent for price weights", "Module " + key);

			_smoothMethod = strategy.Param(key + "_SmoothMethod", smoothMethod)
			.SetDisplay(key + " Smooth Method", "Smoothing algorithm", "Module " + key);

			_smoothLength = strategy.Param(key + "_SmoothLength", smoothLength)
			.SetDisplay(key + " Smooth Length", "Length of the smoothing moving average", "Module " + key)
			.SetGreaterThanZero();

			_smoothPhase = strategy.Param(key + "_SmoothPhase", smoothPhase)
			.SetDisplay(key + " Smooth Phase", "Phase parameter for certain smoothers", "Module " + key);

			_appliedPrice = strategy.Param(key + "_AppliedPrice", appliedPrice)
			.SetDisplay(key + " Price", "Price source", "Module " + key);

			_digit = strategy.Param(key + "_Digit", digit)
			.SetDisplay(key + " Digit", "Rounding precision", "Module " + key)
			.SetNotNegative();

			_signalBar = strategy.Param(key + "_SignalBar", signalBar)
			.SetDisplay(key + " Signal Bar", "Shift for the signal candle", "Module " + key)
			.SetNotNegative();

			_buyMagic = strategy.Param(key + "_BuyMagic", buyMagic)
			.SetDisplay(key + " Buy Magic", "Identifier for long trades", "Module " + key);

			_sellMagic = strategy.Param(key + "_SellMagic", sellMagic)
			.SetDisplay(key + " Sell Magic", "Identifier for short trades", "Module " + key);

			_buyTotalTrigger = strategy.Param(key + "_BuyTotalTrigger", buyTotalTrigger)
			.SetDisplay(key + " Buy Total Trigger", "Number of last trades to check", "Module " + key)
			.SetNotNegative();

			_buyLossTrigger = strategy.Param(key + "_BuyLossTrigger", buyLossTrigger)
			.SetDisplay(key + " Buy Loss Trigger", "Losses before reducing position size", "Module " + key)
			.SetNotNegative();

			_sellTotalTrigger = strategy.Param(key + "_SellTotalTrigger", sellTotalTrigger)
			.SetDisplay(key + " Sell Total Trigger", "Number of last short trades to check", "Module " + key)
			.SetNotNegative();

			_sellLossTrigger = strategy.Param(key + "_SellLossTrigger", sellLossTrigger)
			.SetDisplay(key + " Sell Loss Trigger", "Losses before reducing short size", "Module " + key)
			.SetNotNegative();

			_smallMoneyManagement = strategy.Param(key + "_SmallMM", smallMoneyManagement)
			.SetDisplay(key + " Small MM", "Reduced position size", "Module " + key)
			.SetNotNegative();

			_normalMoneyManagement = strategy.Param(key + "_NormalMM", normalMoneyManagement)
			.SetDisplay(key + " Normal MM", "Standard position size", "Module " + key)
			.SetNotNegative();

			_marginMode = strategy.Param(key + "_MarginMode", marginMode)
			.SetDisplay(key + " Margin Mode", "Reserved for compatibility", "Module " + key);

			_stopLossTicks = strategy.Param(key + "_StopLoss", stopLossTicks)
			.SetDisplay(key + " Stop Loss", "Stop loss distance in price steps", "Module " + key)
			.SetNotNegative();

			_takeProfitTicks = strategy.Param(key + "_TakeProfit", takeProfitTicks)
			.SetDisplay(key + " Take Profit", "Take profit distance in price steps", "Module " + key)
			.SetNotNegative();

			_deviationTicks = strategy.Param(key + "_Deviation", deviationTicks)
			.SetDisplay(key + " Deviation", "Maximum slippage (reserved)", "Module " + key)
			.SetNotNegative();

			_buyOpenAllowed = strategy.Param(key + "_BuyOpen", buyOpenAllowed)
			.SetDisplay(key + " Buy Open", "Allow opening long positions", "Module " + key);

			_sellOpenAllowed = strategy.Param(key + "_SellOpen", sellOpenAllowed)
			.SetDisplay(key + " Sell Open", "Allow opening short positions", "Module " + key);

			_sellCloseAllowed = strategy.Param(key + "_SellClose", sellCloseAllowed)
			.SetDisplay(key + " Sell Close", "Allow closing short positions on bullish signal", "Module " + key);

			_buyCloseAllowed = strategy.Param(key + "_BuyClose", buyCloseAllowed)
			.SetDisplay(key + " Buy Close", "Allow closing long positions on bearish signal", "Module " + key);
		}

		public string Key { get; }

		public DataType CandleType => _candleType.Value;
		public int Period => Math.Max(1, _period.Value);
		public decimal Power => _power.Value;
		public SmoothMethod SmoothMethod => _smoothMethod.Value;
		public int SmoothLength => Math.Max(1, _smoothLength.Value);
		public int SmoothPhase => _smoothPhase.Value;
		public AppliedPrice AppliedPrice => _appliedPrice.Value;
		public int Digit => Math.Max(0, _digit.Value);
		public int SignalBar => Math.Max(0, _signalBar.Value);
		public int BuyMagic => _buyMagic.Value;
		public int SellMagic => _sellMagic.Value;
		public int BuyTotalTrigger => Math.Max(0, _buyTotalTrigger.Value);
		public int BuyLossTrigger => Math.Max(0, _buyLossTrigger.Value);
		public int SellTotalTrigger => Math.Max(0, _sellTotalTrigger.Value);
		public int SellLossTrigger => Math.Max(0, _sellLossTrigger.Value);
		public decimal SmallMoneyManagement => _smallMoneyManagement.Value;
		public decimal NormalMoneyManagement => _normalMoneyManagement.Value;
		public MarginMode MarginMode => _marginMode.Value;
		public decimal StopLossTicks => _stopLossTicks.Value;
		public decimal TakeProfitTicks => _takeProfitTicks.Value;
		public decimal DeviationTicks => _deviationTicks.Value;
		public bool BuyOpenAllowed => _buyOpenAllowed.Value;
		public bool SellOpenAllowed => _sellOpenAllowed.Value;
		public bool SellCloseAllowed => _sellCloseAllowed.Value;
		public bool BuyCloseAllowed => _buyCloseAllowed.Value;
	}
}
