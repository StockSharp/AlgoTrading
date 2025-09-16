using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that closes positions when price crosses the Kijun-sen line.
/// </summary>
public class CloseCrossKijunSenStrategy : Strategy
{
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<decimal> _pointsToCross;
	private readonly StrategyParam<DataType> _candleType;

	private Ichimoku _ichimoku;
	private decimal _prevPrice;
	private decimal _prevKijun;
	private bool _isFirst = true;

	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Offset in points added to the Kijun-sen line.
	/// </summary>
	public decimal PointsToCross
	{
		get => _pointsToCross.Value;
		set => _pointsToCross.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CloseCrossKijunSenStrategy"/>.
	/// </summary>
	public CloseCrossKijunSenStrategy()
	{
		_kijunPeriod = Param(nameof(KijunPeriod), 50)
			.SetDisplay("Kijun Period", "Period for Kijun-sen calculation", "Parameters")
			.SetRange(10, 100)
			.SetCanOptimize(true);

		_pointsToCross = Param(nameof(PointsToCross), 0m)
			.SetDisplay("Offset (points)", "Number of points added to Kijun-sen", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Parameters");
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
		_prevPrice = 0m;
		_prevKijun = 0m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = 9 },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = 52 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ichimoku, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ichimoku);
			DrawOwnTrades(area);
		}
	}

	/// <summary>
	/// Process candle with Ichimoku indicator values.
	/// </summary>
	/// <param name="candle">Candle.</param>
	/// <param name="ichimokuValue">Ichimoku indicator value.</param>
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ichimokuTyped = (IchimokuValue)ichimokuValue;

		if (ichimokuTyped.Kijun is not decimal kijun)
			return;

		var offset = PointsToCross * (Security.PriceStep ?? 1m);

		if (_isFirst)
		{
			_prevPrice = candle.ClosePrice;
			_prevKijun = kijun;
			_isFirst = false;
			return;
		}

		var crossedDown = _prevPrice > _prevKijun && candle.ClosePrice <= kijun - offset;
		var crossedUp = _prevPrice < _prevKijun && candle.ClosePrice >= kijun + offset;

		if (Position > 0 && crossedDown)
			ClosePosition();

		if (Position < 0 && crossedUp)
			ClosePosition();

		_prevPrice = candle.ClosePrice;
		_prevKijun = kijun;
	}
}
