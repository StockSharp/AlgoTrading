using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Market Slayer strategy.
/// The SSL channel on TrendCandleType is built from WMAs of highs and lows over ConfirmationTrendValue candles: a close above the
/// high WMA makes the trend bullish, a close below the low WMA makes it bearish, otherwise the previous state is kept. On CandleType
/// the short WMA crossing above the long WMA in a bullish trend goes long and crossing below in a bearish trend goes short. A position
/// closes when the trend turns opposite. Optional take profit and stop loss are distances in price steps.
/// </summary>
public class MarketSlayerStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _confirmationTrendValue;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<bool> _takeProfitEnabled;
	private readonly StrategyParam<decimal> _takeProfitValue;
	private readonly StrategyParam<bool> _stopLossEnabled;
	private readonly StrategyParam<decimal> _stopLossValue;

	private WeightedMovingAverage _trendHigh;
	private WeightedMovingAverage _trendLow;
	private int _trendHlv;
	private decimal? _prevShort;
	private decimal? _prevLong;

	/// <summary>
	/// Short WMA length.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Long WMA length.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// SSL WMA length on the trend timeframe.
	/// </summary>
	public int ConfirmationTrendValue
	{
		get => _confirmationTrendValue.Value;
		set => _confirmationTrendValue.Value = value;
	}

	/// <summary>
	/// Signal candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trend candle type.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Enable take profit.
	/// </summary>
	public bool TakeProfitEnabled
	{
		get => _takeProfitEnabled.Value;
		set => _takeProfitEnabled.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public decimal TakeProfitValue
	{
		get => _takeProfitValue.Value;
		set => _takeProfitValue.Value = value;
	}

	/// <summary>
	/// Enable stop loss.
	/// </summary>
	public bool StopLossEnabled
	{
		get => _stopLossEnabled.Value;
		set => _stopLossEnabled.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public decimal StopLossValue
	{
		get => _stopLossValue.Value;
		set => _stopLossValue.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MarketSlayerStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Short Length", "Short WMA length", "Indicators");

		_longLength = Param(nameof(LongLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Long Length", "Long WMA length", "Indicators");

		_confirmationTrendValue = Param(nameof(ConfirmationTrendValue), 2)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "SSL WMA length on the trend timeframe", "Trend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Signal candle type", "General");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromMinutes(240).TimeFrame())
			.SetDisplay("Trend Candle Type", "Higher timeframe for the SSL trend", "Trend");

		_takeProfitEnabled = Param(nameof(TakeProfitEnabled), false)
			.SetDisplay("Use Take Profit", "Enable take profit", "Risk");

		_takeProfitValue = Param(nameof(TakeProfitValue), 20m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit distance in price steps", "Risk");

		_stopLossEnabled = Param(nameof(StopLossEnabled), false)
			.SetDisplay("Use Stop Loss", "Enable stop loss", "Risk");

		_stopLossValue = Param(nameof(StopLossValue), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TrendCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_trendHigh = null;
		_trendLow = null;
		_trendHlv = 0;
		_prevShort = null;
		_prevLong = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_trendHlv = 0;
		_prevShort = null;
		_prevLong = null;

		var shortWma = new WeightedMovingAverage { Length = ShortLength };
		var longWma = new WeightedMovingAverage { Length = LongLength };
		_trendHigh = new WeightedMovingAverage { Length = ConfirmationTrendValue };
		_trendLow = new WeightedMovingAverage { Length = ConfirmationTrendValue };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortWma, longWma, ProcessCandle)
			.Start();

		SubscribeCandles(TrendCandleType)
			.Bind(ProcessTrendCandle)
			.Start();

		if (TakeProfitEnabled || StopLossEnabled)
		{
			var step = Security?.PriceStep ?? 1m;
			StartProtection(
				TakeProfitEnabled ? new Unit(TakeProfitValue * step, UnitTypes.Absolute) : new Unit(),
				StopLossEnabled ? new Unit(StopLossValue * step, UnitTypes.Absolute) : new Unit(),
				useMarketOrders: true);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortWma);
			DrawIndicator(area, longWma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessTrendCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highValue = _trendHigh.Process(new DecimalIndicatorValue(_trendHigh, candle.HighPrice, candle.OpenTime) { IsFinal = true });
		var lowValue = _trendLow.Process(new DecimalIndicatorValue(_trendLow, candle.LowPrice, candle.OpenTime) { IsFinal = true });

		if (!_trendHigh.IsFormed || !_trendLow.IsFormed)
			return;

		var high = highValue.GetValue<decimal>();
		var low = lowValue.GetValue<decimal>();

		if (candle.ClosePrice > high)
			_trendHlv = 1;
		else if (candle.ClosePrice < low)
			_trendHlv = -1;
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortValue, decimal longValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevShort = _prevShort;
		var prevLong = _prevLong;
		_prevShort = shortValue;
		_prevLong = longValue;

		if (prevShort is not decimal ps || prevLong is not decimal pl)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bullish = _trendHlv > 0;
		var bearish = _trendHlv < 0;

		if (Position > 0 && bearish)
		{
			SellMarket(Position);
			return;
		}

		if (Position < 0 && bullish)
		{
			BuyMarket(-Position);
			return;
		}

		if (ps <= pl && shortValue > longValue && bullish && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (ps >= pl && shortValue < longValue && bearish && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
