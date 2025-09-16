namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

public class AbsolutelyNoLagLwmaDigitMmRecStrategy : Strategy
{
	private readonly ModuleContext[] _modules;

	public AbsolutelyNoLagLwmaDigitMmRecStrategy()
	{
		_modules =
		[
			new ModuleContext(
			this,
			"A",
			"Module A",
			TimeSpan.FromHours(12),
			5,
			AppliedPrice.Close,
			2,
			0.01m,
			0.1m,
			2,
			2,
			3000,
			10000),
			new ModuleContext(
			this,
			"B",
			"Module B",
			TimeSpan.FromHours(4),
			5,
			AppliedPrice.Close,
			2,
			0.01m,
			0.1m,
			2,
			2,
			2000,
			6000),
			new ModuleContext(
			this,
			"C",
			"Module C",
			TimeSpan.FromHours(2),
			5,
			AppliedPrice.Close,
			1,
			0.01m,
			0.1m,
			3,
			3,
			1000,
			3000)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		foreach (var module in _modules)
		{
			module.Reset();
		}
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		foreach (var module in _modules)
		{
			module.SetupIndicators();

			var subscription = SubscribeCandles(module.CandleType);
			subscription
			.Bind(candle => ProcessModule(candle, module))
			.Start();
		}
	}

	private void ProcessModule(ICandleMessage candle, ModuleContext module)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!module.TryGetValue(candle, out var value))
		return;

		var (trend, changed) = module.UpdateTrend(value);

		if (module.CheckStops(this, candle))
		return;

		if (!changed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		switch (trend)
		{
		case TrendDirection.Up:
			module.HandleUpTrend(this, candle);
			break;
		case TrendDirection.Down:
			module.HandleDownTrend(this, candle);
			break;
		}
	}

	internal decimal GetPriceStep()
	{
		var step = Security?.Step;
		return step is null or 0m ? 1m : step.Value;
	}

	private enum TrendDirection
	{
		None,
		Up,
		Down,
	}

