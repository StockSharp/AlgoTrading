using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Tri-Monthly BTC Swing strategy.
/// Goes long when price is above EMA200, MACD line above signal and RSI above threshold.
/// Allows only one trade per defined interval.
/// </summary>
public class TriMonthlyBtcSwingStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiThreshold;
	private readonly StrategyParam<TimeSpan> _tradeInterval;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private RelativeStrengthIndex _rsi = null!;
	private DateTimeOffset? _lastTradeTime;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI threshold.
	/// </summary>
	public decimal RsiThreshold { get => _rsiThreshold.Value; set => _rsiThreshold.Value = value; }

	/// <summary>
	/// Minimum interval between trades.
	/// </summary>
	public TimeSpan TradeInterval { get => _tradeInterval.Value; set => _tradeInterval.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="TriMonthlyBtcSwingStrategy"/>.
	/// </summary>
	public TriMonthlyBtcSwingStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "General")
			.SetCanOptimize(true);

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast period", "General")
			.SetCanOptimize(true);

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow period", "General")
			.SetCanOptimize(true);

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal period", "General")
			.SetCanOptimize(true);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "General")
			.SetCanOptimize(true);

		_rsiThreshold = Param(nameof(RsiThreshold), 50m)
			.SetDisplay("RSI Threshold", "RSI level", "General")
			.SetCanOptimize(true);

		_tradeInterval = Param(nameof(TradeInterval), TimeSpan.FromDays(90))
			.SetDisplay("Trade Interval", "Minimum time between trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
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

		_ema = null!;
		_macd = null!;
		_rsi = null!;
		_lastTradeTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_macd = new MovingAverageConvergenceDivergenceSignal { Fast = MacdFast, Slow = MacdSlow, Signal = MacdSignal };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, _macd, _rsi, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			var osc = CreateChartArea("Oscillators");
			if (osc != null)
			{
				DrawIndicator(osc, _macd);
				DrawIndicator(osc, _rsi);
			}
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal macd, decimal signal, decimal _, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var now = candle.CloseTime;
		var canTrade = _lastTradeTime == null || now - _lastTradeTime > TradeInterval;

		var longCondition = candle.ClosePrice > ema && macd > signal && rsi > RsiThreshold && canTrade;
		var exitCondition = macd < signal || rsi < RsiThreshold;

		if (longCondition && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
			_lastTradeTime = now;
		}
		else if (exitCondition && Position > 0)
		{
			SellMarket(Position);
		}
	}
}
