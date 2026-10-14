using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ETH signal 15m strategy.
/// A SuperTrend flip to an uptrend goes long when RSI is below RsiOverbought, a flip to a downtrend goes short when RSI is above
/// RsiOversold, reversing an opposite position. The stop lies 4 ATR from the entry, the take profit 2 ATR for longs and 2.237 ATR
/// for shorts.
/// </summary>
public class EthSignal15mStrategy : Strategy
{
	private const decimal _stopAtr = 4m;
	private const decimal _longTakeAtr = 2m;
	private const decimal _shortTakeAtr = 2.237m;

	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _prevUpTrend;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// ATR period of SuperTrend and the exits.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// SuperTrend factor.
	/// </summary>
	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI level below which longs are allowed.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI level above which shorts are allowed.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public EthSignal15mStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period of SuperTrend and the exits", "SuperTrend");

		_factor = Param(nameof(Factor), 2.76m)
			.SetGreaterThanZero()
			.SetDisplay("Factor", "SuperTrend factor", "SuperTrend");

		_rsiLength = Param(nameof(RsiLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI level below which longs are allowed", "RSI");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI level above which shorts are allowed", "RSI");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevUpTrend = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevUpTrend = null;

		var superTrend = new SuperTrend { Length = AtrPeriod, Multiplier = Factor };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(superTrend, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, superTrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue superTrendValue, IIndicatorValue rsiValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!superTrendValue.IsFormed || !rsiValue.IsFormed || !atrValue.IsFormed || superTrendValue is not SuperTrendIndicatorValue trend)
			return;

		var prevUpTrend = _prevUpTrend;
		_prevUpTrend = trend.IsUpTrend;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0 && (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice))
		{
			SellMarket(Position);
			return;
		}

		if (Position < 0 && (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice))
		{
			BuyMarket(-Position);
			return;
		}

		if (prevUpTrend is not bool wasUp)
			return;

		var rsi = rsiValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (!wasUp && trend.IsUpTrend && rsi < RsiOverbought && Position <= 0)
		{
			_stopPrice = close - atr * _stopAtr;
			_takePrice = close + atr * _longTakeAtr;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (wasUp && !trend.IsUpTrend && rsi > RsiOversold && Position >= 0)
		{
			_stopPrice = close + atr * _stopAtr;
			_takePrice = close - atr * _shortTakeAtr;
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
