namespace StockSharp.Samples.Strategies;

using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// HLRSign based strategy.
/// Opens and closes positions based on HLR level crossings.
/// </summary>
public class ExpHlrSignStrategy : Strategy
{
	private enum AlgMethod
	{
		ModeIn,
		ModeOut,
	}

	private readonly StrategyParam<AlgMethod> _mode;
	private readonly StrategyParam<int> _range;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _dnLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	private decimal _previousHlr;
	private bool _isFirst = true;

	/// <summary>
	/// Indicator mode.
	/// </summary>
	public AlgMethod Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Lookback range for highest and lowest values.
	/// </summary>
	public int Range
	{
		get => _range.Value;
		set => _range.Value = value;
	}

	/// <summary>
	/// Upper level in percent for HLR crossing.
	/// </summary>
	public decimal UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	/// <summary>
	/// Lower level in percent for HLR crossing.
	/// </summary>
	public decimal DnLevel
	{
		get => _dnLevel.Value;
		set => _dnLevel.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Allow closing long positions on sell signal.
	/// </summary>
	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on buy signal.
	/// </summary>
	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpHlrSignStrategy"/>.
	/// </summary>
	public ExpHlrSignStrategy()
	{
		_mode = Param(nameof(Mode), AlgMethod.ModeOut)
			.SetDisplay("Mode", "Indicator operation mode", "General");

		_range = Param(nameof(Range), 40)
			.SetDisplay("Range", "Lookback period for HLR", "Indicator")
			.SetOptimize(20, 80, 10)
			.SetCanOptimize(true);

		_upLevel = Param(nameof(UpLevel), 80m)
			.SetDisplay("Up Level", "Upper level for HLR", "Indicator")
			.SetOptimize(60m, 90m, 5m)
			.SetCanOptimize(true);

		_dnLevel = Param(nameof(DnLevel), 20m)
			.SetDisplay("Down Level", "Lower level for HLR", "Indicator")
			.SetOptimize(10m, 40m, 5m)
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Trading");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Trading");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Buy Close", "Allow closing long positions", "Trading");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Sell Close", "Allow closing short positions", "Trading");
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
		_previousHlr = 0;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var donchian = new DonchianChannels { Length = Range };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var donchian = (DonchianChannelsValue)value;
		var upper = donchian.UpperBand;
		var lower = donchian.LowerBand;

		if (upper == null || lower == null)
			return;

		var mid = (candle.HighPrice + candle.LowPrice) / 2m;
		var range = (decimal)(upper - lower);
		var hlr = range != 0m ? 100m * (mid - lower.Value) / range : 0m;

		bool buySignal = false;
		bool sellSignal = false;

		if (_isFirst)
		{
			_previousHlr = hlr;
			_isFirst = false;
			return;
		}

		if (Mode == AlgMethod.ModeIn)
		{
			if (hlr > UpLevel && _previousHlr <= UpLevel)
				buySignal = true;
			if (hlr < DnLevel && _previousHlr >= DnLevel)
				sellSignal = true;
		}
		else
		{
			if (hlr < UpLevel && _previousHlr >= UpLevel)
				sellSignal = true;
			if (hlr > DnLevel && _previousHlr <= DnLevel)
				buySignal = true;
		}

		if (buySignal)
		{
			if (SellClose && Position < 0)
				BuyMarket(Math.Abs(Position));
			if (BuyOpen && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}

		if (sellSignal)
		{
			if (BuyClose && Position > 0)
				SellMarket(Position);
			if (SellOpen && Position >= 0)
				SellMarket(Volume + Position);
		}

		_previousHlr = hlr;
	}
}

