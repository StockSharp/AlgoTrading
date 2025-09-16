using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified version of Scalpel EA using multi-timeframe breakout with CCI filter.
/// </summary>
public class ScalpelEaStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciLimit;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci = null!;
	private decimal _prevHighMain;
	private decimal _prevLowMain;
	private decimal _prevHighH4, _prevLowH4, _currHighH4, _currLowH4;
	private decimal _prevHighH1, _prevLowH1, _currHighH1, _currLowH1;
	private decimal _prevHighM30, _prevLowM30, _currHighM30, _currLowM30;

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Threshold for CCI entries.
	/// </summary>
	public decimal CciLimit
	{
		get => _cciLimit.Value;
		set => _cciLimit.Value = value;
	}

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Primary candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ScalpelEaStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period of CCI indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 5);

		_cciLimit = Param(nameof(CciLimit), 3m)
			.SetDisplay("CCI Limit", "CCI threshold for entries", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_takeProfit = Param(nameof(TakeProfit), 30m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_stopLoss = Param(nameof(StopLoss), 21m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, TimeSpan.FromMinutes(30).TimeFrame());
		yield return (Security, TimeSpan.FromHours(1).TimeFrame());
		yield return (Security, TimeSpan.FromHours(4).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_cci?.Reset();
		_prevHighMain = _prevLowMain = 0m;
		_prevHighH4 = _prevLowH4 = _currHighH4 = _currLowH4 = 0m;
		_prevHighH1 = _prevLowH1 = _currHighH1 = _currLowH1 = 0m;
		_prevHighM30 = _prevLowM30 = _currHighM30 = _currLowM30 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cci = new CommodityChannelIndex { Length = CciPeriod };

		var main = SubscribeCandles(CandleType);
		var m30 = SubscribeCandles(TimeSpan.FromMinutes(30).TimeFrame());
		var h1 = SubscribeCandles(TimeSpan.FromHours(1).TimeFrame());
		var h4 = SubscribeCandles(TimeSpan.FromHours(4).TimeFrame());

		main.Bind(_cci, ProcessMain).Start();
		m30.Bind(ProcessM30).Start();
		h1.Bind(ProcessH1).Start();
		h4.Bind(ProcessH4).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Price),
			stopLoss: new Unit(StopLoss, UnitTypes.Price)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, main);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessM30(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_prevHighM30 = _currHighM30;
		_prevLowM30 = _currLowM30;
		_currHighM30 = candle.HighPrice;
		_currLowM30 = candle.LowPrice;
	}

	private void ProcessH1(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_prevHighH1 = _currHighH1;
		_prevLowH1 = _currLowH1;
		_currHighH1 = candle.HighPrice;
		_currLowH1 = candle.LowPrice;
	}

	private void ProcessH4(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_prevHighH4 = _currHighH4;
		_prevLowH4 = _currLowH4;
		_currHighH4 = candle.HighPrice;
		_currLowH4 = candle.LowPrice;
	}

	private void ProcessMain(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var cciBuy = cciValue > 0m && cciValue < CciLimit;
		var cciSell = cciValue < 0m && -cciValue < CciLimit;

		var breakoutHigh = candle.ClosePrice > _prevHighMain;
		var breakoutLow = candle.ClosePrice < _prevLowMain;

		if (cciBuy &&
		_currLowH4 > _prevLowH4 &&
		_currLowH1 > _prevLowH1 &&
		_currLowM30 > _prevLowM30 &&
		breakoutHigh &&
		Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo($"Buy signal: CCI={cciValue}");
		}
		else if (cciSell &&
		_currHighH4 < _prevHighH4 &&
		_currHighH1 < _prevHighH1 &&
		_currHighM30 < _prevHighM30 &&
		breakoutLow &&
		Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			LogInfo($"Sell signal: CCI={cciValue}");
		}

		_prevHighMain = candle.HighPrice;
		_prevLowMain = candle.LowPrice;
	}
}
