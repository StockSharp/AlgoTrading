using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Uses a simple two-factor matrix-style model based on fast/slow SMA values.
/// </summary>
public class FunctionMatrixLibraryStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _entryThresholdPercent;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _slowSma;
	private int _barsFromSignal;

	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Minimum percent edge required to open or reverse a position.
	/// </summary>
	public decimal EntryThresholdPercent
	{
		get => _entryThresholdPercent.Value;
		set => _entryThresholdPercent.Value = value;
	}

	/// <summary>
	/// Minimum bars between market entries.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public FunctionMatrixLibraryStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast SMA length", "General");

		_slowLength = Param(nameof(SlowLength), 48)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow SMA length", "General");

		_entryThresholdPercent = Param(nameof(EntryThresholdPercent), 0.25m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Threshold %", "Required model edge in percent", "General");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame())
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
		_fastSma = null;
		_slowSma = null;
		_barsFromSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(null, null);

		_fastSma = new() { Length = FastLength };
		_slowSma = new() { Length = SlowLength };
		_barsFromSignal = SignalCooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastSma, _slowSma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_fastSma.IsFormed || !_slowSma.IsFormed)
			return;

		var close = candle.ClosePrice;
		if (close <= 0m)
			return;

		_barsFromSignal++;
		if (_barsFromSignal < SignalCooldownBars)
			return;

		// Weighted linear combination emulates a compact matrix regression output.
		var modelPrice = (2m * fastValue + slowValue) / 3m;
		var edgePercent = (modelPrice - close) / close * 100m;

		if (edgePercent >= EntryThresholdPercent && Position <= 0)
		{
			BuyMarket();
			_barsFromSignal = 0;
		}
		else if (edgePercent <= -EntryThresholdPercent && Position >= 0)
		{
			SellMarket();
			_barsFromSignal = 0;
		}
	}
}
