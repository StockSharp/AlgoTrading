using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that closes positions when price crosses a trailing line based on H4 candles.
/// It does not open new positions and can be used to protect manually opened trades.
/// </summary>
public class TralingLineStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _buyStop;
	private decimal _sellStop;

	/// <summary>
	/// Offset in price steps for the trailing line.
	/// </summary>
	public decimal StopLevel
	{
		get => _stopLevel.Value;
		set => _stopLevel.Value = value;
	}

	/// <summary>
	/// Candle type used to calculate trailing levels.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public TralingLineStrategy()
	{
		_stopLevel = Param(nameof(StopLevel), 70m)
			.SetDisplay("Stop Level", "Offset in price steps from high/low", "Protection")
			.SetCanOptimize(true)
			.SetOptimize(20m, 200m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for trailing", "General");
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

		_buyStop = 0m;
		_sellStop = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var priceStep = Security?.PriceStep ?? 1m;
		var offset = StopLevel * priceStep;

		// Manage long positions
		if (Position > 0)
		{
			var stop = candle.LowPrice - offset;
			if (_buyStop == 0m || stop > _buyStop)
				_buyStop = stop;

			if (candle.ClosePrice <= _buyStop)
			{
				SellMarket(Position);
				_buyStop = 0m;
			}
		}
		// Manage short positions
		else if (Position < 0)
		{
			var stop = candle.HighPrice + offset;
			if (_sellStop == 0m || stop < _sellStop)
				_sellStop = stop;

			if (candle.ClosePrice >= _sellStop)
			{
				BuyMarket(-Position);
				_sellStop = 0m;
			}
		}
	}
}
