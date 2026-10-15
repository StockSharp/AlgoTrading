using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Elliott's Quadratic Momentum strategy.
/// Four SuperTrend indicators with different ATR lengths and multipliers vote on the trend. When all four are up the
/// strategy goes long and when all four are down it goes short. A position closes as soon as any SuperTrend turns
/// against it.
/// </summary>
public class ElliottsQuadraticMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength1;
	private readonly StrategyParam<decimal> _multiplier1;
	private readonly StrategyParam<int> _atrLength2;
	private readonly StrategyParam<decimal> _multiplier2;
	private readonly StrategyParam<int> _atrLength3;
	private readonly StrategyParam<decimal> _multiplier3;
	private readonly StrategyParam<int> _atrLength4;
	private readonly StrategyParam<decimal> _multiplier4;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// ATR length of the first SuperTrend.
	/// </summary>
	public int AtrLength1
	{
		get => _atrLength1.Value;
		set => _atrLength1.Value = value;
	}

	/// <summary>
	/// Multiplier of the first SuperTrend.
	/// </summary>
	public decimal Multiplier1
	{
		get => _multiplier1.Value;
		set => _multiplier1.Value = value;
	}

	/// <summary>
	/// ATR length of the second SuperTrend.
	/// </summary>
	public int AtrLength2
	{
		get => _atrLength2.Value;
		set => _atrLength2.Value = value;
	}

	/// <summary>
	/// Multiplier of the second SuperTrend.
	/// </summary>
	public decimal Multiplier2
	{
		get => _multiplier2.Value;
		set => _multiplier2.Value = value;
	}

	/// <summary>
	/// ATR length of the third SuperTrend.
	/// </summary>
	public int AtrLength3
	{
		get => _atrLength3.Value;
		set => _atrLength3.Value = value;
	}

	/// <summary>
	/// Multiplier of the third SuperTrend.
	/// </summary>
	public decimal Multiplier3
	{
		get => _multiplier3.Value;
		set => _multiplier3.Value = value;
	}

	/// <summary>
	/// ATR length of the fourth SuperTrend.
	/// </summary>
	public int AtrLength4
	{
		get => _atrLength4.Value;
		set => _atrLength4.Value = value;
	}

	/// <summary>
	/// Multiplier of the fourth SuperTrend.
	/// </summary>
	public decimal Multiplier4
	{
		get => _multiplier4.Value;
		set => _multiplier4.Value = value;
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
	public ElliottsQuadraticMomentumStrategy()
	{
		_atrLength1 = Param(nameof(AtrLength1), 7)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length 1", "ATR length of the first SuperTrend", "SuperTrend 1");

		_multiplier1 = Param(nameof(Multiplier1), 4.0m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier 1", "Multiplier of the first SuperTrend", "SuperTrend 1");

		_atrLength2 = Param(nameof(AtrLength2), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length 2", "ATR length of the second SuperTrend", "SuperTrend 2");

		_multiplier2 = Param(nameof(Multiplier2), 3.618m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier 2", "Multiplier of the second SuperTrend", "SuperTrend 2");

		_atrLength3 = Param(nameof(AtrLength3), 21)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length 3", "ATR length of the third SuperTrend", "SuperTrend 3");

		_multiplier3 = Param(nameof(Multiplier3), 3.5m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier 3", "Multiplier of the third SuperTrend", "SuperTrend 3");

		_atrLength4 = Param(nameof(AtrLength4), 28)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length 4", "ATR length of the fourth SuperTrend", "SuperTrend 4");

		_multiplier4 = Param(nameof(Multiplier4), 3.382m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier 4", "Multiplier of the fourth SuperTrend", "SuperTrend 4");

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

		var st1 = new SuperTrend { Length = AtrLength1, Multiplier = Multiplier1 };
		var st2 = new SuperTrend { Length = AtrLength2, Multiplier = Multiplier2 };
		var st3 = new SuperTrend { Length = AtrLength3, Multiplier = Multiplier3 };
		var st4 = new SuperTrend { Length = AtrLength4, Multiplier = Multiplier4 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(st1, st2, st3, st4, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, st1);
			DrawIndicator(area, st2);
			DrawIndicator(area, st3);
			DrawIndicator(area, st4);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue v1, IIndicatorValue v2, IIndicatorValue v3, IIndicatorValue v4)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!v1.IsFormed || !v2.IsFormed || !v3.IsFormed || !v4.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var up1 = ((SuperTrendIndicatorValue)v1).IsUpTrend;
		var up2 = ((SuperTrendIndicatorValue)v2).IsUpTrend;
		var up3 = ((SuperTrendIndicatorValue)v3).IsUpTrend;
		var up4 = ((SuperTrendIndicatorValue)v4).IsUpTrend;

		var allUp = up1 && up2 && up3 && up4;
		var allDown = !up1 && !up2 && !up3 && !up4;

		if (allUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (allDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && !allUp)
			SellMarket(Position);
		else if (Position < 0 && !allDown)
			BuyMarket(-Position);
	}
}
