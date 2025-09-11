using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with ATR trend filter and dynamic exits.
/// </summary>
public class QuantumSentimentFluxBeginnersStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _maStrengthThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _quantity;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private AverageTrueRange _atr;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private int _cooldownCounter;

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop calculation.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum EMA difference in ATR units to confirm trend.
	/// </summary>
	public decimal MaStrengthThreshold
	{
		get => _maStrengthThreshold.Value;
		set => _maStrengthThreshold.Value = value;
	}

	/// <summary>
	/// Number of bars to wait after a trade.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Contracts per trade.
	/// </summary>
	public decimal Quantity
	{
		get => _quantity.Value;
		set => _quantity.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="QuantumSentimentFluxBeginnersStrategy"/>.
	/// </summary>
	public QuantumSentimentFluxBeginnersStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for analysis", "General");

		_fastLength = Param(nameof(FastLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Length of the fast EMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_slowLength = Param(nameof(SlowLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Length of the slow EMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_atrLength = Param(nameof(AtrLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "Period for ATR", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop distance", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.1m);

		_maStrengthThreshold = Param(nameof(MaStrengthThreshold), 0.18m)
			.SetGreaterThanZero()
			.SetDisplay("MA Strength", "ATR weighted EMA difference", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.3m, 0.01m);

		_cooldownBars = Param(nameof(CooldownBars), 1)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Bars to wait after trade", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 5, 1);

		_quantity = Param(nameof(Quantity), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Quantity", "Contracts per trade", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(StockSharp.BusinessEntities.Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = null;
		_prevSlow = null;
		_cooldownCounter = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new() { Length = FastLength };
		_slowEma = new() { Length = SlowLength };
		_atr = new() { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, _atr, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownCounter > 0)
			_cooldownCounter--;

		var strength = atr * MaStrengthThreshold;
		var trendDir = fast > slow + strength ? 1 : fast < slow - strength ? -1 : 0;

		var buySignal = _prevFast is decimal pf && _prevSlow is decimal ps && pf <= ps && fast > slow && trendDir == 1;
		var sellSignal = _prevFast is decimal pf2 && _prevSlow is decimal ps2 && pf2 >= ps2 && fast < slow && trendDir == -1;

		_prevFast = fast;
		_prevSlow = slow;

		if (Position == 0 && _cooldownCounter == 0)
		{
			if (buySignal)
			{
				BuyMarket(Quantity);
				_cooldownCounter = CooldownBars;
				return;
			}

			if (sellSignal)
			{
				SellMarket(Quantity);
				_cooldownCounter = CooldownBars;
				return;
			}
		}

		if (Position > 0)
		{
			var stopPrice = candle.ClosePrice - atr * AtrMultiplier;
			var takePrice = candle.ClosePrice + atr * AtrMultiplier * 2m;

			if (candle.LowPrice <= stopPrice || candle.HighPrice >= takePrice)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			var stopPrice = candle.ClosePrice + atr * AtrMultiplier;
			var takePrice = candle.ClosePrice - atr * AtrMultiplier * 2m;

			if (candle.HighPrice >= stopPrice || candle.LowPrice <= takePrice)
				BuyMarket(-Position);
		}
	}
}

