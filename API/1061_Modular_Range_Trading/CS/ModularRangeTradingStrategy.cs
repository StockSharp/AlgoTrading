using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified modular range strategy using RSI reversion with SMA context.
/// </summary>
public class ModularRangeTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _sma;
	private decimal _prevRsi;
	private bool _hasPrevRsi;
	private int _barsFromSignal;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// SMA period.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// RSI overbought threshold.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI oversold threshold.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Minimum bars between entries.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ModularRangeTradingStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "General");
		_smaPeriod = Param(nameof(SmaPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA period", "General");
		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought threshold", "General");
		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold threshold", "General");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
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
		_rsi = null;
		_sma = null;
		_prevRsi = 0m;
		_hasPrevRsi = false;
		_barsFromSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_sma = new SimpleMovingAverage { Length = SmaPeriod };
		_prevRsi = 0m;
		_hasPrevRsi = false;
		_barsFromSignal = SignalCooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_rsi, _sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_rsi.IsFormed || !_sma.IsFormed)
			return;

		if (!_hasPrevRsi)
		{
			_prevRsi = rsiValue;
			_hasPrevRsi = true;
			return;
		}

		_barsFromSignal++;
		var close = candle.ClosePrice;
		var longSignal = _prevRsi <= RsiOversold && rsiValue > RsiOversold && close < smaValue;
		var shortSignal = _prevRsi >= RsiOverbought && rsiValue < RsiOverbought && close > smaValue;

		if (_barsFromSignal >= SignalCooldownBars && longSignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_barsFromSignal = 0;
		}
		else if (_barsFromSignal >= SignalCooldownBars && shortSignal && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_barsFromSignal = 0;
		}
		else if (Position > 0 && (rsiValue >= 55m || close >= smaValue))
		{
			SellMarket();
		}
		else if (Position < 0 && (rsiValue <= 45m || close <= smaValue))
		{
			BuyMarket();
		}

		_prevRsi = rsiValue;
	}
}
