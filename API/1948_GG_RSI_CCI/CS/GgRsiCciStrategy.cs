namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on the GG-RSI-CCI indicator.
/// </summary>
public class GgRsiCciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<SignalMode> _mode;

	private SimpleMovingAverage _rsiFast = null!;
	private SimpleMovingAverage _rsiSlow = null!;
	private SimpleMovingAverage _cciFast = null!;
	private SimpleMovingAverage _cciSlow = null!;
	private int _prevSignal;

	/// <summary>
	/// Defines how positions are closed.
	/// </summary>
	public enum SignalMode
	{
		/// <summary>Position is closed only on opposite signal.</summary>
		Trend,
		/// <summary>Position is closed on any neutral signal.</summary>
		Flat,
	}

	public GgRsiCciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for indicator calculation.", "General");

		_length = Param(nameof(Length), 8)
			.SetGreaterThanZero()
			.SetDisplay("Length", "RSI and CCI period.", "Indicators");

		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast smoothing period.", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow smoothing period.", "Indicators");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume.", "General");

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
			.SetDisplay("Allow Buy", "Permit opening long positions.", "Permissions");

		_allowSellOpen = Param(nameof(AllowSellOpen), true)
			.SetDisplay("Allow Sell", "Permit opening short positions.", "Permissions");

		_allowBuyClose = Param(nameof(AllowBuyClose), true)
			.SetDisplay("Close Short", "Permit closing short positions.", "Permissions");

		_allowSellClose = Param(nameof(AllowSellClose), true)
			.SetDisplay("Close Long", "Permit closing long positions.", "Permissions");

		_mode = Param(nameof(Mode), SignalMode.Flat)
			.SetDisplay("Mode", "Closing style.", "Trading");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	public bool AllowBuyOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	public bool AllowSellOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	public bool AllowBuyClose
	{
		get => _allowBuyClose.Value;
		set => _allowBuyClose.Value = value;
	}

	public bool AllowSellClose
	{
		get => _allowSellClose.Value;
		set => _allowSellClose.Value = value;
	}

	public SignalMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var rsi = new RelativeStrengthIndex { Length = Length };
		var cci = new CommodityChannelIndex { Length = Length };

		_rsiFast = new SimpleMovingAverage { Length = FastPeriod };
		_rsiSlow = new SimpleMovingAverage { Length = SlowPeriod };
		_cciFast = new SimpleMovingAverage { Length = FastPeriod };
		_cciSlow = new SimpleMovingAverage { Length = SlowPeriod };

		_prevSignal = -1;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, cci, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rsiFast = _rsiFast.Process(rsiValue, candle.OpenTime, true).ToDecimal();
		var rsiSlow = _rsiSlow.Process(rsiValue, candle.OpenTime, true).ToDecimal();
		var cciFast = _cciFast.Process(cciValue, candle.OpenTime, true).ToDecimal();
		var cciSlow = _cciSlow.Process(cciValue, candle.OpenTime, true).ToDecimal();

		int signal;
		if (rsiFast > rsiSlow && cciFast > cciSlow)
			signal = 2;
		else if (rsiFast < rsiSlow && cciFast < cciSlow)
			signal = 0;
		else
			signal = 1;

		if (signal == 2)
		{
			if (AllowSellClose && Position < 0)
				BuyMarket(Math.Abs(Position));

			if (AllowBuyOpen && Position <= 0 && _prevSignal != 2)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (signal == 0)
		{
			if (AllowBuyClose && Position > 0)
				SellMarket(Position);

			if (AllowSellOpen && Position >= 0 && _prevSignal != 0)
				SellMarket(Volume + Math.Abs(Position));
		}
		else if (Mode == SignalMode.Flat)
		{
			if (AllowBuyClose && Position > 0)
				SellMarket(Position);
			if (AllowSellClose && Position < 0)
				BuyMarket(Math.Abs(Position));
		}

		_prevSignal = signal;
	}
}
