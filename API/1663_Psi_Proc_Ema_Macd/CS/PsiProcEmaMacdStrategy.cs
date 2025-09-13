using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on multiple EMA alignment with MACD confirmation.
/// </summary>
public class PsiProcEmaMacdStrategy : Strategy
{
	private readonly StrategyParam<decimal> _limitMacd;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailStopPoints;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema200;
	private ExponentialMovingAverage _ema50;
	private ExponentialMovingAverage _ema10;
	private MovingAverageConvergenceDivergence _macd;

	private decimal _prevEma200;
	private decimal _prevEma50;
	private decimal _prevEma10;
	private decimal _prevMacd;
	private bool _initialized;

	/// <summary>
	/// Minimal MACD value to allow entry.
	/// </summary>
	public decimal LimitMACD
	{
		get => _limitMacd.Value;
		set => _limitMacd.Value = value;
	}

	/// <summary>
	/// Take profit in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop in price points.
	/// </summary>
	public decimal TrailStopPoints
	{
		get => _trailStopPoints.Value;
		set => _trailStopPoints.Value = value;
	}

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="PsiProcEmaMacdStrategy"/>.
	/// </summary>
	public PsiProcEmaMacdStrategy()
	{
		_limitMacd = Param(nameof(LimitMACD), 0.0005m)
			.SetDisplay("Limit MACD", "Minimal MACD value to allow trades", "General");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1000m)
			.SetDisplay("Take Profit", "Take profit in price points", "Risk");

		_trailStopPoints = Param(nameof(TrailStopPoints), 500m)
			.SetDisplay("Trail Stop", "Trailing stop in price points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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

		_prevEma200 = 0m;
		_prevEma50 = 0m;
		_prevEma10 = 0m;
		_prevMacd = 0m;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema200 = new ExponentialMovingAverage { Length = 200 };
		_ema50 = new ExponentialMovingAverage { Length = 50 };
		_ema10 = new ExponentialMovingAverage { Length = 10 };
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = 12,
			LongPeriod = 26,
			SignalPeriod = 9
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema200, _ema50, _ema10, _macd, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfitPoints, UnitTypes.Price),
			new Unit(TrailStopPoints, UnitTypes.Price),
			isStopTrailing: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema200);
			DrawIndicator(area, _ema50);
			DrawIndicator(area, _ema10);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema200, decimal ema50, decimal ema10, decimal macd, decimal signal, decimal hist)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized)
		{
			_prevEma200 = ema200;
			_prevEma50 = ema50;
			_prevEma10 = ema10;
			_prevMacd = macd;
			_initialized = true;
			return;
		}

		if (Position > 0 && candle.ClosePrice < ema50)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && candle.ClosePrice > ema50)
		{
			BuyMarket(-Position);
		}
		else
		{
			var longCond = ema200 > _prevEma200 && ema50 > _prevEma50 && ema50 > ema200 && ema10 > _prevEma10 && ema10 > ema50 && macd > _prevMacd && macd > LimitMACD;
			var shortCond = ema200 < _prevEma200 && ema50 < _prevEma50 && ema50 < ema200 && ema10 < _prevEma10 && ema10 < ema50 && macd < _prevMacd && macd < -LimitMACD;

			if (longCond && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (shortCond && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		_prevEma200 = ema200;
		_prevEma50 = ema50;
		_prevEma10 = ema10;
		_prevMacd = macd;
	}
}
