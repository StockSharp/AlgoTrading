namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that moves stop loss to break-even after reaching a profit in pips.
/// </summary>
public class StopLossToBreakEvenStrategy : Strategy
{
	private readonly StrategyParam<decimal> _breakEvenPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _entryPrice;
	private bool _stopMoved;

	/// <summary>
	/// Profit in pips required to move the stop loss to the entry price.
	/// </summary>
	public decimal BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
	}

	/// <summary>
	/// Candle type used for monitoring price movements.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public StopLossToBreakEvenStrategy()
	{
		_breakEvenPips = Param(nameof(BreakEvenPips), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Break-even Pips", "Profit in pips before moving stop to entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for monitoring", "General");
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

		var step = Security.PriceStep;
		if (step is null)
			return;

		var offset = BreakEvenPips * step.Value;

		if (Position > 0)
		{
			_entryPrice ??= candle.ClosePrice;

			if (!_stopMoved && candle.ClosePrice >= _entryPrice.Value + offset)
			{
				SellStop(Math.Abs(Position), _entryPrice.Value);
				_stopMoved = true;
			}
		}
		else if (Position < 0)
		{
			_entryPrice ??= candle.ClosePrice;

			if (!_stopMoved && candle.ClosePrice <= _entryPrice.Value - offset)
			{
				BuyStop(Math.Abs(Position), _entryPrice.Value);
				_stopMoved = true;
			}
		}
		else
		{
			_entryPrice = null;
			_stopMoved = false;
		}
	}
}
