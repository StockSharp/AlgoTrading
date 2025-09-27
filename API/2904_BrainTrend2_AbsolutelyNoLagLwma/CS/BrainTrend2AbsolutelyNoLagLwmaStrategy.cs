namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Combined strategy that trades BrainTrend2 and AbsolutelyNoLagLWMA signals.
/// The logic keeps two virtual modules and aggregates their target positions.
/// </summary>
public class BrainTrend2AbsolutelyNoLagLwmaStrategy : Strategy
{
	private readonly StrategyParam<int> _brainTrendAtrPeriod;
	private readonly StrategyParam<int> _brainTrendSignalBar;
	private readonly StrategyParam<bool> _brainTrendBuyAllowed;
	private readonly StrategyParam<bool> _brainTrendSellAllowed;
	private readonly StrategyParam<decimal> _brainTrendVolume;
	private readonly StrategyParam<decimal> _brainTrendCoefficient;
	private readonly StrategyParam<DataType> _brainTrendCandleType;

	private readonly StrategyParam<int> _lwmaLength;
	private readonly StrategyParam<int> _lwmaSignalBar;
	private readonly StrategyParam<AppliedPriceMode> _lwmaAppliedPrice;
	private readonly StrategyParam<bool> _lwmaBuyAllowed;
	private readonly StrategyParam<bool> _lwmaSellAllowed;
	private readonly StrategyParam<bool> _lwmaCloseLongAllowed;
	private readonly StrategyParam<bool> _lwmaCloseShortAllowed;
	private readonly StrategyParam<decimal> _lwmaVolume;
	private readonly StrategyParam<DataType> _lwmaCandleType;

	private BrainTrend2Indicator _brainTrendIndicator = null!;
	private AbsolutelyNoLagLwmaIndicator _lwmaIndicator = null!;

	private readonly List<int> _brainTrendHistory = new();
	private readonly List<int> _lwmaHistory = new();

	private decimal _brainTrendTarget;
	private decimal _lwmaTarget;

	/// <summary>
	/// Period used by the BrainTrend2 ATR smoothing.
	/// </summary>
	public int BrainTrendAtrPeriod
	{
		get => _brainTrendAtrPeriod.Value;
		set => _brainTrendAtrPeriod.Value = value;
	}

	/// <summary>
	/// Number of finished candles to shift BrainTrend2 signals.
	/// </summary>
	public int BrainTrendSignalBar
	{
		get => _brainTrendSignalBar.Value;
		set => _brainTrendSignalBar.Value = value;
	}

	/// <summary>
	/// Allow BrainTrend2 module to open long positions.
	/// </summary>
	public bool BrainTrendBuyAllowed
	{
		get => _brainTrendBuyAllowed.Value;
		set => _brainTrendBuyAllowed.Value = value;
	}

	/// <summary>
	/// Allow BrainTrend2 module to open short positions.
	/// </summary>
	public bool BrainTrendSellAllowed
	{
		get => _brainTrendSellAllowed.Value;
		set => _brainTrendSellAllowed.Value = value;
	}

	/// <summary>
	/// Volume opened by the BrainTrend2 module.
	/// </summary>
	public decimal BrainTrendVolume
	{
		get => _brainTrendVolume.Value;
		set => _brainTrendVolume.Value = value;
	}

	/// <summary>
	/// ATR coefficient that scales the BrainTrend2 channel width.
	/// </summary>
	public decimal BrainTrendCoefficient
	{
		get => _brainTrendCoefficient.Value;
		set => _brainTrendCoefficient.Value = value;
	}

	/// <summary>
	/// Candle type used by the BrainTrend2 module.
	/// </summary>
	public DataType BrainTrendCandleType
	{
		get => _brainTrendCandleType.Value;
		set => _brainTrendCandleType.Value = value;
	}

	/// <summary>
	/// Length parameter for AbsolutelyNoLagLWMA.
	/// </summary>
	public int LwmaLength
	{
		get => _lwmaLength.Value;
		set => _lwmaLength.Value = value;
	}

	/// <summary>
	/// Number of finished candles to shift AbsolutelyNoLagLWMA signals.
	/// </summary>
	public int LwmaSignalBar
	{
		get => _lwmaSignalBar.Value;
		set => _lwmaSignalBar.Value = value;
	}

