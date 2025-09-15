using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RAVI Histogram trend strategy based on fast and slow EMA difference.
/// </summary>
public class RaviHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _downLevel;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRavi;
	private bool _isFirst = true;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal UpLevel { get => _upLevel.Value; set => _upLevel.Value = value; }
	public decimal DownLevel { get => _downLevel.Value; set => _downLevel.Value = value; }
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RaviHistogramStrategy()
	{
		_fastLength = Param(nameof(FastLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA length", "General");

		_slowLength = Param(nameof(SlowLength), 65)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA length", "General");

		_upLevel = Param(nameof(UpLevel), 0.1m)
			.SetDisplay("Upper Level", "Upper threshold for trend", "General");

		_downLevel = Param(nameof(DownLevel), -0.1m)
			.SetDisplay("Lower Level", "Lower threshold for trend", "General");

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Open Long", "Allow opening long positions", "Trading");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Open Short", "Allow opening short positions", "Trading");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Close Long", "Allow closing long positions", "Trading");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Close Short", "Allow closing short positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fast = new EMA { Length = FastLength };
		var slow = new EMA { Length = SlowLength };

		// Subscribe to candle data and bind indicators.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished || slow == 0)
			return;

		// Calculate RAVI value from EMA difference.
		var ravi = 100m * (fast - slow) / slow;

		if (_isFirst)
		{
			_prevRavi = ravi;
			_isFirst = false;
			return;
		}

		// Handle signals when RAVI crosses thresholds.
		if (ravi > UpLevel)
		{
			if (SellClose && Position < 0)
				BuyMarket();

			if (BuyOpen && _prevRavi <= UpLevel && Position <= 0)
				BuyMarket();
		}
		else if (ravi < DownLevel)
		{
			if (BuyClose && Position > 0)
				SellMarket();

			if (SellOpen && _prevRavi >= DownLevel && Position >= 0)
				SellMarket();
		}

		_prevRavi = ravi;
	}
}
