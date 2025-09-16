using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RVI histogram reversal strategy.
/// Opens long positions when the RVI leaves an overbought area or crosses below its signal line.
/// Opens short positions when the RVI leaves an oversold area or crosses above its signal line.
/// </summary>
public class RviHistogramReversalStrategy : Strategy
{
	public enum TrendMode
	{
		Levels,
		Cross
	}

	private readonly StrategyParam<int> _rviPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<TrendMode> _mode;
	private readonly StrategyParam<bool> _enableBuyOpen;
	private readonly StrategyParam<bool> _enableSellOpen;
	private readonly StrategyParam<bool> _enableBuyClose;
	private readonly StrategyParam<bool> _enableSellClose;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeVigorIndex _rvi;
	private SimpleMovingAverage _signal;

	private decimal? _prevRvi;
	private decimal? _prevSignal;

	/// <summary>
	/// RVI calculation period.
	/// </summary>
	public int RviPeriod { get => _rviPeriod.Value; set => _rviPeriod.Value = value; }

	/// <summary>
	/// Upper RVI threshold.
	/// </summary>
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }

	/// <summary>
	/// Lower RVI threshold.
	/// </summary>
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }

	/// <summary>
	/// Signal generation mode.
	/// </summary>
	public TrendMode Mode { get => _mode.Value; set => _mode.Value = value; }

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableBuyOpen { get => _enableBuyOpen.Value; set => _enableBuyOpen.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableSellOpen { get => _enableSellOpen.Value; set => _enableSellOpen.Value = value; }

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool EnableBuyClose { get => _enableBuyClose.Value; set => _enableBuyClose.Value = value; }

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool EnableSellClose { get => _enableSellClose.Value; set => _enableSellClose.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="RviHistogramReversalStrategy"/>.
	/// </summary>
	public RviHistogramReversalStrategy()
	{
		_rviPeriod = Param(nameof(RviPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RVI Period", "Period of RVI indicator", "General")
		.SetCanOptimize(true);

		_highLevel = Param(nameof(HighLevel), 0.3m)
		.SetDisplay("High Level", "Upper RVI threshold", "General")
		.SetCanOptimize(true);

		_lowLevel = Param(nameof(LowLevel), -0.3m)
		.SetDisplay("Low Level", "Lower RVI threshold", "General")
		.SetCanOptimize(true);

		_mode = Param(nameof(Mode), TrendMode.Cross)
		.SetDisplay("Mode", "Signal generation mode", "General");

		_enableBuyOpen = Param(nameof(EnableBuyOpen), true)
		.SetDisplay("Enable Buy Open", "Allow opening long positions", "Trading");

		_enableSellOpen = Param(nameof(EnableSellOpen), true)
		.SetDisplay("Enable Sell Open", "Allow opening short positions", "Trading");

		_enableBuyClose = Param(nameof(EnableBuyClose), true)
		.SetDisplay("Enable Buy Close", "Allow closing long positions", "Trading");

		_enableSellClose = Param(nameof(EnableSellClose), true)
		.SetDisplay("Enable Sell Close", "Allow closing short positions", "Trading");

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

		_rvi = new RelativeVigorIndex { Length = RviPeriod };
		_signal = new SimpleMovingAverage { Length = RviPeriod };

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

		bool buyOpen = false;
		bool sellOpen = false;
		bool buyClose = false;
		bool sellClose = false;

		if (Mode == TrendMode.Levels)
		{
			if (_prevRvi.HasValue)
			{
				if (_prevRvi > HighLevel && rvi <= HighLevel)
				{
					buyOpen = true;
					sellClose = true;
				}
				else if (_prevRvi < LowLevel && rvi >= LowLevel)
				{
					sellOpen = true;
					buyClose = true;
				}
			}
		}
		else
		{
			if (_prevRvi.HasValue && _prevSignal.HasValue)
			{
				if (_prevRvi > _prevSignal && rvi <= signal)
				{
					buyOpen = true;
					sellClose = true;
				}
				else if (_prevRvi < _prevSignal && rvi >= signal)
				{
					sellOpen = true;
					buyClose = true;
				}
			}
		}

		if (buyClose && EnableBuyClose && Position > 0)
		SellMarket(Position);

		if (sellClose && EnableSellClose && Position < 0)
		BuyMarket(-Position);

		if (buyOpen && EnableBuyOpen && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
		}
		else if (sellOpen && EnableSellOpen && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		}

		_prevRvi = rvi;
		_prevSignal = signal;
	}
}
