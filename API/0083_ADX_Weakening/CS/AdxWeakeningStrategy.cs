using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX Weakening strategy.
/// Enters when ADX is decreasing (trend weakening) using price vs SMA for direction.
/// ADX weakening + price above SMA = buy (reversal up expected).
/// ADX weakening + price below SMA = sell (reversal down expected).
/// Exits on SMA cross.
/// </summary>
public class AdxWeakeningStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevAdx;
	private int _cooldown;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// MA Period.
	/// </summary>
	public int MAPeriod
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
	/// Cooldown bars.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AdxWeakeningStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetRange(7, 28)
			.SetDisplay("ADX Period", "Period for ADX", "Indicators");

		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for SMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_prevAdx = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAdx = 0;
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MAPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaIv, IIndicatorValue adxIv)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!adxIv.IsFormed || !smaIv.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var smaValue = smaIv.ToDecimal();
		var adxTyped = (AverageDirectionalIndexValue)adxIv;

		if (adxTyped.MovingAverage is not decimal adxValue)
			return;

		if (_prevAdx == 0)
		{
			_prevAdx = adxValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevAdx = adxValue;
			return;
		}

		var isWeakening = adxValue < _prevAdx;

		if (Position == 0 && isWeakening && candle.ClosePrice > smaValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && isWeakening && candle.ClosePrice < smaValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && candle.ClosePrice < smaValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice > smaValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevAdx = adxValue;
	}
}
