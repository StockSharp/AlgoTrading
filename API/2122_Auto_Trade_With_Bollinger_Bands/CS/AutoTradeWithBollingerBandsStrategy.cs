using System;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands auto trade strategy with RSI and Stochastic confirmation.
/// </summary>
public class AutoTradeWithBollingerBandsStrategy : Strategy
{
	private readonly StrategyParam<bool> _openBuy;
	private readonly StrategyParam<bool> _openSell;
	private readonly StrategyParam<int> _gmtTradeStart;
	private readonly StrategyParam<int> _gmtTradeStop;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<int> _stochSlowing;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool OpenBuy
	{
		get => _openBuy.Value;
		set => _openBuy.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool OpenSell
	{
		get => _openSell.Value;
		set => _openSell.Value = value;
	}

	/// <summary>
	/// Start hour (GMT) for trading.
	/// </summary>
	public int GmtTradeStart
	{
		get => _gmtTradeStart.Value;
		set => _gmtTradeStart.Value = value;
	}

	/// <summary>
	/// Stop hour (GMT) for trading.
	/// </summary>
	public int GmtTradeStop
	{
		get => _gmtTradeStop.Value;
		set => _gmtTradeStop.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BbPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic slowing value.
	/// </summary>
	public int StochSlowing
	{
		get => _stochSlowing.Value;
		set => _stochSlowing.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AutoTradeWithBollingerBandsStrategy"/>.
	/// </summary>
	public AutoTradeWithBollingerBandsStrategy()
	{
		_openBuy = Param(nameof(OpenBuy), true)
			.SetDisplay("Open Buy", "Allow opening long positions", "General");

		_openSell = Param(nameof(OpenSell), true)
			.SetDisplay("Open Sell", "Allow opening short positions", "General");

		_gmtTradeStart = Param(nameof(GmtTradeStart), 12)
			.SetDisplay("Trade Start Hour", "Start hour in GMT", "Time");

		_gmtTradeStop = Param(nameof(GmtTradeStop), 19)
			.SetDisplay("Trade Stop Hour", "Stop hour in GMT", "Time");

		_bbPeriod = Param(nameof(BbPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators");

		_stochKPeriod = Param(nameof(StochKPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stoch K Period", "Stochastic %K length", "Indicators");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stoch D Period", "Stochastic %D length", "Indicators");

		_stochSlowing = Param(nameof(StochSlowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Slowing", "Stochastic smoothing", "Indicators");

		_trailingStop = Param(nameof(TrailingStop), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BbPeriod,
			Width = 4m
		};

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var stochastic = new StochasticOscillator
		{
			K = { Length = StochKPeriod },
			D = { Length = StochDPeriod },
			Slowing = StochSlowing
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bollinger, rsi, stochastic, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(TrailingStop, UnitTypes.Point),
			isStopTrailing: true);
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal rsiValue, decimal kValue, decimal _)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.OpenTime.Hour;

		if (OpenSell && hour > GmtTradeStart && hour < GmtTradeStop && candle.ClosePrice > upper && rsiValue > 75m && kValue > 85m && Position >= 0)
		{
			SellMarket();
			return;
		}

		if (OpenBuy && hour > GmtTradeStart && hour < GmtTradeStop && candle.ClosePrice < lower && rsiValue < 25m && kValue < 155m && Position <= 0)
		{
			BuyMarket();
		}
	}
}

