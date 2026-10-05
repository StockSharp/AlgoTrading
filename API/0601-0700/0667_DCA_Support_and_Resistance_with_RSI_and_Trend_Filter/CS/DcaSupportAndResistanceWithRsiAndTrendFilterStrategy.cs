using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// DCA support and resistance strategy with RSI and EMA trend filter.
/// Support and resistance are the lowest low and highest high of the previous LookbackPeriod candles.
/// When price touches support with RSI below Oversold and the close above the EMA, another long lot is bought;
/// when price touches resistance with RSI above Overbought and the close below the EMA, another short lot is sold.
/// A long closes at resistance or when RSI rises above Overbought; a short closes at support or when RSI falls below Oversold.
/// </summary>
public class DcaSupportAndResistanceWithRsiAndTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevSupport;
	private decimal? _prevResistance;

	/// <summary>
	/// Candles that define support and resistance.
	/// </summary>
	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }

	/// <summary>
	/// Trend EMA period.
	/// </summary>
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public DcaSupportAndResistanceWithRsiAndTrendFilterStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Candles that define support and resistance", "Levels");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "RSI");

		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "RSI overbought level", "RSI");

		_oversold = Param(nameof(Oversold), 40m)
			.SetDisplay("Oversold", "RSI oversold level", "RSI");

		_emaPeriod = Param(nameof(EmaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Trend EMA period", "Trend");

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
		_prevSupport = null;
		_prevResistance = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSupport = null;
		_prevResistance = null;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var highest = new Highest { Length = LookbackPeriod };
		var lowest = new Lowest { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, rsi, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue rsiValue, IIndicatorValue highestValue, IIndicatorValue lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Levels come from the candles before this one.
		var support = _prevSupport;
		var resistance = _prevResistance;

		if (highestValue.IsFormed && lowestValue.IsFormed)
		{
			_prevResistance = highestValue.GetValue<decimal>();
			_prevSupport = lowestValue.GetValue<decimal>();
		}

		if (!emaValue.IsFormed || !rsiValue.IsFormed || support is not decimal sup || resistance is not decimal res)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ema = emaValue.GetValue<decimal>();
		var rsi = rsiValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		var atSupport = candle.LowPrice <= sup;
		var atResistance = candle.HighPrice >= res;

		if (Position > 0 && (atResistance || rsi > Overbought))
		{
			SellMarket(Position);
			return;
		}

		if (Position < 0 && (atSupport || rsi < Oversold))
		{
			BuyMarket(-Position);
			return;
		}

		if (atSupport && rsi < Oversold && close > ema && Position >= 0)
			BuyMarket();
		else if (atResistance && rsi > Overbought && close < ema && Position <= 0)
			SellMarket();
	}
}
