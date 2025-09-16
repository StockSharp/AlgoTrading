namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Simple SuperTrend crossover strategy.
/// </summary>
public class SupertrendSignalStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevTrend;
	private decimal _prevClose;

	/// <summary>
	/// ATR period for SuperTrend calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for SuperTrend.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Candle type for market data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SupertrendSignalStrategy"/>.
	/// </summary>
	public SupertrendSignalStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 5)
			.SetDisplay("ATR Period", "ATR period for SuperTrend", "Parameters");

		_multiplier = Param(nameof(Multiplier), 3m)
			.SetDisplay("Multiplier", "ATR multiplier for SuperTrend", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "Parameters");
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
		_prevTrend = 0m;
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var st = new SuperTrend { Length = AtrPeriod, Multiplier = Multiplier };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(st, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, st);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!stValue.IsFinal)
			return;

		var st = (SuperTrendIndicatorValue)stValue;
		var trend = st.Value;

		var longSignal = _prevClose <= _prevTrend && candle.ClosePrice > trend;
		var shortSignal = _prevClose >= _prevTrend && candle.ClosePrice < trend;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevClose = candle.ClosePrice;
		_prevTrend = trend;
	}
}

