using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on UltraFATL indicator signals.
/// It opens long or short positions when the indicator changes its state
/// around the threshold between levels 4 and 5.
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
	/// Initial smoothing period for UltraFATL.
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

		_length = Param(nameof(Length), 3)
			.SetDisplay("Length", "Initial smoothing period", "UltraFATL")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

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
		_prevValue = default;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var ultraFatl = new UltraFatl
		{
			Length = Length
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ultraFatl, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ultraFatlValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var current = ultraFatlValue;

		if (!_isInitialized)
		{
			_prevValue = current;
			_isInitialized = true;
			return;
		}

		var previous = _prevValue;
		_prevValue = current;

		var isBuySignal = previous > 4m && current < 5m && current != 0m;
		var isSellSignal = previous < 5m && previous != 0m && current > 4m;

		if (isBuySignal && Position <= 0)
			BuyMarket();
		else if (isSellSignal && Position >= 0)
			SellMarket();
	}
}
