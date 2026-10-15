using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AO/AC trading zones strategy.
/// Uses Awesome Oscillator cross-over of zero line combined with EMA trend filter.
/// </summary>
public class AoAcTradingZonesStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevAo;
	private int _barIndex;
	private int _lastTradeBar;

	/// <summary>
	/// EMA period for trend filter.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle series type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AoAcTradingZonesStrategy"/> class.
	/// </summary>
	public AoAcTradingZonesStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 50)
			.SetDisplay("EMA Length", "EMA trend filter period", "Settings");

		_cooldownBars = Param(nameof(CooldownBars), 350)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Trading");

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
		_prevAo = 0;
		_barIndex = 0;
		_lastTradeBar = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var ao = new AwesomeOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ao, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal aoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var cooldownOk = _barIndex - _lastTradeBar > CooldownBars;

		// AO crosses above zero + uptrend
		var aoCrossUp = _prevAo <= 0 && aoValue > 0;
		// AO crosses below zero + downtrend
		var aoCrossDown = _prevAo >= 0 && aoValue < 0;

		if (aoCrossUp && candle.ClosePrice > emaValue && Position <= 0 && cooldownOk)
		{
			BuyMarket();
			_lastTradeBar = _barIndex;
		}
		else if (aoCrossDown && candle.ClosePrice < emaValue && Position >= 0 && cooldownOk)
		{
			SellMarket();
			_lastTradeBar = _barIndex;
		}

		_prevAo = aoValue;
	}
}
