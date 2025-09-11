using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Chande Momentum Oscillator strategy with EMA signal and cooldown.
/// </summary>
public class ChandeMomentumOscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _cmoLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _overboughtThreshold;
	private readonly StrategyParam<decimal> _oversoldThreshold;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<DataType> _candleType;

	private int _buyCounter;
	private int _sellCounter;
	private decimal? _prevHist;

	/// <summary>
	/// CMO period length.
	/// </summary>
	public int CmoLength
	{
		get => _cmoLength.Value;
		set => _cmoLength.Value = value;
	}

	/// <summary>
	/// EMA signal period length.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Number of bars to wait before a new signal.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Threshold for overbought.
	/// </summary>
	public decimal OverboughtThreshold
	{
		get => _overboughtThreshold.Value;
		set => _overboughtThreshold.Value = value;
	}

	/// <summary>
	/// Threshold for oversold.
	/// </summary>
	public decimal OversoldThreshold
	{
		get => _oversoldThreshold.Value;
		set => _oversoldThreshold.Value = value;
	}

	/// <summary>
	/// Enable long entries.
	/// </summary>
	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}

	/// <summary>
	/// Enable short entries.
	/// </summary>
	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
	}

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ChandeMomentumOscillatorStrategy()
	{
		_cmoLength = Param(nameof(CmoLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("CMO Length", "Period for CMO", "CMO");
_signalLength = Param(nameof(SignalLength), 9)
.SetGreaterThanZero()
.SetDisplay("Signal Length", "Period for EMA signal", "CMO");
_cooldownBars = Param(nameof(CooldownBars), 5)
.SetGreaterThanZero()
.SetDisplay("Cooldown Bars", "Bars to wait before new signal", "Strategy");
_overboughtThreshold = Param(nameof(OverboughtThreshold), 40m)
.SetDisplay("Overbought Threshold", "Sell signal threshold", "Strategy")
.SetCanOptimize(true)
.SetOptimize(30m, 60m, 10m);
_oversoldThreshold = Param(nameof(OversoldThreshold), -40m)
.SetDisplay("Oversold Threshold", "Buy signal threshold", "Strategy")
.SetCanOptimize(true)
.SetOptimize(-60m, -20m, 10m);
_allowLong = Param(nameof(AllowLong), true)
.SetDisplay("Allow Long", "Enable long trades", "Strategy");
_allowShort = Param(nameof(AllowShort), true)
.SetDisplay("Allow Short", "Enable short trades", "Strategy");
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

		_buyCounter = CooldownBars;
		_sellCounter = CooldownBars;
		_prevHist = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var cmo = new ChandeMomentumOscillator { Length = CmoLength };
		var signal = new ExponentialMovingAverage { Length = SignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cmo, signal, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cmoValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hist = cmoValue - signalValue;
		var prevHist = _prevHist;

		var histDownPositive = prevHist is decimal ph && hist < ph && hist > 0m;
		var histUpNegative = prevHist is decimal ph2 && hist > ph2 && hist <= 0m;

		_prevHist = hist;

		if (AllowShort && histDownPositive && signalValue > OverboughtThreshold)
		{
			if (_sellCounter >= CooldownBars)
			{
				if (Position >= 0)
					SellMarket();

				_sellCounter = 0;
				_buyCounter = CooldownBars;
			}
			else
			{
				_sellCounter++;
			}
		}
		else if (AllowLong && histUpNegative && signalValue < OversoldThreshold)
		{
			if (_buyCounter >= CooldownBars)
			{
				if (Position <= 0)
					BuyMarket();

				_buyCounter = 0;
				_sellCounter = CooldownBars;
			}
			else
			{
				_buyCounter++;
			}
		}

		if (Position > 0 && signalValue > OverboughtThreshold)
		{
			SellMarket(Position);
			_buyCounter = CooldownBars;
		}
		else if (Position < 0 && signalValue < OversoldThreshold)
		{
			BuyMarket(-Position);
			_sellCounter = CooldownBars;
		}

		if (signalValue < OverboughtThreshold)
			_sellCounter = CooldownBars;

		if (signalValue > OversoldThreshold)
			_buyCounter = CooldownBars;
	}
}
