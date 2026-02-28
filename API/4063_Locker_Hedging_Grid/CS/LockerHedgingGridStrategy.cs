using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy that opens positions at regular price intervals.
/// Uses ATR to determine grid spacing and reverses direction on profit targets.
/// </summary>
public class LockerHedgingGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _gridMultiplier;

	private decimal _gridLevel;
	private decimal _entryPrice;
	private bool _initialized;

	public LockerHedgingGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "Period for ATR calculation.", "Indicators");

		_gridMultiplier = Param(nameof(GridMultiplier), 1.5m)
			.SetDisplay("Grid Multiplier", "ATR multiplier for grid spacing.", "Grid");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal GridMultiplier
	{
		get => _gridMultiplier.Value;
		set => _gridMultiplier.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_gridLevel = 0;
		_entryPrice = 0;
		_initialized = false;

		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrValue <= 0)
			return;

		var close = candle.ClosePrice;
		var gridStep = atrValue * GridMultiplier;

		if (!_initialized)
		{
			_gridLevel = close;
			_initialized = true;
			return;
		}

		// Grid logic: trade when price moves a full grid step
		if (Position == 0)
		{
			if (close >= _gridLevel + gridStep)
			{
				// Price moved up a grid step - buy
				_entryPrice = close;
				_gridLevel = close;
				BuyMarket();
			}
			else if (close <= _gridLevel - gridStep)
			{
				// Price moved down a grid step - sell
				_entryPrice = close;
				_gridLevel = close;
				SellMarket();
			}
		}
		else if (Position > 0)
		{
			if (close >= _entryPrice + gridStep)
			{
				// Take profit
				SellMarket();
				_gridLevel = close;
			}
			else if (close <= _entryPrice - gridStep * 2)
			{
				// Stop-loss at 2x grid step
				SellMarket();
				_gridLevel = close;
			}
		}
		else if (Position < 0)
		{
			if (close <= _entryPrice - gridStep)
			{
				// Take profit
				BuyMarket();
				_gridLevel = close;
			}
			else if (close >= _entryPrice + gridStep * 2)
			{
				// Stop-loss at 2x grid step
				BuyMarket();
				_gridLevel = close;
			}
		}
	}
}
