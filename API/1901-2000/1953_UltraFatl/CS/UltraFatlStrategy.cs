using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on UltraFATL-style threshold signals.
/// </summary>
public class UltraFatlStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _signalBar;

	private decimal _prevValue;
	private bool _isInitialized;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Smoothing period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Bar index used for signal calculation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Initialize the UltraFATL strategy.
	/// </summary>
	public UltraFatlStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_length = Param(nameof(Length), 8)
			.SetDisplay("Length", "Smoothing period", "UltraFATL")
			.SetOptimize(4, 20, 1);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Bar index for signal calculation", "UltraFATL");
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
		_prevValue = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = Length };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Map RSI to the 0..8 discrete UltraFATL-style scale.
		var current = Math.Max(0m, Math.Min(8m, rsiValue / 12.5m));

		if (!_isInitialized)
		{
			_prevValue = current;
			_isInitialized = true;
			return;
		}

		var previous = _prevValue;
		_prevValue = current;

		var isBuySignal = previous >= 5m && current < 5m && current > 0m;
		var isSellSignal = previous <= 4m && current > 4m;

		if (isBuySignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (isSellSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