	/// <summary>
	/// Applied price type for AbsolutelyNoLagLWMA.
	/// </summary>
	public AppliedPriceMode LwmaAppliedPrice
	{
		get => _lwmaAppliedPrice.Value;
		set => _lwmaAppliedPrice.Value = value;
	}

	/// <summary>
	/// Allow AbsolutelyNoLagLWMA module to open long positions.
	/// </summary>
	public bool LwmaBuyAllowed
	{
		get => _lwmaBuyAllowed.Value;
		set => _lwmaBuyAllowed.Value = value;
	}

	/// <summary>
	/// Allow AbsolutelyNoLagLWMA module to open short positions.
	/// </summary>
	public bool LwmaSellAllowed
	{
		get => _lwmaSellAllowed.Value;
		set => _lwmaSellAllowed.Value = value;
	}

	/// <summary>
	/// Allow AbsolutelyNoLagLWMA module to close long positions when a down trend appears.
	/// </summary>
	public bool LwmaCloseLongAllowed
	{
		get => _lwmaCloseLongAllowed.Value;
		set => _lwmaCloseLongAllowed.Value = value;
	}

	/// <summary>
	/// Allow AbsolutelyNoLagLWMA module to close short positions when an up trend appears.
	/// </summary>
	public bool LwmaCloseShortAllowed
	{
		get => _lwmaCloseShortAllowed.Value;
		set => _lwmaCloseShortAllowed.Value = value;
	}

	/// <summary>
	/// Volume opened by the AbsolutelyNoLagLWMA module.
	/// </summary>
	public decimal LwmaVolume
	{
		get => _lwmaVolume.Value;
		set => _lwmaVolume.Value = value;
	}

