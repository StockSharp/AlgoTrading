using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Scalping strategy using RSI and EMA with StdDev-based stops.
/// Buys on RSI oversold with bullish EMA trend, sells on RSI overbought with bearish EMA.
/// </summary>
public class VwapRsiScalperFinalV1Strategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<decimal> _stopMult;
	private readonly StrategyParam<decimal> _targetMult;
	private readonly StrategyParam<DataType> _candleType;

	private int _tradesToday;
	private DateTime _currentDay;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int MaxTradesPerDay { get => _maxTradesPerDay.Value; set => _maxTradesPerDay.Value = value; }
	public decimal StopMult { get => _stopMult.Value; set => _stopMult.Value = value; }
	public decimal TargetMult { get => _targetMult.Value; set => _targetMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VwapRsiScalperFinalV1Strategy()
	{
		_rsiLength = Param(nameof(RsiLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 35m)
			.SetDisplay("RSI Oversold", "Oversold level", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Overbought level", "Indicators");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Indicators");

		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Max trades per day", "Risk");

		_stopMult = Param(nameof(StopMult), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Mult", "StdDev multiplier for stop", "Risk");

		_targetMult = Param(nameof(TargetMult), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Target Mult", "StdDev multiplier for target", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_tradesToday = 0;
		_currentDay = default;
		_stopPrice = 0;
		_takeProfitPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var stdDev = new StandardDeviation { Length = 14 };

		_tradesToday = 0;
		_currentDay = default;
		_stopPrice = 0;
		_takeProfitPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ema, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal emaVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;
		if (day != _currentDay)
		{
			_currentDay = day;
			_tradesToday = 0;
		}

		// TP/SL exit
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
			{
				SellMarket();
				return;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
			{
				BuyMarket();
				return;
			}
		}

		if (stdVal <= 0 || _tradesToday >= MaxTradesPerDay)
			return;

		// Entry signals
		if (Position == 0)
		{
			var canLong = rsiVal < RsiOversold && candle.ClosePrice > emaVal;
			var canShort = rsiVal > RsiOverbought && candle.ClosePrice < emaVal;

			if (canLong)
			{
				BuyMarket();
				_tradesToday++;
				_stopPrice = candle.ClosePrice - stdVal * StopMult;
				_takeProfitPrice = candle.ClosePrice + stdVal * TargetMult;
			}
			else if (canShort)
			{
				SellMarket();
				_tradesToday++;
				_stopPrice = candle.ClosePrice + stdVal * StopMult;
				_takeProfitPrice = candle.ClosePrice - stdVal * TargetMult;
			}
		}
	}
}
