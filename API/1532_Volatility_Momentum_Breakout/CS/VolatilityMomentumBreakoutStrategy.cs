using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility breakout strategy with momentum filter and ATR based risk management.
/// </summary>
public class VolatilityMomentumBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLongThreshold;
	private readonly StrategyParam<decimal> _rsiShortThreshold;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<decimal> _atrStopMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private decimal _prevHighest;
	private decimal _prevLowest;
	private decimal _entryPrice;
	private decimal _entryAtr;

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for breakout levels.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Lookback period for breakout levels.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// EMA period used as trend filter.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI threshold for long trades.
	/// </summary>
	public decimal RsiLongThreshold
	{
		get => _rsiLongThreshold.Value;
		set => _rsiLongThreshold.Value = value;
	}

	/// <summary>
	/// RSI threshold for short trades.
	/// </summary>
	public decimal RsiShortThreshold
	{
		get => _rsiShortThreshold.Value;
		set => _rsiShortThreshold.Value = value;
	}

	/// <summary>
	/// Risk-reward ratio for targets.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal AtrStopMultiplier
	{
		get => _atrStopMultiplier.Value;
		set => _atrStopMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VolatilityMomentumBreakoutStrategy"/> class.
	/// </summary>
	public VolatilityMomentumBreakoutStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation period", "General")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for breakout", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_lookback = Param(nameof(Lookback), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Breakout lookback period", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 10);

		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for trend filter", "General")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "General")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_rsiLongThreshold = Param(nameof(RsiLongThreshold), 50m)
			.SetDisplay("RSI Long Threshold", "RSI threshold for long trades", "General")
			.SetCanOptimize(true)
			.SetOptimize(40m, 60m, 5m);

		_rsiShortThreshold = Param(nameof(RsiShortThreshold), 50m)
			.SetDisplay("RSI Short Threshold", "RSI threshold for short trades", "General")
			.SetCanOptimize(true)
			.SetOptimize(40m, 60m, 5m);

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Risk-reward ratio", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_atrStopMultiplier = Param(nameof(AtrStopMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Stop Mult", "ATR multiplier for stop", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHighest = 0m;
		_prevLowest = 0m;
		_entryPrice = 0m;
		_entryAtr = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_highest = new Highest { Length = Lookback };
		_lowest = new Lowest { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ema, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal emaValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var currentHighest = _highest.Process(candle).ToDecimal();
		var currentLowest = _lowest.Process(candle).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHighest = currentHighest;
			_prevLowest = currentLowest;
			return;
		}

		var longBreakoutLevel = _prevHighest + AtrMultiplier * atrValue;
		var shortBreakoutLevel = _prevLowest - AtrMultiplier * atrValue;

		if (Position <= 0 && candle.ClosePrice > longBreakoutLevel && candle.ClosePrice > emaValue && rsiValue > RsiLongThreshold)
		{
			_entryPrice = candle.ClosePrice;
			_entryAtr = atrValue;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position >= 0 && candle.ClosePrice < shortBreakoutLevel && candle.ClosePrice < emaValue && rsiValue < RsiShortThreshold)
		{
			_entryPrice = candle.ClosePrice;
			_entryAtr = atrValue;
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0)
		{
			var longStop = _entryPrice - AtrStopMultiplier * _entryAtr;
			var longTarget = _entryPrice + (_entryPrice - longStop) * RiskReward;

			if (candle.LowPrice <= longStop || candle.HighPrice >= longTarget)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			var shortStop = _entryPrice + AtrStopMultiplier * _entryAtr;
			var shortTarget = _entryPrice - (shortStop - _entryPrice) * RiskReward;

			if (candle.HighPrice >= shortStop || candle.LowPrice <= shortTarget)
				BuyMarket(Math.Abs(Position));
		}

		if (Position == 0)
		{
			_entryPrice = 0m;
			_entryAtr = 0m;
		}

		_prevHighest = currentHighest;
		_prevLowest = currentLowest;
	}
}
