using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that detects candles failing to touch EMA and enters short on breakdown.
/// </summary>
public class Ema5AlertCandleShortStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prev1Touch;
	private bool _prev2Touch;
	private bool _prev3Touch;
	private decimal _validAlertLow;
	private decimal _validAlertHigh;
	private bool _isAlertActive;

	/// <summary>
	/// EMA period parameter.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Risk per trade parameter.
	/// </summary>
	public decimal RiskPerTrade
	{
		get => _riskPerTrade.Value;
		set => _riskPerTrade.Value = value;
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
	public Ema5AlertCandleShortStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_riskPerTrade = Param(nameof(RiskPerTrade), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Per Trade", "Risk amount per trade", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

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

		_prev1Touch = false;
		_prev2Touch = false;
		_prev3Touch = false;
		_isAlertActive = false;
		_validAlertLow = 0;
		_validAlertHigh = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var touches = candle.LowPrice <= emaValue;
		var alertCandle = !touches && _prev1Touch && _prev2Touch && _prev3Touch;

		if (alertCandle)
		{
			_validAlertLow = candle.LowPrice;
			_validAlertHigh = candle.HighPrice;
			_isAlertActive = true;
		}

		if (_isAlertActive)
		{
			var shortTrigger = candle.LowPrice < _validAlertLow;
			if (shortTrigger && Position >= 0)
			{
				var range = _validAlertHigh - _validAlertLow;
				if (range > 0)
				{
					var positionSize = RiskPerTrade / range;
					CancelActiveOrders();
					SellMarket(positionSize);
					BuyLimit(positionSize, _validAlertLow - range);
					BuyStop(positionSize, _validAlertHigh);
				}

				_isAlertActive = false;
			}
			else if (!shortTrigger && !touches)
			{
				_validAlertLow = candle.LowPrice;
				_validAlertHigh = candle.HighPrice;
			}
		}

		_prev3Touch = _prev2Touch;
		_prev2Touch = _prev1Touch;
		_prev1Touch = touches;
	}
}
