using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Improved EMA and CDC trailing stop strategy.
/// A long opens when the close is above EMA60, EMA60 is above EMA90 and the MACD (12, 26, 9) line is above its signal line; a short
/// mirrors these rules and an opposite signal reverses the position. A CDC ATR trailing stop follows the close at Multiplier ATRs and only
/// moves in the trade's favour, and a profit target sits ProfitTargetMultiplier ATRs from the entry.
/// </summary>
public class ImprovedEmaCdcTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _ema60Period;
	private readonly StrategyParam<int> _ema90Period;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _profitTargetMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// EMA 60 period.
	/// </summary>
	public int Ema60Period
	{
		get => _ema60Period.Value;
		set => _ema60Period.Value = value;
	}

	/// <summary>
	/// EMA 90 period.
	/// </summary>
	public int Ema90Period
	{
		get => _ema90Period.Value;
		set => _ema90Period.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the trailing stop.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the profit target.
	/// </summary>
	public decimal ProfitTargetMultiplier
	{
		get => _profitTargetMultiplier.Value;
		set => _profitTargetMultiplier.Value = value;
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
	/// Initializes a new instance of <see cref="ImprovedEmaCdcTrailingStopStrategy"/>.
	/// </summary>
	public ImprovedEmaCdcTrailingStopStrategy()
	{
		_ema60Period = Param(nameof(Ema60Period), 60)
			.SetGreaterThanZero()
			.SetDisplay("EMA 60 Period", "Length of the fast EMA", "Parameters")
			.SetOptimize(20, 100, 10);

		_ema90Period = Param(nameof(Ema90Period), 90)
			.SetGreaterThanZero()
			.SetDisplay("EMA 90 Period", "Length of the slow EMA", "Parameters")
			.SetOptimize(30, 120, 10);

		_atrPeriod = Param(nameof(AtrPeriod), 24)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR calculation", "Parameters")
			.SetOptimize(14, 50, 2);

		_multiplier = Param(nameof(Multiplier), 4m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "ATR multiplier for the trailing stop", "Parameters")
			.SetOptimize(1m, 5m, 1m);

		_profitTargetMultiplier = Param(nameof(ProfitTargetMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("Profit Target Multiplier", "ATR multiplier for the profit target", "Parameters")
			.SetOptimize(1m, 5m, 1m);

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
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stopPrice = null;
		_takePrice = null;

		var ema60 = new ExponentialMovingAverage { Length = Ema60Period };
		var ema90 = new ExponentialMovingAverage { Length = Ema90Period };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = 12 }, LongMa = { Length = 26 } },
			SignalMa = { Length = 9 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ema60, ema90, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema60);
			DrawIndicator(area, ema90);
			DrawOwnTrades(area);

			var macdArea = CreateChartArea();
			if (macdArea != null)
				DrawIndicator(macdArea, macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue ema60Value, IIndicatorValue ema90Value, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!atrValue.IsFormed)
			return;

		var atr = atrValue.GetValue<decimal>();

		if (ManageExits(candle, atr))
			return;

		if (!macdValue.IsFormed || macdValue is not MovingAverageConvergenceDivergenceSignalValue { Macd: decimal macd, Signal: decimal signal })
			return;

		if (!ema60Value.IsFormed || !ema90Value.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ema60 = ema60Value.GetValue<decimal>();
		var ema90 = ema90Value.GetValue<decimal>();
		var close = candle.ClosePrice;

		var longCondition = close > ema60 && ema60 > ema90 && macd > signal;
		var shortCondition = close < ema60 && ema60 < ema90 && macd < signal;

		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			SetLevels(close, atr, 1m);
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			SetLevels(close, atr, -1m);
		}
	}

	private void SetLevels(decimal entry, decimal atr, decimal sign)
	{
		_stopPrice = Multiplier > 0 ? entry - sign * atr * Multiplier : null;
		_takePrice = ProfitTargetMultiplier > 0 ? entry + sign * atr * ProfitTargetMultiplier : null;
	}

	// Returns true when the trailing stop or profit target closed the position on this candle.
	private bool ManageExits(ICandleMessage candle, decimal atr)
	{
		if (Position == 0)
		{
			_stopPrice = null;
			_takePrice = null;
			return false;
		}

		var isLong = Position > 0;
		var stopHit = _stopPrice is decimal sl && (isLong ? candle.LowPrice <= sl : candle.HighPrice >= sl);
		var takeHit = _takePrice is decimal tp && (isLong ? candle.HighPrice >= tp : candle.LowPrice <= tp);

		if (stopHit || takeHit)
		{
			if (isLong)
				SellMarket(Position);
			else
				BuyMarket(-Position);

			_stopPrice = null;
			_takePrice = null;
			return true;
		}

		if (_stopPrice is decimal stop)
		{
			var distance = atr * Multiplier;

			_stopPrice = isLong
				? Math.Max(stop, candle.ClosePrice - distance)
				: Math.Min(stop, candle.ClosePrice + distance);
		}

		return false;
	}
}
