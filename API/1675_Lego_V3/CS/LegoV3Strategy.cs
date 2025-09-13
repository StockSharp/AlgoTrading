using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Lego V3 strategy translated from MQL4.
/// Combines moving averages, Stochastic oscillator, Awesome Oscillator,
/// and ATR-based stop management.
/// </summary>
public class LegoV3Strategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<int> _stochD;
	private readonly StrategyParam<decimal> _stochBuy;
	private readonly StrategyParam<decimal> _stochSell;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private bool _protectionStarted;

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochK
	{
		get => _stochK.Value;
		set => _stochK.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int StochD
	{
		get => _stochD.Value;
		set => _stochD.Value = value;
	}

	/// <summary>
	/// Stochastic buy level.
	/// </summary>
	public decimal StochBuy
	{
		get => _stochBuy.Value;
		set => _stochBuy.Value = value;
	}

	/// <summary>
	/// Stochastic sell level.
	/// </summary>
	public decimal StochSell
	{
		get => _stochSell.Value;
		set => _stochSell.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for ATR based stops.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	/// Initializes a new instance of <see cref="LegoV3Strategy"/>.
	/// </summary>
	public LegoV3Strategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast moving average period", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 28)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow moving average period", "Indicators");

		_stochK = Param(nameof(StochK), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Stochastic oscillator %K period", "Indicators");

		_stochD = Param(nameof(StochD), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "Stochastic oscillator %D period", "Indicators");

		_stochBuy = Param(nameof(StochBuy), 20m)
			.SetDisplay("Stochastic Buy Level", "Level below which longs are considered", "Indicators");

		_stochSell = Param(nameof(StochSell), 80m)
			.SetDisplay("Stochastic Sell Level", "Level above which shorts are considered", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for stop calculation", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for ATR based stops", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
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

		var fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
		var slowMa = new SimpleMovingAverage { Length = SlowMaPeriod };
		var stoch = new StochasticOscillator
		{
			K = { Length = StochK },
			D = { Length = StochD },
		};
		var ao = new AwesomeOscillator();
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastMa, slowMa, stoch, ao, atr, Process)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawIndicator(area, stoch);
			DrawIndicator(area, ao);
		}
	}

	private void Process(ICandleMessage candle,
	IIndicatorValue fastMaValue,
	IIndicatorValue slowMaValue,
	IIndicatorValue stochValue,
	IIndicatorValue aoValue,
	IIndicatorValue atrValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var fastMa = fastMaValue.ToDecimal();
	var slowMa = slowMaValue.ToDecimal();
	var stoch = (StochasticOscillatorValue)stochValue;
	var stochK = stoch.K;
	var ao = aoValue.ToDecimal();
	var atr = atrValue.ToDecimal();

	if (!_protectionStarted && atr > 0)
	{
	StartProtection(
		takeProfit: new Unit(atr * AtrMultiplier, UnitTypes.Absolute),
		stopLoss: new Unit(atr * AtrMultiplier, UnitTypes.Absolute));
	_protectionStarted = true;
	}

	var canBuy = fastMa > slowMa && stochK < StochBuy && ao > 0;
	var canSell = fastMa < slowMa && stochK > StochSell && ao < 0;

	if (canBuy && Position <= 0)
	{
	var volume = Volume + Math.Abs(Position);
	BuyMarket(volume);
	}
	else if (canSell && Position >= 0)
	{
	var volume = Volume + Math.Abs(Position);
	SellMarket(volume);
	}
	else if (Position > 0 && canSell)
	{
	SellMarket(Position);
	}
	else if (Position < 0 && canBuy)
	{
	BuyMarket(-Position);
	}
	}
}