	/// <summary>
	/// Candle type used by the AbsolutelyNoLagLWMA module.
	/// </summary>
	public DataType LwmaCandleType
	{
		get => _lwmaCandleType.Value;
		set => _lwmaCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BrainTrend2AbsolutelyNoLagLwmaStrategy"/>.
	/// </summary>
	public BrainTrend2AbsolutelyNoLagLwmaStrategy()
	{
		_brainTrendAtrPeriod = Param(nameof(BrainTrendAtrPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("BrainTrend ATR", "ATR length for BrainTrend2", "BrainTrend2");

		_brainTrendSignalBar = Param(nameof(BrainTrendSignalBar), 1)
		.SetNotNegative()
		.SetDisplay("BrainTrend Signal Bar", "Shift for BrainTrend2 signals", "BrainTrend2");

		_brainTrendBuyAllowed = Param(nameof(BrainTrendBuyAllowed), true)
		.SetDisplay("BrainTrend Buy", "Allow long entries", "BrainTrend2");

		_brainTrendSellAllowed = Param(nameof(BrainTrendSellAllowed), true)
		.SetDisplay("BrainTrend Sell", "Allow short entries", "BrainTrend2");

		_brainTrendVolume = Param(nameof(BrainTrendVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("BrainTrend Volume", "Volume for BrainTrend2 trades", "BrainTrend2");

		_brainTrendCoefficient = Param(nameof(BrainTrendCoefficient), 0.7m)
		.SetGreaterThanZero()
		.SetDisplay("BrainTrend Coefficient", "ATR multiplier used in BrainTrend2", "BrainTrend2");

		_brainTrendCandleType = Param(nameof(BrainTrendCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("BrainTrend Candle", "Candle type for BrainTrend2", "BrainTrend2");

		_lwmaLength = Param(nameof(LwmaLength), 7)
		.SetGreaterThanZero()
		.SetDisplay("LWMA Length", "Length of AbsolutelyNoLagLWMA", "LWMA");

		_lwmaSignalBar = Param(nameof(LwmaSignalBar), 1)
		.SetNotNegative()
		.SetDisplay("LWMA Signal Bar", "Shift for LWMA signals", "LWMA");

		_lwmaAppliedPrice = Param(nameof(LwmaAppliedPrice), AppliedPriceMode.Close)
		.SetDisplay("LWMA Price", "Applied price for LWMA", "LWMA");

		_lwmaBuyAllowed = Param(nameof(LwmaBuyAllowed), true)
		.SetDisplay("LWMA Buy", "Allow LWMA long entries", "LWMA");

		_lwmaSellAllowed = Param(nameof(LwmaSellAllowed), true)
		.SetDisplay("LWMA Sell", "Allow LWMA short entries", "LWMA");

		_lwmaCloseLongAllowed = Param(nameof(LwmaCloseLongAllowed), true)
		.SetDisplay("LWMA Close Long", "Allow LWMA to close longs", "LWMA");

		_lwmaCloseShortAllowed = Param(nameof(LwmaCloseShortAllowed), true)
		.SetDisplay("LWMA Close Short", "Allow LWMA to close shorts", "LWMA");

		_lwmaVolume = Param(nameof(LwmaVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("LWMA Volume", "Volume for LWMA trades", "LWMA");

		_lwmaCandleType = Param(nameof(LwmaCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("LWMA Candle", "Candle type for LWMA", "LWMA");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var seen = new HashSet<DataType>();

		if (seen.Add(BrainTrendCandleType))
			yield return (Security, BrainTrendCandleType);

		if (seen.Add(LwmaCandleType))
			yield return (Security, LwmaCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_brainTrendIndicator?.Reset();
		_lwmaIndicator?.Reset();

		_brainTrendHistory.Clear();
		_lwmaHistory.Clear();

		_brainTrendTarget = 0m;
		_lwmaTarget = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_brainTrendIndicator = new BrainTrend2Indicator
		{
			AtrPeriod = BrainTrendAtrPeriod,
			Coefficient = BrainTrendCoefficient,
		};

		_lwmaIndicator = new AbsolutelyNoLagLwmaIndicator
		{
			Length = LwmaLength,
			AppliedPrice = LwmaAppliedPrice,
		};

		var brainSubscription = SubscribeCandles(BrainTrendCandleType);
		brainSubscription
		.Bind(_brainTrendIndicator, ProcessBrainTrend)
		.Start();

		var lwmaSubscription = SubscribeCandles(LwmaCandleType);
		lwmaSubscription
		.Bind(_lwmaIndicator, ProcessLwma)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			if (brainSubscription == lwmaSubscription)
			{
				DrawCandles(area, brainSubscription);
			}
			else
			{
				DrawCandles(area, brainSubscription);
				DrawCandles(area, lwmaSubscription);
			}

			DrawOwnTrades(area);
		}
	}

	private void ProcessBrainTrend(ICandleMessage candle, decimal colorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_brainTrendIndicator.IsFormed)
			return;

		var color = (int)colorValue;
		_brainTrendHistory.Add(color);

		TrimHistory(_brainTrendHistory, BrainTrendSignalBar);

		var signalIndex = BrainTrendSignalBar;
		if (_brainTrendHistory.Count <= signalIndex)
			return;

		var currentIndex = _brainTrendHistory.Count - signalIndex - 1;
		if (currentIndex < 0)
			return;

		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
			return;

		var currentColor = _brainTrendHistory[currentIndex];
		var previousColor = _brainTrendHistory[previousIndex];

		var changed = false;

		if (currentColor < 2)
		{
			if (_brainTrendTarget < 0m)
			{
				_brainTrendTarget = 0m;
				changed = true;
			}

			if (BrainTrendBuyAllowed && previousColor > 1 && _brainTrendTarget <= 0m)
			{
				_brainTrendTarget = BrainTrendVolume;
				changed = true;
			}
		}
		else if (currentColor > 2)
		{
			if (_brainTrendTarget > 0m)
			{
				_brainTrendTarget = 0m;
				changed = true;
			}

			if (BrainTrendSellAllowed && previousColor < 3 && _brainTrendTarget >= 0m)
			{
				_brainTrendTarget = -BrainTrendVolume;
				changed = true;
			}
		}

		if (changed)
			RebalancePosition();
	}

	private void ProcessLwma(ICandleMessage candle, decimal colorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_lwmaIndicator.IsFormed)
			return;

		var color = (int)colorValue;
		_lwmaHistory.Add(color);

		TrimHistory(_lwmaHistory, LwmaSignalBar);

		var signalIndex = LwmaSignalBar;
		if (_lwmaHistory.Count <= signalIndex)
			return;

		var currentIndex = _lwmaHistory.Count - signalIndex - 1;
		if (currentIndex < 0)
			return;

		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
			return;

		var currentColor = _lwmaHistory[currentIndex];
		var previousColor = _lwmaHistory[previousIndex];

		var changed = false;

		if (currentColor == 2)
		{
			if (LwmaCloseShortAllowed && _lwmaTarget < 0m)
			{
				_lwmaTarget = 0m;
				changed = true;
			}

			if (LwmaBuyAllowed && previousColor != 2 && _lwmaTarget <= 0m)
			{
				_lwmaTarget = LwmaVolume;
				changed = true;
			}
		}
		else if (currentColor == 0)
		{
			if (LwmaCloseLongAllowed && _lwmaTarget > 0m)
			{
				_lwmaTarget = 0m;
				changed = true;
			}

			if (LwmaSellAllowed && previousColor != 0 && _lwmaTarget >= 0m)
			{
				_lwmaTarget = -LwmaVolume;
				changed = true;
			}
		}

		if (changed)
			RebalancePosition();
	}

	private void RebalancePosition()
	{
		var target = _brainTrendTarget + _lwmaTarget;
		var diff = target - Position;

		if (diff > 0m)
		{
			BuyMarket(diff);
		}
		else if (diff < 0m)
		{
			SellMarket(-diff);
		}
	}

	private static void TrimHistory(List<int> history, int signalBar)
	{
		var maxSize = Math.Max(signalBar + 5, 10);
		if (history.Count > maxSize)
		{
			history.RemoveRange(0, history.Count - maxSize);
		}
	}

	/// <summary>
	/// Applied price options for AbsolutelyNoLagLWMA.
	/// </summary>
	public enum AppliedPriceMode
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
		Demark,
	}

	private class BrainTrend2Indicator : Indicator<ICandleMessage>
	{
		private decimal _coefficient = 0.7m;

		private decimal[] _trValues = Array.Empty<decimal>();
		private int _index;
		private bool _initialized;
		private bool _river = true;
		private bool _riverDefined;
		private decimal _emaxtra;
		private decimal? _prevClose;
		private decimal? _prevPrevClose;

		public int AtrPeriod { get; set; } = 7;

		public decimal Coefficient
		{
			get => _coefficient;
			set => _coefficient = value;
		}

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();

			if (AtrPeriod <= 0)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 2m, input.Time);
			}

			EnsureCapacity();

			var prevClose = _prevClose ?? candle.ClosePrice;
			var high = candle.HighPrice;
			var low = candle.LowPrice;
			var highWithSpread = high;

			var tr = highWithSpread - low;
			var diffHigh = Math.Abs(highWithSpread - prevClose);
			var diffLow = Math.Abs(low - prevClose);

			if (diffHigh > tr)
				tr = diffHigh;
			if (diffLow > tr)
				tr = diffLow;

			if (!_initialized)
			{
				for (var i = 0; i < _trValues.Length; i++)
				_trValues[i] = tr;

				_initialized = true;
			}

			_trValues[_index] = tr;

			var atr = CalculateAtr();

			_index++;
			if (_index == _trValues.Length)
				_index = 0;

			if (!_riverDefined)
			{
				if (_prevPrevClose.HasValue && _prevClose.HasValue)
				{
					_river = _prevPrevClose.Value <= _prevClose.Value;
					_emaxtra = _prevClose.Value;
					_riverDefined = true;
				}
				else
				{
					_emaxtra = candle.ClosePrice;
				}
			}

			var widcha = _coefficient * atr;

			if (_river)
			{
				if (low < _emaxtra - widcha)
				{
					_river = false;
					_emaxtra = highWithSpread;
				}
				else if (low > _emaxtra)
				{
					_emaxtra = low;
				}
			}
			else
			{
				if (highWithSpread > _emaxtra + widcha)
				{
					_river = true;
					_emaxtra = low;
				}
				else if (highWithSpread < _emaxtra)
				{
					_emaxtra = highWithSpread;
				}
			}

			var color = _river
			? (candle.OpenPrice <= candle.ClosePrice ? 0m : 1m)
			: (candle.OpenPrice >= candle.ClosePrice ? 4m : 3m);

			_prevPrevClose = _prevClose;
			_prevClose = candle.ClosePrice;

			IsFormed = _riverDefined;

			return new DecimalIndicatorValue(this, color, input.Time);
		}

		public override void Reset()
		{
			base.Reset();

			_trValues = Array.Empty<decimal>();
			_index = 0;
			_initialized = false;
			_river = true;
			_riverDefined = false;
			_emaxtra = 0m;
			_prevClose = null;
			_prevPrevClose = null;
		}

		private void EnsureCapacity()
		{
			if (_trValues.Length == AtrPeriod)
				return;

			_trValues = new decimal[AtrPeriod];
			_index = 0;
			_initialized = false;
		}

		private decimal CalculateAtr()
		{
			var count = _trValues.Length;
			if (count == 0)
				return 0m;

			decimal atr = 0m;
			var weight = count;
			var idx = _index;

			for (var i = 0; i < count; i++)
			{
				atr += _trValues[idx] * weight;
				weight--;

				if (idx == 0)
					idx = count - 1;
				else
				idx--;
			}

			var period = (decimal)count;
			return period > 0m ? 2m * atr / (period * (period + 1m)) : 0m;
		}
	}

	private class AbsolutelyNoLagLwmaIndicator : Indicator<ICandleMessage>
	{
		private decimal[] _priceBuffer = Array.Empty<decimal>();
		private decimal[] _lwmaBuffer = Array.Empty<decimal>();
		private int _index;
		private int _filled;
		private decimal? _prevLine;

		public int Length { get; set; } = 7;
		public AppliedPriceMode AppliedPrice { get; set; } = AppliedPriceMode.Close;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();

			if (Length <= 0)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 1m, input.Time);
			}

			EnsureCapacity();

			var price = GetPrice(candle);
			_priceBuffer[_index] = price;

			var lwma = CalculateWeighted(_priceBuffer);
			_lwmaBuffer[_index] = lwma;

			var line = CalculateWeighted(_lwmaBuffer);

			_index++;
			if (_index == _priceBuffer.Length)
				_index = 0;

			if (_filled < _priceBuffer.Length)
				_filled++;

			var color = 1m;
			if (_prevLine.HasValue)
			{
				if (line > _prevLine.Value)
					color = 2m;
				else if (line < _prevLine.Value)
					color = 0m;
			}

			_prevLine = line;

			IsFormed = _filled >= _priceBuffer.Length;

			return new DecimalIndicatorValue(this, color, input.Time);
		}

		public override void Reset()
		{
			base.Reset();

			_priceBuffer = Array.Empty<decimal>();
			_lwmaBuffer = Array.Empty<decimal>();
			_index = 0;
			_filled = 0;
			_prevLine = null;
		}

		private void EnsureCapacity()
		{
			if (_priceBuffer.Length == Length)
				return;

			_priceBuffer = new decimal[Length];
			_lwmaBuffer = new decimal[Length];
			_index = 0;
			_filled = 0;
			_prevLine = null;
		}

		private decimal CalculateWeighted(decimal[] buffer)
		{
			if (buffer.Length == 0)
				return 0m;

			decimal sum = 0m;
			decimal sumWeight = 0m;
			var weight = buffer.Length;
			var idx = _index;

			for (var i = 0; i < buffer.Length; i++)
			{
				sum += buffer[idx] * weight;
				sumWeight += weight;
				weight--;

				if (idx == 0)
					idx = buffer.Length - 1;
				else
				idx--;
			}

			return sumWeight > 0m ? sum / sumWeight : 0m;
		}

		private decimal GetPrice(ICandleMessage candle)
		{
			return AppliedPrice switch
			{
				AppliedPriceMode.Close => candle.ClosePrice,
				AppliedPriceMode.Open => candle.OpenPrice,
				AppliedPriceMode.High => candle.HighPrice,
				AppliedPriceMode.Low => candle.LowPrice,
				AppliedPriceMode.Median => (candle.HighPrice + candle.LowPrice) / 2m,
				AppliedPriceMode.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
				AppliedPriceMode.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
				AppliedPriceMode.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
				AppliedPriceMode.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
				AppliedPriceMode.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
				AppliedPriceMode.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
				AppliedPriceMode.Demark => CalculateDemarkPrice(candle),
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
	}
}
