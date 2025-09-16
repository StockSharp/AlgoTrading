using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combined BrainTrend2 and AbsolutelyNoLagLWMA strategy with MMRec style permissions.
/// </summary>
public class BrainTrend2AbsolutelyNoLagLwmaMmrecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _brainCandleType;
	private readonly StrategyParam<int> _brainAtrPeriod;
	private readonly StrategyParam<int> _brainSignalBar;
	private readonly StrategyParam<bool> _brainBuyOpen;
	private readonly StrategyParam<bool> _brainSellOpen;
	private readonly StrategyParam<bool> _brainSellClose;
	private readonly StrategyParam<bool> _brainBuyClose;

	private readonly StrategyParam<DataType> _absCandleType;
	private readonly StrategyParam<int> _absLength;
	private readonly StrategyParam<AppliedPrice> _absPriceMode;
	private readonly StrategyParam<int> _absSignalBar;
	private readonly StrategyParam<bool> _absBuyOpen;
	private readonly StrategyParam<bool> _absSellOpen;
	private readonly StrategyParam<bool> _absSellClose;
	private readonly StrategyParam<bool> _absBuyClose;
	private readonly StrategyParam<decimal> _absPriceShift;

	private readonly StrategyParam<decimal> _orderVolume;

	private BrainTrendCalculator _brainCalculator = null!;
	private AbsolutelyNoLagCalculator _absCalculator = null!;

	private readonly List<int> _brainColors = new();
	private readonly List<int> _absColors = new();

	/// <summary>
	/// Candle type used for the BrainTrend2 block.
	/// </summary>
	public DataType BrainCandleType { get => _brainCandleType.Value; set => _brainCandleType.Value = value; }

	/// <summary>
	/// ATR length for the BrainTrend2 block.
	/// </summary>
	public int BrainAtrPeriod { get => _brainAtrPeriod.Value; set => _brainAtrPeriod.Value = Math.Max(1, value); }

	/// <summary>
	/// Number of bars to shift BrainTrend2 signals.
	/// </summary>
	public int BrainSignalBar { get => _brainSignalBar.Value; set => _brainSignalBar.Value = Math.Max(0, value); }

	/// <summary>
	/// Enable BrainTrend2 buy entries.
	/// </summary>
	public bool BrainEnableBuyOpen { get => _brainBuyOpen.Value; set => _brainBuyOpen.Value = value; }

	/// <summary>
	/// Enable BrainTrend2 sell entries.
	/// </summary>
	public bool BrainEnableSellOpen { get => _brainSellOpen.Value; set => _brainSellOpen.Value = value; }

	/// <summary>
	/// Allow BrainTrend2 signals to close short positions.
	/// </summary>
	public bool BrainEnableSellClose { get => _brainSellClose.Value; set => _brainSellClose.Value = value; }

	/// <summary>
	/// Allow BrainTrend2 signals to close long positions.
	/// </summary>
	public bool BrainEnableBuyClose { get => _brainBuyClose.Value; set => _brainBuyClose.Value = value; }

	/// <summary>
	/// Candle type used for the AbsolutelyNoLagLWMA block.
	/// </summary>
	public DataType AbsCandleType { get => _absCandleType.Value; set => _absCandleType.Value = value; }

	/// <summary>
	/// Length of the AbsolutelyNoLagLWMA filter.
	/// </summary>
	public int AbsLength { get => _absLength.Value; set => _absLength.Value = Math.Max(1, value); }

	/// <summary>
	/// Price source used by the AbsolutelyNoLagLWMA filter.
	/// </summary>
	public AppliedPrice AbsPriceMode { get => _absPriceMode.Value; set => _absPriceMode.Value = value; }

	/// <summary>
	/// Number of bars to shift AbsolutelyNoLagLWMA signals.
	/// </summary>
	public int AbsSignalBar { get => _absSignalBar.Value; set => _absSignalBar.Value = Math.Max(0, value); }

	/// <summary>
	/// Enable AbsolutelyNoLagLWMA buy entries.
	/// </summary>
	public bool AbsEnableBuyOpen { get => _absBuyOpen.Value; set => _absBuyOpen.Value = value; }

	/// <summary>
	/// Enable AbsolutelyNoLagLWMA sell entries.
	/// </summary>
	public bool AbsEnableSellOpen { get => _absSellOpen.Value; set => _absSellOpen.Value = value; }

	/// <summary>
	/// Allow AbsolutelyNoLagLWMA signals to close short positions.
	/// </summary>
	public bool AbsEnableSellClose { get => _absSellClose.Value; set => _absSellClose.Value = value; }

	/// <summary>
	/// Allow AbsolutelyNoLagLWMA signals to close long positions.
	/// </summary>
	public bool AbsEnableBuyClose { get => _absBuyClose.Value; set => _absBuyClose.Value = value; }

	/// <summary>
	/// Price shift added to the AbsolutelyNoLagLWMA line.
	/// </summary>
	public decimal AbsPriceShift { get => _absPriceShift.Value; set => _absPriceShift.Value = value; }

	/// <summary>
	/// Default market order volume.
	/// </summary>
	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = Math.Max(0.01m, value); }

	public BrainTrend2AbsolutelyNoLagLwmaMmrecStrategy()
	{
		_brainCandleType = Param(nameof(BrainCandleType), TimeSpan.FromHours(6).TimeFrame())
		.SetDisplay("Brain Candle", "Timeframe for BrainTrend2", "BrainTrend2");
		_brainAtrPeriod = Param(nameof(BrainAtrPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("Brain ATR", "ATR length for BrainTrend2", "BrainTrend2")
		.SetCanOptimize(true);
		_brainSignalBar = Param(nameof(BrainSignalBar), 1)
		.SetDisplay("Brain Signal Shift", "Bars to delay BrainTrend2 signal", "BrainTrend2")
		.SetCanOptimize(true);
		_brainBuyOpen = Param(nameof(BrainEnableBuyOpen), true)
		.SetDisplay("Brain Buy", "Enable BrainTrend2 buy entries", "BrainTrend2");
		_brainSellOpen = Param(nameof(BrainEnableSellOpen), true)
		.SetDisplay("Brain Sell", "Enable BrainTrend2 sell entries", "BrainTrend2");
		_brainSellClose = Param(nameof(BrainEnableSellClose), true)
		.SetDisplay("Brain Close Sells", "Allow BrainTrend2 to close shorts", "BrainTrend2");
		_brainBuyClose = Param(nameof(BrainEnableBuyClose), true)
		.SetDisplay("Brain Close Buys", "Allow BrainTrend2 to close longs", "BrainTrend2");

		_absCandleType = Param(nameof(AbsCandleType), TimeSpan.FromHours(6).TimeFrame())
		.SetDisplay("Abs Candle", "Timeframe for AbsolutelyNoLagLWMA", "AbsolutelyNoLag");
		_absLength = Param(nameof(AbsLength), 7)
		.SetGreaterThanZero()
		.SetDisplay("Abs Length", "LWMA length", "AbsolutelyNoLag")
		.SetCanOptimize(true);
		_absPriceMode = Param(nameof(AbsPriceMode), AppliedPrice.Close)
		.SetDisplay("Abs Price", "Price source", "AbsolutelyNoLag");
		_absSignalBar = Param(nameof(AbsSignalBar), 1)
		.SetDisplay("Abs Signal Shift", "Bars to delay AbsolutelyNoLagLWMA signal", "AbsolutelyNoLag")
		.SetCanOptimize(true);
		_absBuyOpen = Param(nameof(AbsEnableBuyOpen), true)
		.SetDisplay("Abs Buy", "Enable AbsolutelyNoLagLWMA buy entries", "AbsolutelyNoLag");
		_absSellOpen = Param(nameof(AbsEnableSellOpen), true)
		.SetDisplay("Abs Sell", "Enable AbsolutelyNoLagLWMA sell entries", "AbsolutelyNoLag");
		_absSellClose = Param(nameof(AbsEnableSellClose), true)
		.SetDisplay("Abs Close Sells", "Allow AbsolutelyNoLagLWMA to close shorts", "AbsolutelyNoLag");
		_absBuyClose = Param(nameof(AbsEnableBuyClose), true)
		.SetDisplay("Abs Close Buys", "Allow AbsolutelyNoLagLWMA to close longs", "AbsolutelyNoLag");
		_absPriceShift = Param(nameof(AbsPriceShift), 0m)
		.SetDisplay("Abs Shift", "Price shift added to LWMA", "AbsolutelyNoLag");

		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Default market order volume", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var seen = new HashSet<DataType>();
		if (seen.Add(BrainCandleType))
		yield return (Security, BrainCandleType);
		if (seen.Add(AbsCandleType))
		yield return (Security, AbsCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_brainColors.Clear();
		_absColors.Clear();
		_brainCalculator?.Reset();
		_absCalculator?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_brainCalculator = new BrainTrendCalculator(BrainAtrPeriod);
		_absCalculator = new AbsolutelyNoLagCalculator(AbsLength, AbsPriceMode);

		_brainColors.Clear();
		_absColors.Clear();

		var brainSubscription = SubscribeCandles(BrainCandleType);
		brainSubscription.Bind(ProcessBrainCandle).Start();

		var absSubscription = BrainCandleType == AbsCandleType
		? brainSubscription
		: SubscribeCandles(AbsCandleType);

		if (absSubscription != brainSubscription)
		{
			absSubscription.Bind(ProcessAbsCandle).Start();
		}
		else
		{
			absSubscription.Bind(ProcessAbsCandle);
		}
	}

	private void ProcessBrainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (_brainCalculator.Period != BrainAtrPeriod)
		{
			_brainCalculator.UpdatePeriod(BrainAtrPeriod);
		}

		var color = _brainCalculator.Process(candle);
		_brainColors.Add(color);
		EvaluateBrainSignals();
	}

	private void ProcessAbsCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (_absCalculator.Length != AbsLength || _absCalculator.PriceMode != AbsPriceMode)
		{
			_absCalculator.UpdateParameters(AbsLength, AbsPriceMode);
		}

		var color = _absCalculator.Process(candle, AbsPriceShift);
		_absColors.Add(color);
		EvaluateAbsSignals();
	}

	private void EvaluateBrainSignals()
	{
		var required = BrainSignalBar + 2;
		if (_brainColors.Count < required)
		{
			return;
		}

		var currentIndex = _brainColors.Count - 1 - BrainSignalBar;
		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
		{
			return;
		}

		var currentColor = _brainColors[currentIndex];
		var previousColor = _brainColors[previousIndex];

		var openLong = BrainEnableBuyOpen && currentColor < 2 && previousColor > 1;
		var closeShort = BrainEnableSellClose && currentColor < 2;
		var openShort = BrainEnableSellOpen && currentColor > 2 && previousColor < 3;
		var closeLong = BrainEnableBuyClose && currentColor > 2;

		HandleSignals(openLong, closeLong, openShort, closeShort);
	}

	private void EvaluateAbsSignals()
	{
		var required = AbsSignalBar + 2;
		if (_absColors.Count < required)
		{
			return;
		}

		var currentIndex = _absColors.Count - 1 - AbsSignalBar;
		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
		{
			return;
		}

		var currentColor = _absColors[currentIndex];
		var previousColor = _absColors[previousIndex];

		var openLong = AbsEnableBuyOpen && currentColor == 2 && previousColor != 2;
		var closeShort = AbsEnableSellClose && currentColor == 2;
		var openShort = AbsEnableSellOpen && currentColor == 0 && previousColor != 0;
		var closeLong = AbsEnableBuyClose && currentColor == 0;

		HandleSignals(openLong, closeLong, openShort, closeShort);
	}

	private void HandleSignals(bool openLong, bool closeLong, bool openShort, bool closeShort)
	{
		if (closeLong && Position > 0)
		SellMarket(Position);

		if (closeShort && Position < 0)
		BuyMarket(-Position);

		if (openLong)
		{
			if (Position < 0)
			{
				if (closeShort)
				BuyMarket(-Position);
				else
				openLong = false;
			}

			if (openLong && Position == 0)
			BuyMarket();
		}

		if (openShort)
		{
			if (Position > 0)
			{
				if (closeLong)
				SellMarket(Position);
				else
				openShort = false;
			}

			if (openShort && Position == 0)
			SellMarket();
		}
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

	private sealed class BrainTrendCalculator
	{
		private const decimal Dartp = 7m;
		private const decimal Cecf = 0.7m;

		private decimal[] _values;
		private int _index;
		private int _filled;
		private bool _river;
		private decimal _emaxtra;
		private bool _hasEmaxtra;
		private decimal? _prevClose;
		private decimal? _prevPrevClose;

		public BrainTrendCalculator(int period)
		{
			_values = Array.Empty<decimal>();
			UpdatePeriod(period);
		}

		public int Period { get; private set; }

		public void UpdatePeriod(int period)
		{
			Period = Math.Max(1, period);
			_values = new decimal[Period];
			Reset();
		}

		public void Reset()
		{
			Array.Clear(_values, 0, _values.Length);
			_index = 0;
			_filled = 0;
			_river = true;
			_emaxtra = 0m;
			_hasEmaxtra = false;
			_prevClose = null;
			_prevPrevClose = null;
		}

		public int Process(ICandleMessage candle)
		{
			var close = candle.ClosePrice;
			var open = candle.OpenPrice;
			var high = candle.HighPrice;
			var low = candle.LowPrice;

			if (_prevClose is null)
			{
				_prevClose = close;
				_values[_index] = high - low;
				AdvanceIndex();
				return 2;
			}

			if (_prevPrevClose is null)
			{
				_prevPrevClose = _prevClose;
				_prevClose = close;
				_river = _prevPrevClose < _prevClose;
				_emaxtra = _prevClose.Value;
				_hasEmaxtra = true;
				_values[_index] = Math.Max(high - low, Math.Abs(high - _prevPrevClose.Value));
				AdvanceIndex();
				return 2;
			}

			var prevClose = _prevClose.Value;
			var tr = high - low;
			var diffHigh = Math.Abs(high - prevClose);
			var diffLow = Math.Abs(low - prevClose);

			if (diffHigh > tr)
			tr = diffHigh;

			if (diffLow > tr)
			tr = diffLow;

			_values[_index] = tr;

			var atr = CalculateAtr();
			AdvanceIndex();

			if (!_hasEmaxtra)
			{
				_emaxtra = prevClose;
				_hasEmaxtra = true;
			}

			var widcha = Cecf * atr;

			if (_river && low < _emaxtra - widcha)
			{
				_river = false;
				_emaxtra = high;
			}
			else if (!_river && high > _emaxtra + widcha)
			{
				_river = true;
				_emaxtra = low;
			}
			else if (_river && low > _emaxtra)
			{
				_emaxtra = low;
			}
			else if (!_river && high < _emaxtra)
			{
				_emaxtra = high;
			}

			_prevPrevClose = _prevClose;
			_prevClose = close;

			if (_filled < Period)
			return 2;

			return _river
			? (open <= close ? 0 : 1)
			: (open >= close ? 4 : 3);
		}

		private decimal CalculateAtr()
		{
			decimal atr = 0m;
			var weight = Period;
			var idx = _index;

			for (var i = 0; i < Period; i++)
			{
				atr += _values[idx] * weight;
				weight--;
				idx--;
				if (idx < 0)
				idx = Period - 1;
			}

			return Period > 0 ? 2m * atr / (Dartp * (Dartp + 1m)) : 0m;
		}

		private void AdvanceIndex()
		{
			_index++;
			if (_index >= Period)
			_index = 0;
			if (_filled < Period)
			_filled++;
		}
	}

	private sealed class AbsolutelyNoLagCalculator
	{
		private decimal[] _priceValues;
		private decimal[] _lwmaValues;
		private int _index;
		private int _filled;
		private decimal? _prevValue;

		public AbsolutelyNoLagCalculator(int length, AppliedPrice priceMode)
		{
			_priceValues = Array.Empty<decimal>();
			_lwmaValues = Array.Empty<decimal>();
			UpdateParameters(length, priceMode);
		}

		public int Length { get; private set; }

		public AppliedPrice PriceMode { get; private set; }

		public void UpdateParameters(int length, AppliedPrice priceMode)
		{
			Length = Math.Max(1, length);
			PriceMode = priceMode;
			_priceValues = new decimal[Length];
			_lwmaValues = new decimal[Length];
			Reset();
		}

		public void Reset()
		{
			Array.Clear(_priceValues, 0, _priceValues.Length);
			Array.Clear(_lwmaValues, 0, _lwmaValues.Length);
			_index = 0;
			_filled = 0;
			_prevValue = null;
		}

		public int Process(ICandleMessage candle, decimal priceShift)
		{
			var price = SelectPrice(candle);
			_priceValues[_index] = price;

			var lwma1 = CalculateWeightedAverage(_priceValues);
			_lwmaValues[_index] = lwma1;

			var lwma2 = CalculateWeightedAverage(_lwmaValues);
			var value = lwma2 + priceShift;

			AdvanceIndex();

			if (_filled < Length)
			{
				_prevValue = value;
				return 1;
			}

			var prev = _prevValue ?? value;
			_prevValue = value;

			if (value > prev)
			return 2;

			if (value < prev)
			return 0;

			return 1;
		}

		private decimal CalculateWeightedAverage(decimal[] source)
		{
			decimal sum = 0m;
			decimal sumw = 0m;
			var weight = Length;
			var idx = _index;

			for (var i = 0; i < Length; i++)
			{
				sum += source[idx] * weight;
				sumw += weight;
				weight--;
				idx--;
				if (idx < 0)
				idx = Length - 1;
			}

			return sumw > 0m ? sum / sumw : 0m;
		}

		private void AdvanceIndex()
		{
			_index++;
			if (_index >= Length)
			_index = 0;
			if (_filled < Length)
			_filled++;
		}

		private decimal SelectPrice(ICandleMessage candle)
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
				AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
				AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
				AppliedPrice.DeMark => CalculateDeMarkPrice(candle),
				_ => candle.ClosePrice
			};
		}

		private static decimal CalculateDeMarkPrice(ICandleMessage candle)
		{
			var res = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

			if (candle.ClosePrice < candle.OpenPrice)
			{
				res = (res + candle.LowPrice) / 2m;
			}
			else if (candle.ClosePrice > candle.OpenPrice)
			{
				res = (res + candle.HighPrice) / 2m;
			}
			else
			{
				res = (res + candle.ClosePrice) / 2m;
			}

			return ((res - candle.LowPrice) + (res - candle.HighPrice)) / 2m;
		}
	}
}
