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

using StockSharp.Algo;
using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on VWAP and ADX indicators.
/// Enters long when price is above VWAP and ADX > 25.
/// Enters short when price is below VWAP and ADX > 25.
/// Exits when ADX < 20.
/// </summary>
public class VwapAdxStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx;
	private VolumeWeightedMovingAverage _vwap;
	private decimal _prevAdxValue;
	private int _cooldown;

	/// <summary>
	/// Stop loss percentage value.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// ADX indicator period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
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
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VwapAdxStrategy"/>.
	/// </summary>
	public VwapAdxStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop loss (%)", "Stop loss percentage from entry price", "Risk Management");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Period for Average Directional Movement Index", "Indicators")
			
			.SetOptimize(10, 20, 1);

		_cooldownBars = Param(nameof(CooldownBars), 25)
			.SetRange(1, 200)
			.SetDisplay("Cooldown Bars", "Bars between entries", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe of data for strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevAdxValue = default;
		_cooldown = 0;
		_adx = null;
		_vwap = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create ADX indicator
		_adx = new() { Length = AdxPeriod };
		_vwap = new() { Length = AdxPeriod };
		var dummyEma = new ExponentialMovingAverage { Length = 10 };

		// Create subscription and subscribe to VWAP
		var subscription = SubscribeCandles(CandleType);

		// Process candles with ADX + dummy EMA (BindEx needs 2+ indicators)
		subscription
			.BindEx(_adx, dummyEma, ProcessCandle)
			.Start();

		// Setup chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue dummyValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		decimal vwap;
		try
		{
			var vwapValue = _vwap.Process(candle);
			if (vwapValue == null || !_vwap.IsFormed)
				return;

			vwap = vwapValue.ToDecimal();
		}
		catch (IndexOutOfRangeException)
		{
			return;
		}

		// Get current ADX value
		var typedAdx = (AverageDirectionalIndexValue)adxValue;

		if (typedAdx.MovingAverage is not decimal currentAdxValue)
			return;

		// Trading logic
		if (_cooldown > 0)
		{
			_cooldown--;
		}

		var adxImpulseUp = _prevAdxValue <= 25 && currentAdxValue > 25;

		if (_cooldown == 0 && adxImpulseUp)
		{
			if (candle.ClosePrice > vwap * 1.001m && Position <= 0)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (candle.ClosePrice < vwap * 0.999m && Position >= 0)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (currentAdxValue < 18 && Position != 0)
		{
			if (Position > 0)
				SellMarket();
			else
				BuyMarket();
			_cooldown = CooldownBars;
		}

		// Store current ADX value for next candle
		_prevAdxValue = currentAdxValue;
	}
}
