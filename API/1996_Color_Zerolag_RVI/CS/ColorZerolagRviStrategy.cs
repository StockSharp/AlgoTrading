using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on crossing of RVI and its signal line.
/// When the RVI main line crosses below the signal line, the strategy
/// closes short positions and opens a long one if allowed.
/// When the RVI main line crosses above the signal line, the strategy
/// closes long positions and opens a short one if allowed.
/// </summary>
public class ColorZerolagRviStrategy : Strategy
{
	private readonly StrategyParam<int> _rviLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeVigorIndex _rvi;
	private SimpleMovingAverage _signal;

	private decimal? _prevRvi;
	private decimal? _prevSignal;

	/// <summary>
	/// Length for RVI calculation.
	/// </summary>
	public int RviLength
	{
		get => _rviLength.Value;
		set => _rviLength.Value = value;
	}

	/// <summary>
	/// Length for RVI signal line.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorZerolagRviStrategy"/>.
	/// </summary>
	public ColorZerolagRviStrategy()
	{
		_rviLength = Param(nameof(RviLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RVI Length", "Length for RVI calculation", "Indicator")
			.SetCanOptimize(true);

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Length for RVI signal line", "Indicator")
			.SetCanOptimize(true);

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Trading");
		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Trading");
		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Buy Close", "Allow closing long positions", "Trading");
		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Sell Close", "Allow closing short positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		subscription.WhenNew(ProcessCandle).Start();

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

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rviValue = _rvi.Process(candle);
		var signalValue = _signal.Process(rviValue);

		if (!rviValue.IsFinal || !signalValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rvi = rviValue.ToDecimal();
		var signal = signalValue.ToDecimal();

		if (_prevRvi is null || _prevSignal is null)
		{
			_prevRvi = rvi;
			_prevSignal = signal;
			return;
		}

		var crossDown = _prevRvi > _prevSignal && rvi < signal;
		var crossUp = _prevRvi < _prevSignal && rvi > signal;

		if (crossDown)
		{
			var volume = 0m;
			if (SellClose && Position < 0)
				volume += -Position;
			if (BuyOpen && Position <= 0)
				volume += Volume;
			if (volume > 0m)
				BuyMarket(volume);
		}
		else if (crossUp)
		{
			var volume = 0m;
			if (BuyClose && Position > 0)
				volume += Position;
			if (SellOpen && Position >= 0)
				volume += Volume;
			if (volume > 0m)
				SellMarket(volume);
		}

		_prevRvi = rvi;
		_prevSignal = signal;
	}
}
