using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Flexible Moving Average strategy.
/// A long-only position is sized against a moving average of the chosen method. With AllowInitialBuy the full position is bought
/// on the first tradable candle. When a candle closes above the average after closing at or below it, the position is restored
/// to the full Volume; when it closes below after closing at or above, the position is reduced by SellPercentage percent.
/// </summary>
public class FlexibleMovingAverageStrategy : Strategy
{
	/// <summary>
	/// Moving average methods.
	/// </summary>
	public enum MaMethods
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		SMA,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		EMA,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		WMA,

		/// <summary>
		/// Hull moving average.
		/// </summary>
		HMA,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		SMMA,
	}

	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _sellPercentage;
	private readonly StrategyParam<MaMethods> _maMethod;
	private readonly StrategyParam<bool> _allowInitialBuy;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevMa;
	private bool _initialDone;

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Percent of the position sold on a cross below the average.
	/// </summary>
	public decimal SellPercentage
	{
		get => _sellPercentage.Value;
		set => _sellPercentage.Value = value;
	}

	/// <summary>
	/// Moving average method.
	/// </summary>
	public MaMethods MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Buy the full position on the first candle.
	/// </summary>
	public bool AllowInitialBuy
	{
		get => _allowInitialBuy.Value;
		set => _allowInitialBuy.Value = value;
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
	public FlexibleMovingAverageStrategy()
	{
		_maLength = Param(nameof(MaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "Indicators");

		_sellPercentage = Param(nameof(SellPercentage), 100m)
			.SetRange(0m, 100m)
			.SetDisplay("Sell %", "Percent of the position sold on a cross below the average", "Trading");

		_maMethod = Param(nameof(MaMethod), MaMethods.SMA)
			.SetDisplay("MA Method", "Moving average method", "Indicators");

		_allowInitialBuy = Param(nameof(AllowInitialBuy), true)
			.SetDisplay("Initial Buy", "Buy the full position on the first candle", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_prevClose = null;
		_prevMa = null;
		_initialDone = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevMa = null;
		_initialDone = false;

		var ma = CreateMovingAverage(MaMethod, MaLength);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private static DecimalLengthIndicator CreateMovingAverage(MaMethods method, int length)
	{
		return method switch
		{
			MaMethods.EMA => new ExponentialMovingAverage { Length = length },
			MaMethods.WMA => new WeightedMovingAverage { Length = length },
			MaMethods.HMA => new HullMovingAverage { Length = length },
			MaMethods.SMMA => new SmoothedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevClose = _prevClose;
		var prevMa = _prevMa;
		_prevClose = candle.ClosePrice;
		_prevMa = ma;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialDone)
		{
			_initialDone = true;

			if (AllowInitialBuy && Position < Volume)
			{
				BuyMarket(Volume - Position);
				return;
			}
		}

		if (prevClose is not decimal lastClose || prevMa is not decimal lastMa)
			return;

		var close = candle.ClosePrice;

		if (lastClose <= lastMa && close > ma)
		{
			if (Position < Volume)
				BuyMarket(Volume - Position);
		}
		else if (lastClose >= lastMa && close < ma && Position > 0)
		{
			var volume = SellPercentage >= 100m ? Position : RoundVolume(Position * SellPercentage / 100m);
			if (volume > 0)
				SellMarket(volume);
		}
	}

	private decimal RoundVolume(decimal volume)
	{
		var step = Security?.VolumeStep ?? 0m;
		return step > 0 ? Math.Floor(volume / step) * step : volume;
	}
}
