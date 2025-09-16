using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Volume Weighted Moving Average (VWMA) slope reversals.
/// Opens or closes positions when the VWMA changes direction.
/// </summary>
public class VwapCloseStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	private VolumeWeightedMovingAverage _vwma;
	private decimal? _prev1;
	private decimal? _prev2;

	/// <summary>
	/// VWMA period.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enable long entries.
	/// </summary>
	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Enable short entries.
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
	/// Constructor.
	/// </summary>
	public VwapCloseStrategy()
	{
		_period = Param(nameof(Period), 2)
			.SetGreaterThanZero()
			.SetDisplay("Period", "VWMA calculation period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 5, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

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
	protected override void OnStarted(DateTimeOffset time)
	{
		_vwma = new VolumeWeightedMovingAverage { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_vwma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _vwma);
			DrawOwnTrades(area);
		}

		StartProtection();

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwmaValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Warm up stage
		if (_prev1 is null || _prev2 is null)
		{
			_prev2 = _prev1;
			_prev1 = vwmaValue;
			return;
		}

		var prev1 = _prev1.Value;
		var prev2 = _prev2.Value;

		// Close existing positions on slope reversal
		if (prev1 < prev2 && SellClose && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}
		else if (prev1 > prev2 && BuyClose && Position > 0)
		{
			SellMarket(Math.Abs(Position));
		}

		// Open long on valley
		if (prev1 < prev2 && vwmaValue > prev1 && BuyOpen && Position <= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		// Open short on peak
		else if (prev1 > prev2 && vwmaValue < prev1 && SellOpen && Position >= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prev2 = prev1;
		_prev1 = vwmaValue;
	}
}

