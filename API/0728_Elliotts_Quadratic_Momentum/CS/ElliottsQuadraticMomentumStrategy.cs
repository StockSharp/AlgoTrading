using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Elliott's Quadratic Momentum strategy using multiple SuperTrend indicators.
/// Enters long when all SuperTrend indicators confirm uptrend and short when all confirm downtrend.
/// Exits when any SuperTrend changes direction.
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
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<DataType> _candleType;

	private SuperTrend _st1;
	private SuperTrend _st2;
	private SuperTrend _st3;
	private SuperTrend _st4;

	private int _prevTrend1;
	private int _prevTrend2;
	private int _prevTrend3;
	private int _prevTrend4;

	/// <summary>
	/// ATR length for SuperTrend 1.
	/// </summary>
	public int AtrLength1
	{
		get => _atrLength1.Value;
		set => _atrLength1.Value = value;
	}

	/// <summary>
	/// Multiplier for SuperTrend 1.
	/// </summary>
	public decimal Multiplier1
	{
		get => _multiplier1.Value;
		set => _multiplier1.Value = value;
	}

	/// <summary>
	/// ATR length for SuperTrend 2.
	/// </summary>
	public int AtrLength2
	{
		get => _atrLength2.Value;
		set => _atrLength2.Value = value;
	}

	/// <summary>
	/// Multiplier for SuperTrend 2.
	/// </summary>
	public decimal Multiplier2
	{
		get => _multiplier2.Value;
		set => _multiplier2.Value = value;
	}

	/// <summary>
	/// ATR length for SuperTrend 3.
	/// </summary>
	public int AtrLength3
	{
		get => _atrLength3.Value;
		set => _atrLength3.Value = value;
	}

	/// <summary>
	/// Multiplier for SuperTrend 3.
	/// </summary>
	public decimal Multiplier3
	{
		get => _multiplier3.Value;
		set => _multiplier3.Value = value;
	}

	/// <summary>
	/// ATR length for SuperTrend 4.
	/// </summary>
	public int AtrLength4
	{
		get => _atrLength4.Value;
		set => _atrLength4.Value = value;
	}

	/// <summary>
	/// Multiplier for SuperTrend 4.
	/// </summary>
	public decimal Multiplier4
	{
		get => _multiplier4.Value;
		set => _multiplier4.Value = value;
	}

	/// <summary>
	/// Enable long entries.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Enable short entries.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
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
			.SetDisplay("ATR Length 1", "ATR length for SuperTrend 1", "SuperTrend");

		_multiplier1 = Param(nameof(Multiplier1), 4.0m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier 1", "Multiplier for SuperTrend 1", "SuperTrend");

		_atrLength2 = Param(nameof(AtrLength2), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length 2", "ATR length for SuperTrend 2", "SuperTrend");

		_multiplier2 = Param(nameof(Multiplier2), 3.618m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier 2", "Multiplier for SuperTrend 2", "SuperTrend");

		_atrLength3 = Param(nameof(AtrLength3), 21)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length 3", "ATR length for SuperTrend 3", "SuperTrend");

		_multiplier3 = Param(nameof(Multiplier3), 3.5m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier 3", "Multiplier for SuperTrend 3", "SuperTrend");

		_atrLength4 = Param(nameof(AtrLength4), 28)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length 4", "ATR length for SuperTrend 4", "SuperTrend");

		_multiplier4 = Param(nameof(Multiplier4), 3.382m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier 4", "Multiplier for SuperTrend 4", "SuperTrend");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Long Entries", "Enable long entries", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Short Entries", "Enable short entries", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevTrend1 = _prevTrend2 = _prevTrend3 = _prevTrend4 = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_st1 = new SuperTrend { Length = AtrLength1, Multiplier = Multiplier1 };
		_st2 = new SuperTrend { Length = AtrLength2, Multiplier = Multiplier2 };
		_st3 = new SuperTrend { Length = AtrLength3, Multiplier = Multiplier3 };
		_st4 = new SuperTrend { Length = AtrLength4, Multiplier = Multiplier4 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_st1, _st2, _st3, _st4, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _st1);
			DrawIndicator(area, _st2);
			DrawIndicator(area, _st3);
			DrawIndicator(area, _st4);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal st1Value, decimal st2Value, decimal st3Value, decimal st4Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trend1 = candle.ClosePrice > st1Value ? 1 : -1;
		var trend2 = candle.ClosePrice > st2Value ? 1 : -1;
		var trend3 = candle.ClosePrice > st3Value ? 1 : -1;
		var trend4 = candle.ClosePrice > st4Value ? 1 : -1;

		var longCondition = trend1 > 0 && trend2 > 0 && trend3 > 0 && trend4 > 0;
		var shortCondition = trend1 < 0 && trend2 < 0 && trend3 < 0 && trend4 < 0;

		var trendChanged = trend1 != _prevTrend1 || trend2 != _prevTrend2 || trend3 != _prevTrend3 || trend4 != _prevTrend4;

		if (EnableLong && longCondition && Position <= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (EnableShort && shortCondition && Position >= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (trendChanged && Position != 0)
		{
			ClosePosition();
		}

		_prevTrend1 = trend1;
		_prevTrend2 = trend2;
		_prevTrend3 = trend3;
		_prevTrend4 = trend4;
	}
}
