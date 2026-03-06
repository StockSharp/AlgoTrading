using System;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum strategy based on aggregated volume delta windows.
/// </summary>
public class MocDeltaMooEntryV2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _deltaWindow;
	private readonly StrategyParam<decimal> _deltaThresholdPercent;
	private readonly StrategyParam<int> _signalCooldownBars;

	private decimal _windowBuyVolume;
	private decimal _windowSellVolume;
	private int _windowBarCount;
	private int _barsFromSignal;

	/// <summary>
	/// Candle timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of bars per delta window.
	/// </summary>
	public int DeltaWindow
	{
		get => _deltaWindow.Value;
		set => _deltaWindow.Value = value;
	}

	/// <summary>
	/// Absolute delta percent needed to trigger an entry.
	/// </summary>
	public decimal DeltaThresholdPercent
	{
		get => _deltaThresholdPercent.Value;
		set => _deltaThresholdPercent.Value = value;
	}

	/// <summary>
	/// Minimum bars between entries.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	public MocDeltaMooEntryV2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
		_deltaWindow = Param(nameof(DeltaWindow), 24)
			.SetGreaterThanZero()
			.SetDisplay("Delta Window", "Bars per delta calculation window", "General");
		_deltaThresholdPercent = Param(nameof(DeltaThresholdPercent), 12m)
			.SetGreaterThanZero()
			.SetDisplay("Delta Threshold %", "Minimum delta percent for momentum", "General");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 16)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_windowBuyVolume = 0m;
		_windowSellVolume = 0m;
		_windowBarCount = 0;
		_barsFromSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_windowBuyVolume = 0m;
		_windowSellVolume = 0m;
		_windowBarCount = 0;
		_barsFromSignal = SignalCooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.ClosePrice > candle.OpenPrice)
			_windowBuyVolume += candle.TotalVolume;
		else if (candle.ClosePrice < candle.OpenPrice)
			_windowSellVolume += candle.TotalVolume;
		else
		{
			_windowBuyVolume += candle.TotalVolume * 0.5m;
			_windowSellVolume += candle.TotalVolume * 0.5m;
		}

		_windowBarCount++;
		_barsFromSignal++;

		if (_windowBarCount < DeltaWindow)
			return;

		var totalVolume = _windowBuyVolume + _windowSellVolume;
		var deltaPercent = totalVolume > 0m
			? (_windowBuyVolume - _windowSellVolume) / totalVolume * 100m
			: 0m;

		var momentumSignal = 0;
		if (deltaPercent > DeltaThresholdPercent)
			momentumSignal = 1;
		else if (deltaPercent < -DeltaThresholdPercent)
			momentumSignal = -1;

		if (_barsFromSignal >= SignalCooldownBars && momentumSignal != 0)
		{
			if (momentumSignal > 0 && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				_barsFromSignal = 0;
			}
			else if (momentumSignal < 0 && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				_barsFromSignal = 0;
			}
		}

		_windowBuyVolume = 0m;
		_windowSellVolume = 0m;
		_windowBarCount = 0;
	}
}
