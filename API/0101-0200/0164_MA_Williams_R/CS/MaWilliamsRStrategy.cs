using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA Williams %R strategy.
/// A close above the MaPeriod moving average of type MaType with Williams %R below WilliamsROversold goes long and a close below it with
/// %R above WilliamsROverbought goes short, reversing an opposite position. A long closes once %R returns to the -50 middle from below
/// and a short once it returns from above, and a percent stop limits the loss.
/// </summary>
public class MaWilliamsRStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<MovingAverageTypes> _maType;
	private readonly StrategyParam<int> _williamsRPeriod;
	private readonly StrategyParam<decimal> _williamsROversold;
	private readonly StrategyParam<decimal> _williamsROverbought;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period of the moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Type of the moving average.
	/// </summary>
	public MovingAverageTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Period of Williams %R.
	/// </summary>
	public int WilliamsRPeriod
	{
		get => _williamsRPeriod.Value;
		set => _williamsRPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R level for longs.
	/// </summary>
	public decimal WilliamsROversold
	{
		get => _williamsROversold.Value;
		set => _williamsROversold.Value = value;
	}

	/// <summary>
	/// Williams %R level for shorts.
	/// </summary>
	public decimal WilliamsROverbought
	{
		get => _williamsROverbought.Value;
		set => _williamsROverbought.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	public MaWilliamsRStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period of the moving average", "Indicators");

		_maType = Param(nameof(MaType), MovingAverageTypes.Simple)
			.SetDisplay("MA Type", "Type of the moving average", "Indicators");

		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period of Williams %R", "Indicators");

		_williamsROversold = Param(nameof(WilliamsROversold), -80m)
			.SetDisplay("Williams %R Oversold", "Williams %R level for longs", "Indicators");

		_williamsROverbought = Param(nameof(WilliamsROverbought), -20m)
			.SetDisplay("Williams %R Overbought", "Williams %R level for shorts", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ma = CreateMovingAverage();
		var williams = new WilliamsR { Length = WilliamsRPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ma, williams, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, williams);
			}
		}
	}

	private DecimalLengthIndicator CreateMovingAverage()
	{
		return MaType switch
		{
			MovingAverageTypes.Exponential => new ExponentialMovingAverage { Length = MaPeriod },
			MovingAverageTypes.Weighted => new WeightedMovingAverage { Length = MaPeriod },
			MovingAverageTypes.Smoothed => new SmoothedMovingAverage { Length = MaPeriod },
			MovingAverageTypes.HullMA => new HullMovingAverage { Length = MaPeriod },
			_ => new SimpleMovingAverage { Length = MaPeriod },
		};
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue williamsValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!maValue.IsFormed || !williamsValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ma = maValue.GetValue<decimal>();
		var williams = williamsValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (close > ma && williams < WilliamsROversold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < ma && williams > WilliamsROverbought && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && williams >= -50m)
			SellMarket(Position);
		else if (Position < 0 && williams <= -50m)
			BuyMarket(-Position);
	}

	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MovingAverageTypes
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Weighted,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,

		/// <summary>
		/// Hull moving average.
		/// </summary>
		HullMA,
	}
}
