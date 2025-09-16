using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Lossless Moving Average strategy.
/// Trades fast and slow SMA crossovers with optional break-even protection.
/// </summary>
public class LosslessMaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _maxDeals;
	private readonly StrategyParam<bool> _closeLosses;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private int _orderType; // 1 = buy, -1 = sell, 0 = none

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Maximum number of simultaneous deals.
	/// </summary>
	public int MaxDeals { get => _maxDeals.Value; set => _maxDeals.Value = value; }

	/// <summary>
	/// Close trades even if loss.
	/// </summary>
	public bool CloseLosses { get => _closeLosses.Value; set => _closeLosses.Value = value; }

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public LosslessMaStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast SMA length", "Parameters");

		_slowLength = Param(nameof(SlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow SMA length", "Parameters");

		_maxDeals = Param(nameof(MaxDeals), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Deals", "Maximum open deals", "Risk");

		_closeLosses = Param(nameof(CloseLosses), true)
			.SetDisplay("Close Losses", "Close losing trades immediately", "Risk");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for strategy", "General");
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
		_entryPrice = 0m;
		_orderType = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastMa = new SimpleMovingAverage { Length = FastLength };
		var slowMa = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var deals = Volume == 0 ? 0 : (int)(Math.Abs(Position) / Volume);

		// Closing logic
		if (Position > 0 && fastValue < slowValue)
		{
			if (CloseLosses || candle.ClosePrice > _entryPrice)
			{
				SellMarket(Math.Abs(Position));
				_orderType = -1;
			}
			else
			{
				SellLimit(_entryPrice, Math.Abs(Position));
			}
		}
		else if (Position < 0 && fastValue > slowValue)
		{
			if (CloseLosses || candle.ClosePrice < _entryPrice)
			{
				BuyMarket(Math.Abs(Position));
				_orderType = 1;
			}
			else
			{
				BuyLimit(_entryPrice, Math.Abs(Position));
			}
		}

		// Opening logic
		if (deals == 0 || (!CloseLosses && deals < MaxDeals))
		{
			if (fastValue > slowValue && _orderType != 1)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_orderType = 1;
			}
			else if (fastValue < slowValue && _orderType != -1)
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_orderType = -1;
			}
		}
	}
}