	private enum AppliedPrice
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
		TrendFollow1,
		TrendFollow2,
		Demark,
	}

	private sealed class ModuleContext
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _length;
		private readonly StrategyParam<AppliedPrice> _appliedPrice;
		private readonly StrategyParam<int> _digits;
		private readonly StrategyParam<bool> _buyOpen;
		private readonly StrategyParam<bool> _sellOpen;
		private readonly StrategyParam<bool> _buyClose;
		private readonly StrategyParam<bool> _sellClose;
		private readonly StrategyParam<decimal> _smallVolume;
		private readonly StrategyParam<decimal> _normalVolume;
		private readonly StrategyParam<int> _buyLossTrigger;
		private readonly StrategyParam<int> _sellLossTrigger;
		private readonly StrategyParam<int> _stopLossPoints;
		private readonly StrategyParam<int> _takeProfitPoints;

		private readonly Queue<bool> _buyLosses = new();
		private readonly Queue<bool> _sellLosses = new();

		private decimal? _previousValue;
		private TrendDirection _previousTrend = TrendDirection.None;

		private decimal _positionVolume;
		private decimal? _longEntry;
		private decimal? _shortEntry;
		private decimal? _longStop;
		private decimal? _longTarget;
		private decimal? _shortStop;
		private decimal? _shortTarget;

		public ModuleContext(
		Strategy strategy,
		string prefix,
		string displayName,
		TimeSpan defaultTimeFrame,
		int defaultLength,
		AppliedPrice defaultPrice,
		int defaultDigits,
		decimal defaultSmallVolume,
		decimal defaultNormalVolume,
		int defaultBuyLossTrigger,
		int defaultSellLossTrigger,
		int defaultStopLoss,
		int defaultTakeProfit)
		{
			_candleType = strategy.Param($"{prefix}CandleType", defaultTimeFrame.TimeFrame())
			.SetDisplay($"{displayName} Candle", $"Time frame for {displayName}.", displayName);

			_length = strategy.Param($"{prefix}Length", defaultLength)
			.SetDisplay($"{displayName} Length", $"AbsolutelyNoLagLWMA length for {displayName}.", displayName)
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

			_appliedPrice = strategy.Param($"{prefix}AppliedPrice", defaultPrice)
			.SetDisplay($"{displayName} Price", $"Price type used by {displayName}.", displayName);

			_digits = strategy.Param($"{prefix}Digits", defaultDigits)
			.SetDisplay($"{displayName} Digits", $"Rounding digits for {displayName} smooth line.", displayName);

			_buyOpen = strategy.Param($"{prefix}BuyOpen", true)
			.SetDisplay($"{displayName} Buy Entry", $"Allow long entries for {displayName}.", displayName);

			_sellOpen = strategy.Param($"{prefix}SellOpen", true)
			.SetDisplay($"{displayName} Sell Entry", $"Allow short entries for {displayName}.", displayName);

			_buyClose = strategy.Param($"{prefix}BuyClose", true)
			.SetDisplay($"{displayName} Buy Exit", $"Allow long exits for {displayName}.", displayName);

			_sellClose = strategy.Param($"{prefix}SellClose", true)
			.SetDisplay($"{displayName} Sell Exit", $"Allow short exits for {displayName}.", displayName);

			_smallVolume = strategy.Param($"{prefix}SmallVolume", defaultSmallVolume)
			.SetDisplay($"{displayName} Reduced Volume", $"Volume used after repeated losses in {displayName}.", displayName);

			_normalVolume = strategy.Param($"{prefix}NormalVolume", defaultNormalVolume)
			.SetDisplay($"{displayName} Normal Volume", $"Default trading volume for {displayName}.", displayName);

			_buyLossTrigger = strategy.Param($"{prefix}BuyLossTrigger", defaultBuyLossTrigger)
			.SetDisplay($"{displayName} Long Loss Trigger", $"Number of consecutive losing longs before reducing volume for {displayName}.", displayName);

			_sellLossTrigger = strategy.Param($"{prefix}SellLossTrigger", defaultSellLossTrigger)
			.SetDisplay($"{displayName} Short Loss Trigger", $"Number of consecutive losing shorts before reducing volume for {displayName}.", displayName);

			_stopLossPoints = strategy.Param($"{prefix}StopLossPoints", defaultStopLoss)
			.SetDisplay($"{displayName} Stop Loss", $"Protective stop in price steps for {displayName}.", displayName);

			_takeProfitPoints = strategy.Param($"{prefix}TakeProfitPoints", defaultTakeProfit)
			.SetDisplay($"{displayName} Take Profit", $"Profit target in price steps for {displayName}.", displayName);
		}

		public DataType CandleType => _candleType.Value;

		public int Length => Math.Max(1, _length.Value);

		public AppliedPrice PriceMode => _appliedPrice.Value;

		public int Digits => Math.Max(0, _digits.Value);

		public bool BuyOpenEnabled => _buyOpen.Value;

		public bool SellOpenEnabled => _sellOpen.Value;

		public bool BuyCloseEnabled => _buyClose.Value;

		public bool SellCloseEnabled => _sellClose.Value;

		public decimal SmallVolume => Math.Max(0m, _smallVolume.Value);

		public decimal NormalVolume => Math.Max(0m, _normalVolume.Value);

		public int BuyLossTrigger => Math.Max(0, _buyLossTrigger.Value);

		public int SellLossTrigger => Math.Max(0, _sellLossTrigger.Value);

		public int StopLossPoints => Math.Max(0, _stopLossPoints.Value);

		public int TakeProfitPoints => Math.Max(0, _takeProfitPoints.Value);

		public WeightedMovingAverage Primary { get; private set; } = null!;

		public WeightedMovingAverage Secondary { get; private set; } = null!;

		public void SetupIndicators()
		{
			Primary = new WeightedMovingAverage { Length = Length };
			Secondary = new WeightedMovingAverage { Length = Length };

			_previousValue = null;
			_previousTrend = TrendDirection.None;
		}

		public void Reset()
		{
			_buyLosses.Clear();
			_sellLosses.Clear();

			_previousValue = null;
			_previousTrend = TrendDirection.None;

			_positionVolume = 0m;
			_longEntry = null;
			_shortEntry = null;
			_longStop = null;
			_longTarget = null;
			_shortStop = null;
			_shortTarget = null;

			Primary?.Reset();
			Secondary?.Reset();
		}

		public bool TryGetValue(ICandleMessage candle, out decimal value)
		{
			var price = GetPrice(candle);
			var primaryValue = Primary.Process(new DecimalIndicatorValue(Primary, price, candle.OpenTime));
			if (primaryValue is not DecimalIndicatorValue { IsFinal: true, Value: var primary })
			{
				value = default;
				return false;
			}

			var secondaryValue = Secondary.Process(new DecimalIndicatorValue(Secondary, primary, candle.OpenTime));
			if (secondaryValue is not DecimalIndicatorValue { IsFinal: true, Value: var smooth })
			{
				value = default;
				return false;
			}

			value = Round(smooth);
			return true;
		}

		public (TrendDirection trend, bool changed) UpdateTrend(decimal value)
		{
			if (_previousValue is null)
			{
				_previousValue = value;
				_previousTrend = TrendDirection.None;
				return (TrendDirection.None, false);
			}

			var prevTrend = _previousTrend;
			var prevValue = _previousValue.Value;

			TrendDirection trend;
			if (value > prevValue)
			trend = TrendDirection.Up;
			else if (value < prevValue)
			trend = TrendDirection.Down;
			else
			trend = prevTrend;

			_previousValue = value;
			_previousTrend = trend;

			return (trend, trend != prevTrend);
		}

		public bool CheckStops(AbsolutelyNoLagLwmaDigitMmRecStrategy strategy, ICandleMessage candle)
		{
			if (_positionVolume > 0m)
			{
				if (_longStop is decimal stop && candle.LowPrice <= stop)
				{
					CloseLong(strategy, stop);
					return true;
				}

				if (_longTarget is decimal target && candle.HighPrice >= target)
				{
					CloseLong(strategy, target);
					return true;
				}
			}
			else if (_positionVolume < 0m)
			{
				if (_shortStop is decimal stop && candle.HighPrice >= stop)
				{
					CloseShort(strategy, stop);
					return true;
				}

				if (_shortTarget is decimal target && candle.LowPrice <= target)
				{
					CloseShort(strategy, target);
					return true;
				}
			}

			return false;
		}

		public void HandleUpTrend(AbsolutelyNoLagLwmaDigitMmRecStrategy strategy, ICandleMessage candle)
		{
			if (SellCloseEnabled && _positionVolume < 0m)
			{
				CloseShort(strategy, candle.ClosePrice);
			}

			if (!BuyOpenEnabled)
			return;

			if (_positionVolume > 0m)
			{
				UpdateLongLevels(strategy, _longEntry ?? candle.ClosePrice);
				return;
			}

			if (_positionVolume < 0m)
			return;

			var volume = GetBuyVolume();
			if (volume <= 0m)
			return;

			strategy.BuyMarket(volume);

			_positionVolume += volume;
			_longEntry = candle.ClosePrice;
			UpdateLongLevels(strategy, _longEntry.Value);
		}

		public void HandleDownTrend(AbsolutelyNoLagLwmaDigitMmRecStrategy strategy, ICandleMessage candle)
		{
			if (BuyCloseEnabled && _positionVolume > 0m)
			{
				CloseLong(strategy, candle.ClosePrice);
			}

			if (!SellOpenEnabled)
			return;

			if (_positionVolume < 0m)
			{
				UpdateShortLevels(strategy, _shortEntry ?? candle.ClosePrice);
				return;
			}

			if (_positionVolume > 0m)
			return;

			var volume = GetSellVolume();
			if (volume <= 0m)
			return;

			strategy.SellMarket(volume);

			_positionVolume -= volume;
			_shortEntry = candle.ClosePrice;
			UpdateShortLevels(strategy, _shortEntry.Value);
		}

		private decimal GetPrice(ICandleMessage candle)
		{
			return PriceMode switch
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
				AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
				? candle.HighPrice
				: candle.ClosePrice < candle.OpenPrice
				? candle.LowPrice
				: candle.ClosePrice,
				AppliedPrice.TrendFollow2 => candle.ClosePrice > candle.OpenPrice
				? (candle.HighPrice + candle.ClosePrice) / 2m
				: candle.ClosePrice < candle.OpenPrice
				? (candle.LowPrice + candle.ClosePrice) / 2m
				: candle.ClosePrice,
				AppliedPrice.Demark => CalculateDemarkPrice(candle),
				_ => candle.ClosePrice,
			};
		}

		private static decimal CalculateDemarkPrice(ICandleMessage candle)
		{
			var result = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

			if (candle.ClosePrice < candle.OpenPrice)
			result = (result + candle.LowPrice) / 2m;
			else if (candle.ClosePrice > candle.OpenPrice)
			result = (result + candle.HighPrice) / 2m;
			else
			result = (result + candle.ClosePrice) / 2m;

			return ((result - candle.LowPrice) + (result - candle.HighPrice)) / 2m;
		}

		private decimal Round(decimal value)
		{
			var digits = Digits;
			return digits > 0 ? Math.Round(value, digits, MidpointRounding.AwayFromZero) : value;
		}

		private void UpdateLongLevels(AbsolutelyNoLagLwmaDigitMmRecStrategy strategy, decimal entry)
		{
			var step = strategy.GetPriceStep();

			_longStop = StopLossPoints > 0 ? entry - StopLossPoints * step : null;
			_longTarget = TakeProfitPoints > 0 ? entry + TakeProfitPoints * step : null;
		}

		private void UpdateShortLevels(AbsolutelyNoLagLwmaDigitMmRecStrategy strategy, decimal entry)
		{
			var step = strategy.GetPriceStep();

			_shortStop = StopLossPoints > 0 ? entry + StopLossPoints * step : null;
			_shortTarget = TakeProfitPoints > 0 ? entry - TakeProfitPoints * step : null;
		}

		private decimal GetBuyVolume()
		{
			var volume = NormalVolume;
			var trigger = BuyLossTrigger;
			if (trigger <= 0)
			return volume;

			if (_buyLosses.Count >= trigger && AllLosses(_buyLosses, trigger))
			volume = SmallVolume;

			return volume;
		}

		private decimal GetSellVolume()
		{
			var volume = NormalVolume;
			var trigger = SellLossTrigger;
			if (trigger <= 0)
			return volume;

			if (_sellLosses.Count >= trigger && AllLosses(_sellLosses, trigger))
			volume = SmallVolume;

			return volume;
		}

		private static bool AllLosses(Queue<bool> queue, int trigger)
		{
			if (queue.Count < trigger)
			return false;

			var index = 0;
			foreach (var loss in queue)
			{
				if (!loss)
				return false;

				index++;
				if (index >= trigger)
				break;
			}

			return true;
		}

		private void CloseLong(AbsolutelyNoLagLwmaDigitMmRecStrategy strategy, decimal exitPrice)
		{
			if (_positionVolume <= 0m)
			return;

			strategy.SellMarket(_positionVolume);

			if (_longEntry is decimal entry)
			{
				var profit = (exitPrice - entry) * _positionVolume;
				RegisterBuyResult(profit < 0m);
			}

			_positionVolume = 0m;
			_longEntry = null;
			_longStop = null;
			_longTarget = null;
		}

		private void CloseShort(AbsolutelyNoLagLwmaDigitMmRecStrategy strategy, decimal exitPrice)
		{
			if (_positionVolume >= 0m)
			return;

			var volume = Math.Abs(_positionVolume);
			strategy.BuyMarket(volume);

			if (_shortEntry is decimal entry)
			{
				var profit = (entry - exitPrice) * volume;
				RegisterSellResult(profit < 0m);
			}

			_positionVolume = 0m;
			_shortEntry = null;
			_shortStop = null;
			_shortTarget = null;
		}

		private void RegisterBuyResult(bool isLoss)
		{
			var trigger = BuyLossTrigger;
			if (trigger <= 0)
			{
				_buyLosses.Clear();
				return;
			}

			_buyLosses.Enqueue(isLoss);
			TrimQueue(_buyLosses, trigger);
		}

		private void RegisterSellResult(bool isLoss)
		{
			var trigger = SellLossTrigger;
			if (trigger <= 0)
			{
				_sellLosses.Clear();
				return;
			}

			_sellLosses.Enqueue(isLoss);
			TrimQueue(_sellLosses, trigger);
		}

		private static void TrimQueue(Queue<bool> queue, int maxSize)
		{
			while (queue.Count > maxSize)
			{
				queue.Dequeue();
			}
		}
	}
}
