using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Short SMA crossover strategy with ADX filter and trailing stop.
/// </summary>
public class SafaBotAlertStrategy : Strategy
{
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailPoints;
	private readonly StrategyParam<int> _sessionCloseHour;
	private readonly StrategyParam<int> _sessionCloseMinute;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevSma;
	private decimal? _entryPrice;
	private decimal? _takePrice;
	private decimal? _stopPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	/// <summary>
	/// SMA period length.
	/// </summary>
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public decimal TrailPoints { get => _trailPoints.Value; set => _trailPoints.Value = value; }

	/// <summary>
	/// Session close hour.
	/// </summary>
	public int SessionCloseHour { get => _sessionCloseHour.Value; set => _sessionCloseHour.Value = value; }

	/// <summary>
	/// Session close minute.
	/// </summary>
	public int SessionCloseMinute { get => _sessionCloseMinute.Value; set => _sessionCloseMinute.Value = value; }

	/// <summary>
	/// ADX period length.
	/// </summary>
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }

	/// <summary>
	/// Minimum ADX to trade.
	/// </summary>
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SafaBotAlertStrategy()
	{
		_smaLength = Param(nameof(SmaLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Period of simple moving average", "Indicators");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 80m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Points", "Take profit distance in points", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 35m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Points", "Stop loss distance in points", "Risk");

		_trailPoints = Param(nameof(TrailPoints), 15m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Points", "Trailing stop distance in points", "Risk");

		_sessionCloseHour = Param(nameof(SessionCloseHour), 16)
			.SetDisplay("Session Close Hour", "Hour to force close positions (24h)", "Session");

		_sessionCloseMinute = Param(nameof(SessionCloseMinute), 0)
			.SetDisplay("Session Close Minute", "Minute to force close positions", "Session");

		_adxLength = Param(nameof(AdxLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "Period for ADX calculation", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 15m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold", "Minimum ADX value to trade", "Indicators");

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
		_prevSma = null;
		_entryPrice = null;
		_takePrice = null;
		_stopPrice = null;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SMA { Length = SmaLength };
		var adx = new AverageDirectionalIndex { Length = AdxLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);

			var second = CreateChartArea();
			if (second != null)
				DrawIndicator(second, adx);

			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaVal, IIndicatorValue adxVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adxTyped = (AverageDirectionalIndexValue)adxVal;
		if (adxTyped.MovingAverage is not decimal adx)
			return;

		var sma = smaVal.ToDecimal();

		if (_prevClose is decimal prevClose && _prevSma is decimal prevSma)
		{
			var longCond = prevClose <= prevSma && candle.ClosePrice > sma && adx > AdxThreshold;
			var shortCond = prevClose >= prevSma && candle.ClosePrice < sma && adx > AdxThreshold;

			if (longCond && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
				_takePrice = _entryPrice + TakeProfitPoints;
				_stopPrice = _entryPrice - StopLossPoints;
				_highestPrice = candle.HighPrice;
				_lowestPrice = 0m;
			}
			else if (shortCond && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
				_takePrice = _entryPrice - TakeProfitPoints;
				_stopPrice = _entryPrice + StopLossPoints;
				_lowestPrice = candle.LowPrice;
				_highestPrice = 0m;
			}
		}

		if (Position > 0 && _entryPrice is not null)
		{
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
			var trail = _highestPrice - TrailPoints;
			_stopPrice = _stopPrice is decimal stopL ? Math.Max(stopL, trail) : trail;

			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetPositionState();
			}
			else if (_takePrice is decimal tpL && candle.HighPrice >= tpL)
			{
				SellMarket(Position);
				ResetPositionState();
			}
		}
		else if (Position < 0 && _entryPrice is not null)
		{
			_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);
			var trail = _lowestPrice + TrailPoints;
			_stopPrice = _stopPrice is decimal stopS ? Math.Min(stopS, trail) : trail;

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
			else if (_takePrice is decimal tpS && candle.LowPrice <= tpS)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}

		if (candle.OpenTime.Hour == SessionCloseHour && candle.OpenTime.Minute == SessionCloseMinute && Position != 0)
		{
			ClosePosition();
			ResetPositionState();
		}

		_prevClose = candle.ClosePrice;
		_prevSma = sma;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_takePrice = null;
		_stopPrice = null;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}
}
