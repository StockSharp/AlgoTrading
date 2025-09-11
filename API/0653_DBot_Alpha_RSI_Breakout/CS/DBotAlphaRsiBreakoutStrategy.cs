using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alpha RSI breakout strategy with trailing stop logic.
/// </summary>
public class DBotAlphaRsiBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiEntry;
	private readonly StrategyParam<decimal> _rsiStopLoss;
	private readonly StrategyParam<decimal> _rsiTakeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _trailingStopActivate;
	private decimal? _trailingStop;
	private decimal? _lastClose;

	/// <summary>
	/// Period for simple moving average.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Period for relative strength index.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI entry level.
	/// </summary>
	public decimal RsiEntry
	{
		get => _rsiEntry.Value;
		set => _rsiEntry.Value = value;
	}

	/// <summary>
	/// RSI stop loss level.
	/// </summary>
	public decimal RsiStopLoss
	{
		get => _rsiStopLoss.Value;
		set => _rsiStopLoss.Value = value;
	}

	/// <summary>
	/// RSI take profit level.
	/// </summary>
	public decimal RsiTakeProfit
	{
		get => _rsiTakeProfit.Value;
		set => _rsiTakeProfit.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public DBotAlphaRsiBreakoutStrategy()
	{
		_smaLength = Param(nameof(SmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Period for simple moving average", "General")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 10);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period for relative strength index", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_rsiEntry = Param(nameof(RsiEntry), 34m)
			.SetDisplay("RSI Entry", "RSI entry level", "Levels")
			.SetCanOptimize(true)
			.SetOptimize(20m, 50m, 1m);

		_rsiStopLoss = Param(nameof(RsiStopLoss), 30m)
			.SetDisplay("RSI Stop Loss", "RSI stop loss level", "Levels")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 1m);

		_rsiTakeProfit = Param(nameof(RsiTakeProfit), 50m)
			.SetDisplay("RSI Take Profit", "RSI take profit level", "Levels")
			.SetCanOptimize(true)
			.SetOptimize(40m, 80m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_prevRsi = 0m;
		_trailingStopActivate = false;
		_trailingStop = null;
		_lastClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SMA { Length = SmaLength };
		var rsi = new RSI { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossedUp = _prevRsi <= RsiEntry && rsiValue > RsiEntry;
		var longCondition = crossedUp && candle.ClosePrice > smaValue;

		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));

			_trailingStopActivate = false;
			_trailingStop = null;
			_lastClose = null;
		}

		if (Position > 0)
		{
			if (_lastClose == null || candle.ClosePrice < _lastClose)
			{
				_lastClose = candle.ClosePrice;
				_trailingStop = candle.ClosePrice;
			}

			if (rsiValue >= RsiTakeProfit)
				_trailingStopActivate = true;

			if (_trailingStopActivate && _trailingStop != null && candle.ClosePrice < _trailingStop)
				SellMarket(Math.Abs(Position));

			if (rsiValue <= RsiStopLoss)
				SellMarket(Math.Abs(Position));

			if (!_trailingStopActivate && rsiValue >= RsiTakeProfit)
				SellMarket(Math.Abs(Position));

			if (_trailingStopActivate && rsiValue >= RsiTakeProfit)
				SellMarket(Math.Abs(Position));
		}

		_prevRsi = rsiValue;
	}
}
