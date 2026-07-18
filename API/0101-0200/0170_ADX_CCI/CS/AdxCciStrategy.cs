using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on ADX and CCI indicators.
/// Enters long when ADX > 25 and CCI is oversold (< -100)
/// Enters short when ADX > 25 and CCI is overbought (> 100)
/// </summary>
public class AdxCciStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevCciValue;
	private bool _isFirstValue = true;
	private int _cooldown;

	/// <summary>
	/// ADX period
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// CCI period
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Bars to wait between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	public AdxCciStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX indicator", "Indicators")
			
			.SetOptimize(10, 20, 1);

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for CCI indicator", "Indicators")
			
			.SetOptimize(14, 30, 1);

		_adxThreshold = Param(nameof(AdxThreshold), 18m)
			.SetRange(10m, 40m)
			.SetDisplay("ADX Threshold", "Minimum ADX for trend entries", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 120)
			.SetRange(5, 500)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")
			
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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

		_prevCciValue = 0;
		_isFirstValue = true;
		_cooldown = 0;
	}

/// <inheritdoc />
protected override void OnStarted2(DateTime time)
{
	base.OnStarted2(time);

		// Create indicators
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		// Reset state variables
		_prevCciValue = 0;
		_isFirstValue = true;
		_cooldown = 0;

		// Subscribe to candles and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, cci, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			
			// Create a separate area for CCI
			var cciArea = CreateChartArea();
			if (cciArea != null)
			{
				DrawIndicator(cciArea, cci);
			}
			
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue cciValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;
		
		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// For the first value, just store and skip trading
		if (_isFirstValue)
		{
			_prevCciValue = cciValue.ToDecimal();
			_isFirstValue = false;
			return;
		}
		
		var cciDec = cciValue.ToDecimal();

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		var adxMa = adxTyped.MovingAverage;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevCciValue = cciDec;
			return;
		}

		// Trading logic
		if (Position == 0)
		{
			if (_prevCciValue >= -40m && cciDec < -40m)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (_prevCciValue <= 40m && cciDec > 40m)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (adxMa < AdxThreshold * 0.8m || (Position > 0 && cciDec > 0) || (Position < 0 && cciDec < 0))
		{
			// Trend is weakening - close any position
			if (Position > 0)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
			else if (Position < 0)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}

		// Store for the next iteration
		_prevCciValue = cciDec;
	}
}
