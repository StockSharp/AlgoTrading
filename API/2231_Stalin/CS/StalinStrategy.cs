using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the "Stalin" indicator.
/// Uses fast and slow EMAs with an optional RSI filter and price confirmation.
/// </summary>
public class StalinStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _confirm;
	private readonly StrategyParam<decimal> _flat;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastBuyPrice;
	private decimal _lastSellPrice;
	private decimal? _pendingBuyPrice;
	private decimal? _pendingSellPrice;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// RSI period. Set to 0 to disable.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Required price move to confirm the signal.
	/// </summary>
	public decimal Confirm
	{
		get => _confirm.Value;
		set => _confirm.Value = value;
	}

	/// <summary>
	/// Minimum distance from previous signal.
	/// </summary>
	public decimal Flat
	{
		get => _flat.Value;
		set => _flat.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public StalinStrategy()
	{
		_fastLength = Param(nameof(FastLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_slowLength = Param(nameof(SlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(20, 50, 1);

		_rsiLength = Param(nameof(RsiLength), 17)
			.SetDisplay("RSI Length", "RSI period (0 to disable)", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(0, 30, 1);

		_confirm = Param(nameof(Confirm), 0m)
			.SetDisplay("Confirm", "Price confirmation in points", "General")
			.SetCanOptimize(true)
			.SetOptimize(0m, 100m, 10m);

		_flat = Param(nameof(Flat), 0m)
			.SetDisplay("Flat", "Minimum distance between signals", "General")
			.SetCanOptimize(true)
			.SetOptimize(0m, 100m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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

		_lastBuyPrice = 0m;
		_lastSellPrice = 0m;
		_pendingBuyPrice = null;
		_pendingSellPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var fastMa = new EMA { Length = FastLength };
		var slowMa = new EMA { Length = SlowLength };
		var rsi = new RSI { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);

		var prevFast = 0m;
		var prevSlow = 0m;
		var initialized = false;

		subscription
			.Bind(fastMa, slowMa, rsi, (candle, fast, slow, rsiValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!initialized && fastMa.IsFormed && slowMa.IsFormed && (RsiLength == 0 || rsi.IsFormed))
				{
					prevFast = fast;
					prevSlow = slow;
					initialized = true;
					return;
				}

				if (!initialized)
					return;

				// Check for crossovers
				if (prevFast <= prevSlow && fast > slow && (RsiLength == 0 || rsiValue > 50m))
				{
					if (Confirm > 0)
						_pendingBuyPrice = candle.LowPrice;
					else
						TryBuy(candle.LowPrice);
				}
				else if (prevFast >= prevSlow && fast < slow && (RsiLength == 0 || rsiValue < 50m))
				{
					if (Confirm > 0)
						_pendingSellPrice = candle.HighPrice;
					else
						TrySell(candle.HighPrice);
				}

				// Confirm pending trades
				if (_pendingBuyPrice.HasValue && candle.HighPrice - _pendingBuyPrice.Value >= Confirm && candle.ClosePrice <= candle.HighPrice)
				{
					TryBuy(candle.LowPrice);
					_pendingBuyPrice = null;
				}

				if (_pendingSellPrice.HasValue && _pendingSellPrice.Value - candle.LowPrice >= Confirm && candle.OpenPrice >= candle.ClosePrice)
				{
					TrySell(candle.HighPrice);
					_pendingSellPrice = null;
				}

				prevFast = fast;
				prevSlow = slow;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			if (RsiLength > 0)
				DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void TryBuy(decimal price)
	{
		if (Flat > 0 && Math.Abs(price - _lastBuyPrice) < Flat)
			return;

		if (Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		_lastBuyPrice = price;
	}

	private void TrySell(decimal price)
	{
		if (Flat > 0 && Math.Abs(price - _lastSellPrice) < Flat)
			return;

		if (Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_lastSellPrice = price;
	}
}
