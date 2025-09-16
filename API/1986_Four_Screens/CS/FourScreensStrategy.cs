using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Four Screens strategy using Heikin-Ashi candles on 5, 15, 30 and 60 minute timeframes.
/// </summary>
public class FourScreensStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _useTrailing;

	private decimal _prevHaOpen5;
	private decimal _prevHaClose5;
	private decimal _prevHaOpen15;
	private decimal _prevHaClose15;
	private decimal _prevHaOpen30;
	private decimal _prevHaClose30;
	private decimal _prevHaOpen60;
	private decimal _prevHaClose60;

	private bool? _bull5;
	private bool? _bull15;
	private bool? _bull30;
	private bool? _bull60;

	/// <summary>
	/// Base candle type (5 minute).
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FourScreensStrategy"/>.
	/// </summary>
	public FourScreensStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Base timeframe for 5m candles", "General");

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
			.SetDisplay("Stop Loss Points", "Stop-loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20m)
			.SetDisplay("Take Profit Points", "Take-profit distance in points", "Risk");

		_useTrailing = Param(nameof(UseTrailing), true)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, TimeSpan.FromMinutes(15).TimeFrame());
		yield return (Security, TimeSpan.FromMinutes(30).TimeFrame());
		yield return (Security, TimeSpan.FromMinutes(60).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHaOpen5 = _prevHaClose5 = 0m;
		_prevHaOpen15 = _prevHaClose15 = 0m;
		_prevHaOpen30 = _prevHaClose30 = 0m;
		_prevHaOpen60 = _prevHaClose60 = 0m;

		_bull5 = _bull15 = _bull30 = _bull60 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			takeProfit: new Unit(TakeProfitPoints, UnitTypes.Point),
			stopLoss: new Unit(StopLossPoints, UnitTypes.Point),
			isStopTrailing: UseTrailing);

		var sub5 = SubscribeCandles(CandleType);
		sub5.Bind(ProcessCandle5).Start();

		var sub15 = SubscribeCandles(TimeSpan.FromMinutes(15).TimeFrame());
		sub15.Bind(ProcessCandle15).Start();

		var sub30 = SubscribeCandles(TimeSpan.FromMinutes(30).TimeFrame());
		sub30.Bind(ProcessCandle30).Start();

		var sub60 = SubscribeCandles(TimeSpan.FromMinutes(60).TimeFrame());
		sub60.Bind(ProcessCandle60).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub5);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle5(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var (haOpen, haClose) = CalcHeikinAshi(candle, ref _prevHaOpen5, ref _prevHaClose5);
		_bull5 = haClose > haOpen;
		CheckSignal();
	}

	private void ProcessCandle15(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var (haOpen, haClose) = CalcHeikinAshi(candle, ref _prevHaOpen15, ref _prevHaClose15);
		_bull15 = haClose > haOpen;
		CheckSignal();
	}

	private void ProcessCandle30(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var (haOpen, haClose) = CalcHeikinAshi(candle, ref _prevHaOpen30, ref _prevHaClose30);
		_bull30 = haClose > haOpen;
		CheckSignal();
	}

	private void ProcessCandle60(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var (haOpen, haClose) = CalcHeikinAshi(candle, ref _prevHaOpen60, ref _prevHaClose60);
		_bull60 = haClose > haOpen;
		CheckSignal();
	}

	private static (decimal haOpen, decimal haClose) CalcHeikinAshi(ICandleMessage candle, ref decimal prevOpen, ref decimal prevClose)
	{
		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = prevOpen == 0m ? (candle.OpenPrice + candle.ClosePrice) / 2m : (prevOpen + prevClose) / 2m;
		prevOpen = haOpen;
		prevClose = haClose;
		return (haOpen, haClose);
	}

	private void CheckSignal()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_bull5 is null || _bull15 is null || _bull30 is null || _bull60 is null)
			return;

		var allBull = _bull5.Value && _bull15.Value && _bull30.Value && _bull60.Value;
		var allBear = !_bull5.Value && !_bull15.Value && !_bull30.Value && !_bull60.Value;

		if (allBull && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (allBear && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
