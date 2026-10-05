using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR early buy with moving average exit.
/// A close crossing above the Parabolic SAR goes long and a close crossing below it goes short, reversing an opposite position.
/// A long is also closed early when the SAR is above the close and the close is below the MaPeriod simple moving average.
/// </summary>
public class ParabolicSarEarlyBuyMaExitStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStart;
	private readonly StrategyParam<decimal> _sarIncrement;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevSar;

	/// <summary>
	/// Initial SAR acceleration factor.
	/// </summary>
	public decimal SarStart
	{
		get => _sarStart.Value;
		set => _sarStart.Value = value;
	}

	/// <summary>
	/// SAR acceleration factor increment.
	/// </summary>
	public decimal SarIncrement
	{
		get => _sarIncrement.Value;
		set => _sarIncrement.Value = value;
	}

	/// <summary>
	/// Maximum SAR acceleration factor.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// Period of the exit moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	public ParabolicSarEarlyBuyMaExitStrategy()
	{
		_sarStart = Param(nameof(SarStart), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Start", "Initial SAR acceleration factor", "Indicators");

		_sarIncrement = Param(nameof(SarIncrement), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Increment", "SAR acceleration factor increment", "Indicators");

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max", "Maximum SAR acceleration factor", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period of the exit moving average", "Indicators");

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
		_prevClose = null;
		_prevSar = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevSar = null;

		var sar = new ParabolicSar
		{
			Acceleration = SarStart,
			AccelerationStep = SarIncrement,
			AccelerationMax = SarMax
		};
		var ma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sar, ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sar, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		var prevSar = _prevSar;
		_prevClose = close;
		_prevSar = sar;

		if (prevClose is not decimal pc || prevSar is not decimal ps)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = pc <= ps && close > sar;
		var crossDown = pc >= ps && close < sar;

		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && sar > close && close < ma)
			SellMarket(Position);
	}
}
