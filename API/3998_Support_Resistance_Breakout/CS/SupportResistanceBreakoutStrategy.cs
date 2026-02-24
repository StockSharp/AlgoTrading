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
/// Support and resistance breakout strategy with EMA trend filter.
/// Buys above resistance during bullish trends and sells below support during bearish trends.
/// Manually tracks highest high and lowest low over N candles for support/resistance.
/// </summary>
public class SupportResistanceBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal _support;
	private decimal _resistance;
	private decimal? _entryPrice;

	/// <summary>
	/// Number of candles used to compute support and resistance.
	/// </summary>
	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	/// <summary>
	/// EMA length used as the trend filter.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss in absolute points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit in absolute points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SupportResistanceBreakoutStrategy()
	{
		_rangeLength = Param(nameof(RangeLength), 55)
			.SetGreaterThanZero()
			.SetDisplay("Range Length", "Candles used to form support/resistance", "General")
			.SetOptimize(20, 100, 5);

		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Length of the EMA trend filter", "General")
			.SetOptimize(20, 200, 10);

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss in absolute points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit in absolute points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");

		Volume = 1;
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
		_ema = null;
		_highs.Clear();
		_lows.Clear();
		_support = 0m;
		_resistance = 0m;
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		_ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}

		var tp = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : null;
		var sl = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Absolute) : null;
		if (tp != null || sl != null)
			StartProtection(tp, sl);

		base.OnStarted2(time);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (Position != 0 && _entryPrice == null)
			_entryPrice = trade.Trade.Price;
		if (Position == 0)
			_entryPrice = null;
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track highs and lows manually
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		if (_highs.Count > RangeLength)
			_highs.RemoveAt(0);
		if (_lows.Count > RangeLength)
			_lows.RemoveAt(0);

		if (_highs.Count < RangeLength)
			return;

		// Compute support/resistance from previous bars (exclude current)
		var prevResistance = _resistance;
		var prevSupport = _support;

		decimal maxHigh = decimal.MinValue;
		decimal minLow = decimal.MaxValue;
		// Use bars 0..N-2 (exclude last which is current)
		for (int i = 0; i < _highs.Count - 1; i++)
		{
			if (_highs[i] > maxHigh) maxHigh = _highs[i];
			if (_lows[i] < minLow) minLow = _lows[i];
		}

		_resistance = maxHigh;
		_support = minLow;

		// Determine trend from EMA
		var isBullish = candle.ClosePrice > emaValue;
		var isBearish = candle.ClosePrice < emaValue;

		// Exit: close longs if price falls back below support while in profit
		if (Position > 0 && _entryPrice is decimal entryLong)
		{
			if (candle.ClosePrice - entryLong > 0 && candle.ClosePrice < _support)
			{
				SellMarket(Position);
				return;
			}
		}
		else if (Position < 0 && _entryPrice is decimal entryShort)
		{
			if (entryShort - candle.ClosePrice > 0 && candle.ClosePrice > _resistance)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Entry: breakout above resistance in bullish trend
		if (isBullish && Position <= 0 && candle.ClosePrice > _resistance && _resistance > 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
		}
		// Entry: breakdown below support in bearish trend
		else if (isBearish && Position >= 0 && candle.ClosePrice < _support && _support > 0)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket(Volume);
		}
	}
}
