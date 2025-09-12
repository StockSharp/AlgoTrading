using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Scalping strategy using VWAP and RSI with session and daily trade limits.
/// </summary>
public class VwapRsiScalperFinalV1Strategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopAtrMult;
	private readonly StrategyParam<decimal> _targetAtrMult;
	private readonly StrategyParam<DataType> _candleType;

	private int _tradesToday;
	private DateTime _currentDay;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public TimeSpan SessionStart { get => _sessionStart.Value; set => _sessionStart.Value = value; }
	public TimeSpan SessionEnd { get => _sessionEnd.Value; set => _sessionEnd.Value = value; }
	public int MaxTradesPerDay { get => _maxTradesPerDay.Value; set => _maxTradesPerDay.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal StopAtrMult { get => _stopAtrMult.Value; set => _stopAtrMult.Value = value; }
	public decimal TargetAtrMult { get => _targetAtrMult.Value; set => _targetAtrMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VwapRsiScalperFinalV1Strategy()
	{
		_rsiLength = Param(nameof(RsiLength), 3)
			.SetDisplay("RSI Length", "RSI period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 6, 1);

		_rsiOversold = Param(nameof(RsiOversold), 35m)
			.SetDisplay("RSI Oversold", "Oversold level", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(25m, 45m, 5m);

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Overbought level", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_emaLength = Param(nameof(EmaLength), 50)
			.SetDisplay("EMA Length", "EMA period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 80, 5);

		_sessionStart = Param(nameof(SessionStart), TimeSpan.FromHours(9))
			.SetDisplay("Session Start", "Session start hour", "Session");

		_sessionEnd = Param(nameof(SessionEnd), TimeSpan.FromHours(16))
			.SetDisplay("Session End", "Session end hour", "Session");

		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 3)
			.SetDisplay("Max Trades Per Day", "Daily trade limit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 7);

		_stopAtrMult = Param(nameof(StopAtrMult), 1m)
			.SetDisplay("Stop ATR Mult", "ATR multiplier for stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.5m);

		_targetAtrMult = Param(nameof(TargetAtrMult), 2m)
			.SetDisplay("Target ATR Mult", "ATR multiplier for target", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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
		_tradesToday = 0;
		_currentDay = default;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var vwap = new VolumeWeightedMovingAverage();
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(vwap, ema, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwap);
			DrawIndicator(area, ema);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwapValue, decimal emaValue, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

	var day = candle.OpenTime.Date;
		if (day != _currentDay)
		{
			_currentDay = day;
			_tradesToday = 0;
		}

		var time = candle.OpenTime.TimeOfDay;
		var inSession = time >= SessionStart && time < SessionEnd;

	if (!IsFormedAndOnlineAndAllowTrading())
			return;

	if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
				SellMarket(Position);
		}
	else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
				BuyMarket(-Position);
		}
	else if (inSession && _tradesToday < MaxTradesPerDay)
		{
			var canLong = rsiValue < RsiOversold && candle.ClosePrice > vwapValue && candle.ClosePrice > emaValue;
			var canShort = rsiValue > RsiOverbought && candle.ClosePrice < vwapValue && candle.ClosePrice < emaValue;

			if (canLong)
			{
			BuyMarket();
			_tradesToday++;
			_stopPrice = candle.ClosePrice - atrValue * StopAtrMult;
			_takeProfitPrice = candle.ClosePrice + atrValue * TargetAtrMult;
			}
			else if (canShort)
			{
			SellMarket();
			_tradesToday++;
			_stopPrice = candle.ClosePrice + atrValue * StopAtrMult;
			_takeProfitPrice = candle.ClosePrice - atrValue * TargetAtrMult;
			}
		}
	}
}
