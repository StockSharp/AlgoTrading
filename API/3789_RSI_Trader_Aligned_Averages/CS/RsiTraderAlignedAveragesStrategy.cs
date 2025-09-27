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
/// RSI Trader strategy combining price and RSI trend filters.
/// The strategy compares short and long moving averages of both price and RSI to detect trend alignment.
/// </summary>
public class RsiTraderAlignedAveragesStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _shortPriceMaPeriod;
	private readonly StrategyParam<int> _longPriceMaPeriod;
	private readonly StrategyParam<int> _shortRsiMaPeriod;
	private readonly StrategyParam<int> _longRsiMaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _shortRsiMa;
	private SimpleMovingAverage _longRsiMa;
	private SimpleMovingAverage _shortPriceMa;
	private LinearWeightedMovingAverage _longPriceMa;

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Period for the short simple moving average of price.
	/// </summary>
	public int ShortPriceMaPeriod
	{
		get => _shortPriceMaPeriod.Value;
		set => _shortPriceMaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the long linear weighted moving average of price.
	/// </summary>
	public int LongPriceMaPeriod
	{
		get => _longPriceMaPeriod.Value;
		set => _longPriceMaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the short smoothing moving average applied to RSI.
	/// </summary>
	public int ShortRsiMaPeriod
	{
		get => _shortRsiMaPeriod.Value;
		set => _shortRsiMaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the long smoothing moving average applied to RSI.
	/// </summary>
	public int LongRsiMaPeriod
	{
		get => _longRsiMaPeriod.Value;
		set => _longRsiMaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for generating signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
/// Initializes strategy parameters.
/// </summary>
public RsiTraderAlignedAveragesStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation period", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 1);

		_shortPriceMaPeriod = Param(nameof(ShortPriceMaPeriod), 9)
			.SetDisplay("Short Price MA", "Short period for price moving average", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_longPriceMaPeriod = Param(nameof(LongPriceMaPeriod), 45)
			.SetDisplay("Long Price MA", "Long period for price moving average", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(30, 90, 5);

		_shortRsiMaPeriod = Param(nameof(ShortRsiMaPeriod), 9)
			.SetDisplay("Short RSI MA", "Short smoothing period for RSI", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_longRsiMaPeriod = Param(nameof(LongRsiMaPeriod), 45)
			.SetDisplay("Long RSI MA", "Long smoothing period for RSI", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(30, 90, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for analysis", "General");
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

		_rsi = null;
		_shortRsiMa = null;
		_longRsiMa = null;
		_shortPriceMa = null;
		_longPriceMa = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators for price and RSI smoothing.
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_shortRsiMa = new SimpleMovingAverage { Length = ShortRsiMaPeriod };
		_longRsiMa = new SimpleMovingAverage { Length = LongRsiMaPeriod };
		_shortPriceMa = new SimpleMovingAverage { Length = ShortPriceMaPeriod };
		_longPriceMa = new LinearWeightedMovingAverage { Length = LongPriceMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_rsi, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, _shortPriceMa);
			DrawIndicator(priceArea, _longPriceMa);
			DrawOwnTrades(priceArea);

			var rsiArea = CreateChartArea();
			if (rsiArea != null)
			{
				DrawIndicator(rsiArea, _rsi);
				DrawIndicator(rsiArea, _shortRsiMa);
				DrawIndicator(rsiArea, _longRsiMa);
			}
		}

	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!rsiValue.IsFinal)
			return;

		var rsi = rsiValue.ToDecimal();
		var shortRsi = _shortRsiMa.Process(rsi, candle.OpenTime, true).ToDecimal();
		var longRsi = _longRsiMa.Process(rsi, candle.OpenTime, true).ToDecimal();
		var shortPrice = _shortPriceMa.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();
		var longPrice = _longPriceMa.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();

		if (!_shortRsiMa.IsFormed || !_longRsiMa.IsFormed || !_shortPriceMa.IsFormed || !_longPriceMa.IsFormed)
			return;

		var isLong = shortPrice > longPrice && shortRsi > longRsi;
		var isShort = shortPrice < longPrice && shortRsi < longRsi;
		var isSideways = (shortPrice > longPrice && shortRsi < longRsi) || (shortPrice < longPrice && shortRsi > longRsi);

		if (isLong)
		{
			if (Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (Position == 0)
				BuyMarket(Volume);
		}
		else if (isShort)
		{
			if (Position > 0)
			{
				SellMarket(Position);
				return;
			}

			if (Position == 0)
				SellMarket(Volume);
		}
		else if (isSideways && Position != 0)
		{
			if (Position > 0)
			{
				SellMarket(Position);
			}
			else
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}

