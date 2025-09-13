using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Oracle indicator combining RSI and CCI.
/// Supports three signal modes: zero line breakdown, direction twist,
/// and crossing between indicator and its signal line.
/// </summary>
public class ExpOracleStrategy : Strategy
{
	private readonly StrategyParam<int> _oraclePeriod;
	private readonly StrategyParam<int> _smooth;
	private readonly StrategyParam<AlgorithmMode> _mode;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;

	private decimal _prevSignal;
	private decimal _prevPrevSignal;
	private decimal _prevOracle;

	/// <summary>
	/// Oracle calculation period.
	/// </summary>
	public int OraclePeriod
	{
		get => _oraclePeriod.Value;
		set => _oraclePeriod.Value = value;
	}

	/// <summary>
	/// Smoothing length for signal line.
	/// </summary>
	public int Smooth
	{
		get => _smooth.Value;
		set => _smooth.Value = value;
	}

	/// <summary>
	/// Selected trading algorithm.
	/// </summary>
	public AlgorithmMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Allow long positions.
	/// </summary>
	public bool AllowBuy
	{
		get => _allowBuy.Value;
		set => _allowBuy.Value = value;
	}

	/// <summary>
	/// Allow short positions.
	/// </summary>
	public bool AllowSell
	{
		get => _allowSell.Value;
		set => _allowSell.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ExpOracleStrategy"/>.
	/// </summary>
	public ExpOracleStrategy()
	{
		_oraclePeriod = Param(nameof(OraclePeriod), 55)
			.SetGreaterThanZero()
			.SetDisplay("Oracle Period", "Oracle period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 120, 5);

		_smooth = Param(nameof(Smooth), 8)
			.SetGreaterThanZero()
			.SetDisplay("Smooth", "Smoothing length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 2);

		_mode = Param(nameof(Mode), AlgorithmMode.Twist)
			.SetDisplay("Mode", "Signal algorithm", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "Parameters");

		_allowBuy = Param(nameof(AllowBuy), true)
			.SetDisplay("Allow Buy", "Enable long entries", "Parameters");

		_allowSell = Param(nameof(AllowSell), true)
			.SetDisplay("Allow Sell", "Enable short entries", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var oracle = new OracleIndicator
		{
			Length = OraclePeriod,
			Smooth = Smooth
		};

		SubscribeCandles(CandleType)
			.BindEx(oracle, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var value = (OracleIndicatorValue)indicatorValue;
		var signal = value.Signal;
		var oracle = value.Oracle;

		switch (Mode)
		{
			case AlgorithmMode.Breakdown:
				if (AllowBuy && _prevSignal <= 0m && signal > 0m)
				{
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
				}
				else if (AllowSell && _prevSignal >= 0m && signal < 0m)
				{
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
				}
				break;

			case AlgorithmMode.Twist:
				if (AllowBuy && _prevPrevSignal < _prevSignal && signal >= _prevSignal)
				{
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
				}
				else if (AllowSell && _prevPrevSignal > _prevSignal && signal <= _prevSignal)
				{
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
				}
				break;

			case AlgorithmMode.Disposition:
				if (AllowBuy && _prevSignal < _prevOracle && signal >= oracle)
				{
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
				}
				else if (AllowSell && _prevSignal > _prevOracle && signal <= oracle)
				{
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
				}
				break;
		}

		_prevPrevSignal = _prevSignal;
		_prevSignal = signal;
		_prevOracle = oracle;
	}
}

/// <summary>
/// Trading algorithm modes.
/// </summary>
public enum AlgorithmMode
{
	/// <summary>
	/// Signal line crossing zero.
	/// </summary>
	Breakdown,

	/// <summary>
	/// Change of signal line direction.
	/// </summary>
	Twist,

	/// <summary>
	/// Signal line crossing main line.
	/// </summary>
	Disposition
}

/// <summary>
/// Oracle indicator combining RSI and CCI with smoothing.
/// </summary>
public class OracleIndicator : Indicator<decimal>
{
	public int Length { get; set; } = 55;
	public int Smooth { get; set; } = 8;

	private readonly RelativeStrengthIndex _rsi = new();
	private readonly CommodityChannelIndex _cci = new();
	private readonly SimpleMovingAverage _sma = new();

	private readonly decimal[] _rsiBuf = new decimal[4];
	private readonly decimal[] _cciBuf = new decimal[4];

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		_rsi.Length = Length;
		_cci.Length = Length;
		_sma.Length = Smooth;

		var rsiVal = _rsi.Process(input).GetValue<decimal>();
		var cciVal = _cci.Process(input).GetValue<decimal>();

		_rsiBuf[3] = _rsiBuf[2];
		_rsiBuf[2] = _rsiBuf[1];
		_rsiBuf[1] = _rsiBuf[0];
		_rsiBuf[0] = rsiVal;

		_cciBuf[3] = _cciBuf[2];
		_cciBuf[2] = _cciBuf[1];
		_cciBuf[1] = _cciBuf[0];
		_cciBuf[0] = cciVal;

		var div0 = _cciBuf[0] - _rsiBuf[0];
		var dDiv = div0;
		var div1 = _cciBuf[1] - _rsiBuf[1] - dDiv;
		dDiv += div1;
		var div2 = _cciBuf[2] - _rsiBuf[2] - dDiv;
		dDiv += div2;
		var div3 = _cciBuf[3] - _rsiBuf[3] - dDiv;

		var max = Math.Max(Math.Max(div0, div1), Math.Max(div2, div3));
		var min = Math.Min(Math.Min(div0, div1), Math.Min(div2, div3));
		var oracle = max + min;

		var signal = _sma.Process(new DecimalIndicatorValue(this, oracle, input.Time)).GetValue<decimal>();

		return new OracleIndicatorValue(this, input, oracle, signal);
	}

	public override void Reset()
	{
		base.Reset();
		_rsi.Reset();
		_cci.Reset();
		_sma.Reset();
		Array.Clear(_rsiBuf, 0, _rsiBuf.Length);
		Array.Clear(_cciBuf, 0, _cciBuf.Length);
	}
}

/// <summary>
/// Indicator value for <see cref="OracleIndicator"/>.
/// </summary>
public class OracleIndicatorValue : ComplexIndicatorValue
{
	public OracleIndicatorValue(IIndicator indicator, IIndicatorValue input, decimal oracle, decimal signal)
		: base(indicator, input, (nameof(Oracle), oracle), (nameof(Signal), signal))
	{
	}

	/// <summary>
	/// Oracle line value.
	/// </summary>
	public decimal Oracle => (decimal)GetValue(nameof(Oracle));

	/// <summary>
	/// Smoothed signal line value.
	/// </summary>
	public decimal Signal => (decimal)GetValue(nameof(Signal));
}
