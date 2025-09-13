using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot breakout strategy with ATR and ADX filters.
/// Places trades when price breaks previous support or resistance.
/// </summary>
public class EmVolStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _trailStart;
	private readonly StrategyParam<decimal> _trailStep;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private Adx _adx;

	private ICandleMessage _prevCandle;
	private decimal _prevAtr;
	private decimal _prevAdx;

	private Order _stopOrder;
	private Order _targetOrder;
	private decimal _entryPrice;
	private decimal _tickSize;

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }

	/// <summary>
	/// Maximum ADX level to allow trading.
	/// </summary>
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }

	/// <summary>
	/// Profit required before trailing stop starts.
	/// </summary>
	public decimal TrailStart { get => _trailStart.Value; set => _trailStart.Value = value; }

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailStep { get => _trailStep.Value; set => _trailStep.Value = value; }

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="EmVolStrategy"/> class.
	/// </summary>
	public EmVolStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 100m)
			.SetDisplay("Take Profit", "Take profit distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50m, 200m, 50m);

		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 1000m, 100m);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "ADX period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_adxThreshold = Param(nameof(AdxThreshold), 30m)
			.SetDisplay("ADX Threshold", "Trades allowed when ADX below", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_trailStart = Param(nameof(TrailStart), 10m)
			.SetDisplay("Trailing Start", "Profit before trailing", "Risk");

		_trailStep = Param(nameof(TrailStep), 10m)
			.SetDisplay("Trailing Step", "Trailing distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security.PriceStep ?? 1m;

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_adx = new Adx { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_atr, _adx, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr, subscription);
			DrawIndicator(area, _adx, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal adx)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevCandle == null)
		{
			_prevCandle = candle;
			_prevAtr = atr;
			_prevAdx = adx;
			return;
		}

		var res1 = _prevCandle.HighPrice + _prevAtr;
		var sup1 = _prevCandle.LowPrice - _prevAtr;

		if (Position == 0 && _prevAdx < AdxThreshold)
		{
			var volume = Volume;
			if (volume <= 0)
				volume = 1;

			if (candle.ClosePrice > res1)
			{
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
				_stopOrder = SellStop(_entryPrice - StopLoss * _tickSize, volume);
				_targetOrder = SellLimit(_entryPrice + TakeProfit * _tickSize, volume);
			}
			else if (candle.ClosePrice < sup1)
			{
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
				_stopOrder = BuyStop(_entryPrice + StopLoss * _tickSize, volume);
				_targetOrder = BuyLimit(_entryPrice - TakeProfit * _tickSize, volume);
			}
		}
		else if (Position > 0 && TrailStart > 0)
		{
			var profit = candle.ClosePrice - _entryPrice;
			if (profit >= TrailStart * _tickSize)
			{
				var newStop = candle.ClosePrice - TrailStep * _tickSize;
				if (_stopOrder != null && newStop > _stopOrder.Price + _tickSize)
				{
					CancelOrder(_stopOrder);
					_stopOrder = SellStop(newStop, Position);
				}
			}
		}
		else if (Position < 0 && TrailStart > 0)
		{
			var profit = _entryPrice - candle.ClosePrice;
			if (profit >= TrailStart * _tickSize)
			{
				var newStop = candle.ClosePrice + TrailStep * _tickSize;
				if (_stopOrder != null && newStop < _stopOrder.Price - _tickSize)
				{
					CancelOrder(_stopOrder);
					_stopOrder = BuyStop(newStop, -Position);
				}
			}
		}

		_prevCandle = candle;
		_prevAtr = atr;
		_prevAdx = adx;
	}
}
