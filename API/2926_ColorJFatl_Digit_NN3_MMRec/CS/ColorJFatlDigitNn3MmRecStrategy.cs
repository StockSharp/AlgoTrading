using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe conversion of the ColorJFatl Digit NN3 MMRec expert advisor.
/// Three independent modules monitor different timeframes and react to slope changes of a Jurik MA.
/// Signals close opposite positions and optionally open new ones using the shared portfolio.
/// </summary>
public class ColorJFatlDigitNn3MmRecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeA;
	private readonly StrategyParam<int> _jmaLengthA;
	private readonly StrategyParam<int> _jmaPhaseA;
	private readonly StrategyParam<int> _signalBarA;
	private readonly StrategyParam<AppliedPrice> _appliedPriceA;
	private readonly StrategyParam<bool> _allowBuyOpenA;
	private readonly StrategyParam<bool> _allowSellOpenA;
	private readonly StrategyParam<bool> _allowBuyCloseA;
	private readonly StrategyParam<bool> _allowSellCloseA;
	private readonly StrategyParam<decimal> _volumeA;

	private readonly StrategyParam<DataType> _candleTypeB;
	private readonly StrategyParam<int> _jmaLengthB;
	private readonly StrategyParam<int> _jmaPhaseB;
	private readonly StrategyParam<int> _signalBarB;
	private readonly StrategyParam<AppliedPrice> _appliedPriceB;
	private readonly StrategyParam<bool> _allowBuyOpenB;
	private readonly StrategyParam<bool> _allowSellOpenB;
	private readonly StrategyParam<bool> _allowBuyCloseB;
	private readonly StrategyParam<bool> _allowSellCloseB;
	private readonly StrategyParam<decimal> _volumeB;

	private readonly StrategyParam<DataType> _candleTypeC;
	private readonly StrategyParam<int> _jmaLengthC;
	private readonly StrategyParam<int> _jmaPhaseC;
	private readonly StrategyParam<int> _signalBarC;
	private readonly StrategyParam<AppliedPrice> _appliedPriceC;
	private readonly StrategyParam<bool> _allowBuyOpenC;
	private readonly StrategyParam<bool> _allowSellOpenC;
	private readonly StrategyParam<bool> _allowBuyCloseC;
	private readonly StrategyParam<bool> _allowSellCloseC;
	private readonly StrategyParam<decimal> _volumeC;

	private readonly SignalModule _moduleA;
	private readonly SignalModule _moduleB;
	private readonly SignalModule _moduleC;

	/// <summary>
	/// Initializes a new instance of the strategy and configures default parameters.
	/// </summary>
	public ColorJFatlDigitNn3MmRecStrategy()
	{
		_candleTypeA = Param(nameof(CandleTypeA), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("A Candle Type", "Timeframe for module A", "Module A");
		_jmaLengthA = Param(nameof(JmaLengthA), 5)
		.SetGreaterThanZero()
		.SetDisplay("A JMA Length", "Period of the Jurik MA for module A", "Module A");
		_jmaPhaseA = Param(nameof(JmaPhaseA), -100)
		.SetDisplay("A JMA Phase", "Phase parameter from the original script (not used by StockSharp JMA)", "Module A");
		_signalBarA = Param(nameof(SignalBarA), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("A Signal Bar", "Delay in bars before acting on module A signals", "Module A");
		_appliedPriceA = Param(nameof(AppliedPriceA), AppliedPrice.Close)
		.SetDisplay("A Applied Price", "Price source used by module A", "Module A");
		_allowBuyOpenA = Param(nameof(AllowBuyOpenA), true)
		.SetDisplay("A Allow Buy Open", "Enable opening long positions for module A", "Module A");
		_allowSellOpenA = Param(nameof(AllowSellOpenA), true)
		.SetDisplay("A Allow Sell Open", "Enable opening short positions for module A", "Module A");
		_allowBuyCloseA = Param(nameof(AllowBuyCloseA), true)
		.SetDisplay("A Allow Buy Close", "Allow module A to close long positions", "Module A");
		_allowSellCloseA = Param(nameof(AllowSellCloseA), true)
		.SetDisplay("A Allow Sell Close", "Allow module A to close short positions", "Module A");
		_volumeA = Param(nameof(VolumeA), 1m)
		.SetGreaterThanZero()
		.SetDisplay("A Volume", "Order volume used by module A", "Module A");

		_candleTypeB = Param(nameof(CandleTypeB), TimeSpan.FromHours(8).TimeFrame())
		.SetDisplay("B Candle Type", "Timeframe for module B", "Module B");
		_jmaLengthB = Param(nameof(JmaLengthB), 5)
		.SetGreaterThanZero()
		.SetDisplay("B JMA Length", "Period of the Jurik MA for module B", "Module B");
		_jmaPhaseB = Param(nameof(JmaPhaseB), -100)
		.SetDisplay("B JMA Phase", "Phase parameter from the original script (not used by StockSharp JMA)", "Module B");
		_signalBarB = Param(nameof(SignalBarB), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("B Signal Bar", "Delay in bars before acting on module B signals", "Module B");
		_appliedPriceB = Param(nameof(AppliedPriceB), AppliedPrice.Close)
		.SetDisplay("B Applied Price", "Price source used by module B", "Module B");
		_allowBuyOpenB = Param(nameof(AllowBuyOpenB), true)
		.SetDisplay("B Allow Buy Open", "Enable opening long positions for module B", "Module B");
		_allowSellOpenB = Param(nameof(AllowSellOpenB), true)
		.SetDisplay("B Allow Sell Open", "Enable opening short positions for module B", "Module B");
		_allowBuyCloseB = Param(nameof(AllowBuyCloseB), true)
		.SetDisplay("B Allow Buy Close", "Allow module B to close long positions", "Module B");
		_allowSellCloseB = Param(nameof(AllowSellCloseB), true)
		.SetDisplay("B Allow Sell Close", "Allow module B to close short positions", "Module B");
		_volumeB = Param(nameof(VolumeB), 1m)
		.SetGreaterThanZero()
		.SetDisplay("B Volume", "Order volume used by module B", "Module B");

		_candleTypeC = Param(nameof(CandleTypeC), TimeSpan.FromHours(3).TimeFrame())
		.SetDisplay("C Candle Type", "Timeframe for module C", "Module C");
		_jmaLengthC = Param(nameof(JmaLengthC), 5)
		.SetGreaterThanZero()
		.SetDisplay("C JMA Length", "Period of the Jurik MA for module C", "Module C");
		_jmaPhaseC = Param(nameof(JmaPhaseC), -100)
		.SetDisplay("C JMA Phase", "Phase parameter from the original script (not used by StockSharp JMA)", "Module C");
		_signalBarC = Param(nameof(SignalBarC), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("C Signal Bar", "Delay in bars before acting on module C signals", "Module C");
		_appliedPriceC = Param(nameof(AppliedPriceC), AppliedPrice.Close)
		.SetDisplay("C Applied Price", "Price source used by module C", "Module C");
		_allowBuyOpenC = Param(nameof(AllowBuyOpenC), true)
		.SetDisplay("C Allow Buy Open", "Enable opening long positions for module C", "Module C");
		_allowSellOpenC = Param(nameof(AllowSellOpenC), true)
		.SetDisplay("C Allow Sell Open", "Enable opening short positions for module C", "Module C");
		_allowBuyCloseC = Param(nameof(AllowBuyCloseC), true)
		.SetDisplay("C Allow Buy Close", "Allow module C to close long positions", "Module C");
		_allowSellCloseC = Param(nameof(AllowSellCloseC), true)
		.SetDisplay("C Allow Sell Close", "Allow module C to close short positions", "Module C");
		_volumeC = Param(nameof(VolumeC), 1m)
		.SetGreaterThanZero()
		.SetDisplay("C Volume", "Order volume used by module C", "Module C");

		_moduleA = new SignalModule(this, _candleTypeA, _jmaLengthA, _signalBarA, _appliedPriceA, _allowBuyOpenA, _allowSellOpenA, _allowBuyCloseA, _allowSellCloseA, _volumeA);
		_moduleB = new SignalModule(this, _candleTypeB, _jmaLengthB, _signalBarB, _appliedPriceB, _allowBuyOpenB, _allowSellOpenB, _allowBuyCloseB, _allowSellCloseB, _volumeB);
		_moduleC = new SignalModule(this, _candleTypeC, _jmaLengthC, _signalBarC, _appliedPriceC, _allowBuyOpenC, _allowSellOpenC, _allowBuyCloseC, _allowSellCloseC, _volumeC);
	}

	/// <summary>
	/// Candle type processed by module A.
	/// </summary>
	public DataType CandleTypeA
	{
		get => _candleTypeA.Value;
		set => _candleTypeA.Value = value;
	}

	/// <summary>
	/// Jurik MA length used by module A.
	/// </summary>
	public int JmaLengthA
	{
		get => _jmaLengthA.Value;
		set => _jmaLengthA.Value = value;
	}

	/// <summary>
	/// Phase setting passed through for module A (kept for documentation).
	/// </summary>
	public int JmaPhaseA
	{
		get => _jmaPhaseA.Value;
		set => _jmaPhaseA.Value = value;
	}

	/// <summary>
	/// Number of completed bars waited before acting on module A signals.
	/// </summary>
	public int SignalBarA
	{
		get => _signalBarA.Value;
		set => _signalBarA.Value = value;
	}

	/// <summary>
	/// Applied price mode for module A.
	/// </summary>
	public AppliedPrice AppliedPriceA
	{
		get => _appliedPriceA.Value;
		set => _appliedPriceA.Value = value;
	}

	/// <summary>
	/// Enable opening long trades for module A.
	/// </summary>
	public bool AllowBuyOpenA
	{
		get => _allowBuyOpenA.Value;
		set => _allowBuyOpenA.Value = value;
	}

	/// <summary>
	/// Enable opening short trades for module A.
	/// </summary>
	public bool AllowSellOpenA
	{
		get => _allowSellOpenA.Value;
		set => _allowSellOpenA.Value = value;
	}

	/// <summary>
	/// Allow module A to close long positions.
	/// </summary>
	public bool AllowBuyCloseA
	{
		get => _allowBuyCloseA.Value;
		set => _allowBuyCloseA.Value = value;
	}

	/// <summary>
	/// Allow module A to close short positions.
	/// </summary>
	public bool AllowSellCloseA
	{
		get => _allowSellCloseA.Value;
		set => _allowSellCloseA.Value = value;
	}

	/// <summary>
	/// Order volume for module A.
	/// </summary>
	public decimal VolumeA
	{
		get => _volumeA.Value;
		set => _volumeA.Value = value;
	}

	/// <summary>
	/// Candle type processed by module B.
	/// </summary>
	public DataType CandleTypeB
	{
		get => _candleTypeB.Value;
		set => _candleTypeB.Value = value;
	}

	/// <summary>
	/// Jurik MA length used by module B.
	/// </summary>
	public int JmaLengthB
	{
		get => _jmaLengthB.Value;
		set => _jmaLengthB.Value = value;
	}

	/// <summary>
	/// Phase setting passed through for module B (kept for documentation).
	/// </summary>
	public int JmaPhaseB
	{
		get => _jmaPhaseB.Value;
		set => _jmaPhaseB.Value = value;
	}

	/// <summary>
	/// Number of completed bars waited before acting on module B signals.
	/// </summary>
	public int SignalBarB
	{
		get => _signalBarB.Value;
		set => _signalBarB.Value = value;
	}

	/// <summary>
	/// Applied price mode for module B.
	/// </summary>
	public AppliedPrice AppliedPriceB
	{
		get => _appliedPriceB.Value;
		set => _appliedPriceB.Value = value;
	}

	/// <summary>
	/// Enable opening long trades for module B.
	/// </summary>
	public bool AllowBuyOpenB
	{
		get => _allowBuyOpenB.Value;
		set => _allowBuyOpenB.Value = value;
	}

	/// <summary>
	/// Enable opening short trades for module B.
	/// </summary>
	public bool AllowSellOpenB
	{
		get => _allowSellOpenB.Value;
		set => _allowSellOpenB.Value = value;
	}

	/// <summary>
	/// Allow module B to close long positions.
	/// </summary>
	public bool AllowBuyCloseB
	{
		get => _allowBuyCloseB.Value;
		set => _allowBuyCloseB.Value = value;
	}

	/// <summary>
	/// Allow module B to close short positions.
	/// </summary>
	public bool AllowSellCloseB
	{
		get => _allowSellCloseB.Value;
		set => _allowSellCloseB.Value = value;
	}

	/// <summary>
	/// Order volume for module B.
	/// </summary>
	public decimal VolumeB
	{
		get => _volumeB.Value;
		set => _volumeB.Value = value;
	}

	/// <summary>
	/// Candle type processed by module C.
	/// </summary>
	public DataType CandleTypeC
	{
		get => _candleTypeC.Value;
		set => _candleTypeC.Value = value;
	}

	/// <summary>
	/// Jurik MA length used by module C.
	/// </summary>
	public int JmaLengthC
	{
		get => _jmaLengthC.Value;
		set => _jmaLengthC.Value = value;
	}

	/// <summary>
	/// Phase setting passed through for module C (kept for documentation).
	/// </summary>
	public int JmaPhaseC
	{
		get => _jmaPhaseC.Value;
		set => _jmaPhaseC.Value = value;
	}

	/// <summary>
	/// Number of completed bars waited before acting on module C signals.
	/// </summary>
	public int SignalBarC
	{
		get => _signalBarC.Value;
		set => _signalBarC.Value = value;
	}

	/// <summary>
	/// Applied price mode for module C.
	/// </summary>
	public AppliedPrice AppliedPriceC
	{
		get => _appliedPriceC.Value;
		set => _appliedPriceC.Value = value;
	}

	/// <summary>
	/// Enable opening long trades for module C.
	/// </summary>
	public bool AllowBuyOpenC
	{
		get => _allowBuyOpenC.Value;
		set => _allowBuyOpenC.Value = value;
	}

	/// <summary>
	/// Enable opening short trades for module C.
	/// </summary>
	public bool AllowSellOpenC
	{
		get => _allowSellOpenC.Value;
		set => _allowSellOpenC.Value = value;
	}

	/// <summary>
	/// Allow module C to close long positions.
	/// </summary>
	public bool AllowBuyCloseC
	{
		get => _allowBuyCloseC.Value;
		set => _allowBuyCloseC.Value = value;
	}

	/// <summary>
	/// Allow module C to close short positions.
	/// </summary>
	public bool AllowSellCloseC
	{
		get => _allowSellCloseC.Value;
		set => _allowSellCloseC.Value = value;
	}

	/// <summary>
	/// Order volume for module C.
	/// </summary>
	public decimal VolumeC
	{
		get => _volumeC.Value;
		set => _volumeC.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var types = new List<DataType>();

		void AddType(DataType type)
		{
			if (!types.Contains(type))
			types.Add(type);
		}

		AddType(CandleTypeA);
		AddType(CandleTypeB);
		AddType(CandleTypeC);

		foreach (var type in types)
		yield return (Security, type);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_moduleA.Reset();
		_moduleB.Reset();
		_moduleC.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_moduleA.Start();
		_moduleB.Start();
		_moduleC.Start();

		// Enable default risk protections.
		StartProtection();
	}

	private static decimal SelectPrice(ICandleMessage candle, AppliedPrice mode)
	{
		return mode switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPrice.Weighted => (candle.ClosePrice * 2m + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice
			? candle.HighPrice
			: candle.ClosePrice < candle.OpenPrice
			? candle.LowPrice
			: candle.ClosePrice,
			AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
			? (candle.HighPrice + candle.ClosePrice) / 2m
			: candle.ClosePrice < candle.OpenPrice
			? (candle.LowPrice + candle.ClosePrice) / 2m
			: candle.ClosePrice,
			AppliedPrice.DeMark => CalculateDeMark(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal CalculateDeMark(ICandleMessage candle)
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

	private enum SignalColor
	{
		Down,
		Neutral,
		Up
	}

	private class SignalModule
	{
		private readonly ColorJFatlDigitNn3MmRecStrategy _strategy;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _jmaLength;
		private readonly StrategyParam<int> _signalBar;
		private readonly StrategyParam<AppliedPrice> _appliedPrice;
		private readonly StrategyParam<bool> _allowBuyOpen;
		private readonly StrategyParam<bool> _allowSellOpen;
		private readonly StrategyParam<bool> _allowBuyClose;
		private readonly StrategyParam<bool> _allowSellClose;
		private readonly StrategyParam<decimal> _volume;

		private JurikMovingAverage? _jma;
		private decimal? _prevJma;
		private SignalColor _lastColor = SignalColor.Neutral;
		private readonly Queue<SignalColor> _pending = new();
		private SignalColor? _lastProcessed;

		public SignalModule(
		ColorJFatlDigitNn3MmRecStrategy strategy,
		StrategyParam<DataType> candleType,
		StrategyParam<int> jmaLength,
		StrategyParam<int> signalBar,
		StrategyParam<AppliedPrice> appliedPrice,
		StrategyParam<bool> allowBuyOpen,
		StrategyParam<bool> allowSellOpen,
		StrategyParam<bool> allowBuyClose,
		StrategyParam<bool> allowSellClose,
		StrategyParam<decimal> volume)
		{
			_strategy = strategy;
			_candleType = candleType;
			_jmaLength = jmaLength;
			_signalBar = signalBar;
			_appliedPrice = appliedPrice;
			_allowBuyOpen = allowBuyOpen;
			_allowSellOpen = allowSellOpen;
			_allowBuyClose = allowBuyClose;
			_allowSellClose = allowSellClose;
			_volume = volume;
		}

		public void Start()
		{
			_jma = new JurikMovingAverage { Length = _jmaLength.Value };

			var subscription = _strategy.SubscribeCandles(_candleType.Value);
			subscription.Bind(ProcessCandle).Start();
		}

		public void Reset()
		{
			_jma = null;
			_prevJma = null;
			_lastColor = SignalColor.Neutral;
			_pending.Clear();
			_lastProcessed = null;
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished || _jma == null)
			return;

			var price = SelectPrice(candle, _appliedPrice.Value);
			var jmaValue = _jma.Process(price, candle.OpenTime, true).GetValue<decimal>();

			if (!_jma.IsFormed)
			{
				_prevJma = jmaValue;
				return;
			}

			var prev = _prevJma ?? jmaValue;
			var diff = jmaValue - prev;

			var color = diff > 0m
			? SignalColor.Up
			: diff < 0m
			? SignalColor.Down
			: _lastColor;

			_prevJma = jmaValue;
			_lastColor = color;

			_pending.Enqueue(color);

			var maxSize = Math.Max(1, _signalBar.Value + 1);
			while (_pending.Count > maxSize)
			_pending.Dequeue();

			if (_pending.Count <= _signalBar.Value)
			return;

			var signalColor = _pending.Dequeue();
			var previous = _lastProcessed ?? SignalColor.Neutral;

			if (signalColor == previous)
			{
				_lastProcessed = signalColor;
				return;
			}

			if (!_strategy.IsFormedAndOnlineAndAllowTrading())
			{
				_lastProcessed = signalColor;
				return;
			}

			switch (signalColor)
			{
				case SignalColor.Up:
				HandleUpSignal();
				break;
				case SignalColor.Down:
				HandleDownSignal();
				break;
			}

			_lastProcessed = signalColor;
		}

		private void HandleUpSignal()
		{
			if (_allowSellClose.Value && _strategy.Position < 0)
			{
				// Close existing short exposure before switching to long side.
				_strategy.BuyMarket(Math.Abs(_strategy.Position));
			}

			if (_allowBuyOpen.Value && _strategy.Position <= 0)
			{
				var volume = _volume.Value;
				if (volume > 0m)
				{
					// Open new long exposure.
					_strategy.BuyMarket(volume);
				}
			}
		}

		private void HandleDownSignal()
		{
			if (_allowBuyClose.Value && _strategy.Position > 0)
			{
				// Close existing long exposure before switching to short side.
				_strategy.SellMarket(_strategy.Position);
			}

			if (_allowSellOpen.Value && _strategy.Position >= 0)
			{
				var volume = _volume.Value;
				if (volume > 0m)
				{
					// Open new short exposure.
					_strategy.SellMarket(volume);
				}
			}
		}
	}

	/// <summary>
	/// Applied price modes replicated from the original indicator.
	/// </summary>
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
}
