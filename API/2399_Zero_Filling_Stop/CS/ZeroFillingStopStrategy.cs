namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Moves stop loss to breakeven after reaching specified profit in points.
/// </summary>
public class ZeroFillingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _zeroFillingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private bool _stopMoved;

	/// <summary>
	/// Profit in points required to move stop to entry price.
	/// </summary>
	public decimal ZeroFillingStop
	{
		get => _zeroFillingStop.Value;
		set => _zeroFillingStop.Value = value;
	}

	/// <summary>
	/// Candle data type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ZeroFillingStopStrategy()
	{
		_zeroFillingStop = Param(nameof(ZeroFillingStop), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Zero Filling Stop", "Profit in points to move stop to breakeven", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_stopPrice = 0m;
		_stopMoved = false;
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

		if (Position > 0)
		{
			var profitPoints = (candle.ClosePrice - PositionPrice) / priceStep;

			if (!_stopMoved && profitPoints >= ZeroFillingStop)
			{
				_stopPrice = PositionPrice;
				_stopMoved = true;
			}

			if (_stopMoved && candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				_stopMoved = false;
			}
		}
		else if (Position < 0)
		{
			var profitPoints = (PositionPrice - candle.ClosePrice) / priceStep;

			if (!_stopMoved && profitPoints >= ZeroFillingStop)
			{
				_stopPrice = PositionPrice;
				_stopMoved = true;
			}

			if (_stopMoved && candle.HighPrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_stopMoved = false;
			}
		}
		else
		{
			_stopMoved = false;
		}
	}
}
