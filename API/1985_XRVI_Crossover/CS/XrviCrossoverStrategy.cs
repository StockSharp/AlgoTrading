using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XRVI crossover strategy.
/// Uses Relative Vigor Index with an additional moving average signal line.
/// Buys when XRVI crosses above its signal line and sells when XRVI crosses below.
/// </summary>
public class XrviCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _rviLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeVigorIndex _rvi;
	private SimpleMovingAverage _signal;

	private decimal? _prevRvi;
	private decimal? _prevSignal;

	/// <summary>
	/// Length of RVI.
	/// </summary>
	public int RviLength
	{
		get => _rviLength.Value;
		set => _rviLength.Value = value;
	}

	/// <summary>
	/// Length of signal line.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public XrviCrossoverStrategy()
	{
		_rviLength = Param(nameof(RviLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("RVI Length", "Length for XRVI", "General")
			.SetCanOptimize(true);

		_signalLength = Param(nameof(SignalLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Length for signal line", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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

		_rvi = default;
		_signal = default;
		_prevRvi = default;
		_prevSignal = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rvi = new RelativeVigorIndex { Length = RviLength };
		_signal = new SimpleMovingAverage { Length = SignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_rvi, ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rvi);
			DrawIndicator(area, _signal);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rviValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var signalValue = _signal.Process(rviValue);

		if (!rviValue.IsFinal || !signalValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rvi = rviValue.GetValue<decimal>();
		var signal = signalValue.GetValue<decimal>();

		var longSignal = _prevRvi <= _prevSignal && rvi > signal;
		var shortSignal = _prevRvi >= _prevSignal && rvi < signal;

		if (longSignal && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
		}
		else if (shortSignal && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		}

		_prevRvi = rvi;
		_prevSignal = signal;
	}
}
