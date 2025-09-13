namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trades when Kaufman Adaptive Moving Average crosses Weighted Moving Average.
/// </summary>
public class KaufWmaCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _amaPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _wmaPeriod;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevKama;
	private decimal _prevWma;
	private bool _isFirst = true;

	public int AmaPeriod { get => _amaPeriod.Value; set => _amaPeriod.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int WmaPeriod { get => _wmaPeriod.Value; set => _wmaPeriod.Value = value; }
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public KaufWmaCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle type", "Type of candles", "General");

		_amaPeriod = Param(nameof(AmaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("AMA length", "Kaufman AMA period", "Indicators");

		_fastPeriod = Param(nameof(FastPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast smoothing period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow smoothing period", "Indicators");

		_wmaPeriod = Param(nameof(WmaPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("WMA length", "Weighted MA period", "Indicators");

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Open long", "Allow opening long position", "Signals");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Open short", "Allow opening short position", "Signals");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Close long", "Allow closing long on sell signal", "Signals");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Close short", "Allow closing short on buy signal", "Signals");
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

		_prevKama = default;
		_prevWma = default;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var kama = new KaufmanAdaptiveMovingAverage
		{
			Length = AmaPeriod,
			FastSCPeriod = FastPeriod,
			SlowSCPeriod = SlowPeriod
		};

		var wma = new WeightedMovingAverage { Length = WmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(kama, wma, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, kama);
			DrawIndicator(area, wma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal kamaValue, decimal wmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirst)
		{
			_prevKama = kamaValue;
			_prevWma = wmaValue;
			_isFirst = false;
			return;
		}

		var crossUp = _prevKama <= _prevWma && kamaValue > wmaValue;
		var crossDown = _prevKama >= _prevWma && kamaValue < wmaValue;

		if (crossUp)
		{
			if (SellClose && Position < 0)
				BuyMarket(Math.Abs(Position));

			if (BuyOpen && Position <= 0)
				BuyMarket(Volume);
		}
		else if (crossDown)
		{
			if (BuyClose && Position > 0)
				SellMarket(Position);

			if (SellOpen && Position >= 0)
				SellMarket(Volume);
		}

		_prevKama = kamaValue;
		_prevWma = wmaValue;
	}
}

