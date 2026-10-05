using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Three Kilos BTC 15m strategy.
/// Three TEMAs (ShortPeriod, LongPeriod, Long2Period) with a Supertrend filter. A long opens when TEMA2 crosses above TEMA1 while
/// TEMA2 is above TEMA3 and the Supertrend is up; a short opens when TEMA1 crosses above TEMA2 while TEMA2 is below TEMA3 and the
/// Supertrend is down. Positions close by a percent take profit or stop loss.
/// </summary>
public class ThreeKilosBtc15mStrategy : Strategy
{
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<int> _long2Period;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevTema1;
	private decimal? _prevTema2;

	/// <summary>
	/// Period of TEMA1.
	/// </summary>
	public int ShortPeriod
	{
		get => _shortPeriod.Value;
		set => _shortPeriod.Value = value;
	}

	/// <summary>
	/// Period of TEMA2.
	/// </summary>
	public int LongPeriod
	{
		get => _longPeriod.Value;
		set => _longPeriod.Value = value;
	}

	/// <summary>
	/// Period of TEMA3.
	/// </summary>
	public int Long2Period
	{
		get => _long2Period.Value;
		set => _long2Period.Value = value;
	}

	/// <summary>
	/// ATR length of the Supertrend.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the Supertrend.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
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
	public ThreeKilosBtc15mStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Short Period", "Period of TEMA1", "Indicators");

		_longPeriod = Param(nameof(LongPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Long Period", "Period of TEMA2", "Indicators");

		_long2Period = Param(nameof(Long2Period), 140)
			.SetGreaterThanZero()
			.SetDisplay("Long2 Period", "Period of TEMA3", "Indicators");

		_atrLength = Param(nameof(AtrLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR length of the Supertrend", "Supertrend");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "ATR multiplier of the Supertrend", "Supertrend");

		_takeProfit = Param(nameof(TakeProfit), 1m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_stopLoss = Param(nameof(StopLoss), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

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
		_prevTema1 = null;
		_prevTema2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevTema1 = null;
		_prevTema2 = null;

		var tema1 = new TripleExponentialMovingAverage { Length = ShortPeriod };
		var tema2 = new TripleExponentialMovingAverage { Length = LongPeriod };
		var tema3 = new TripleExponentialMovingAverage { Length = Long2Period };
		var superTrend = new SuperTrend { Length = AtrLength, Multiplier = Multiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(tema1, tema2, tema3, superTrend, ProcessCandle)
			.Start();

		StartProtection(new Unit(TakeProfit, UnitTypes.Percent), new Unit(StopLoss, UnitTypes.Percent), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, tema1);
			DrawIndicator(area, tema2);
			DrawIndicator(area, tema3);
			DrawIndicator(area, superTrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue tema1Value, IIndicatorValue tema2Value, IIndicatorValue tema3Value, IIndicatorValue superTrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!tema1Value.IsFormed || !tema2Value.IsFormed || !tema3Value.IsFormed || !superTrendValue.IsFormed)
			return;

		var tema1 = tema1Value.ToDecimal();
		var tema2 = tema2Value.ToDecimal();
		var tema3 = tema3Value.ToDecimal();
		var isUpTrend = ((SuperTrendIndicatorValue)superTrendValue).IsUpTrend;

		var prevTema1 = _prevTema1;
		var prevTema2 = _prevTema2;
		_prevTema1 = tema1;
		_prevTema2 = tema2;

		if (prevTema1 is not decimal p1 || prevTema2 is not decimal p2)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var tema2CrossesUp = p2 <= p1 && tema2 > tema1;
		var tema1CrossesUp = p1 <= p2 && tema1 > tema2;

		if (tema2CrossesUp && tema2 > tema3 && isUpTrend && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (tema1CrossesUp && tema2 < tema3 && !isUpTrend && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
