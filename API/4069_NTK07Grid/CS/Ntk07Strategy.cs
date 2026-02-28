using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy that enters on ATR-based breakouts and scales positions
/// using a multiplier on subsequent breakouts in the same direction.
/// </summary>
public class Ntk07Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _gridMultiplier;

	private decimal _referencePrice;
	private decimal _entryPrice;
	private bool _initialized;
	private int _gridLevel;

	public Ntk07Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "Period for ATR.", "Indicators");

		_gridMultiplier = Param(nameof(GridMultiplier), 1.5m)
			.SetDisplay("Grid Multiplier", "ATR multiplier for grid step.", "Grid");
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

		_referencePrice = 0;
		_entryPrice = 0;
		_initialized = false;
		_gridLevel = 0;

		var atr = new AverageTrueRange { Length = AtrLength };
		var ema = new ExponentialMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrValue <= 0)
			return;

		var close = candle.ClosePrice;
		var gridStep = atrValue * GridMultiplier;

		if (!_initialized)
		{
			_referencePrice = close;
			_initialized = true;
			return;
		}

		// Position management
		if (Position > 0)
		{
			// Take profit at 2x grid step from entry
			if (_entryPrice > 0 && close >= _entryPrice + gridStep * 2)
			{
				SellMarket();
				_referencePrice = close;
				_gridLevel = 0;
			}
			// Stop-loss at 1.5x grid step from entry
			else if (_entryPrice > 0 && close <= _entryPrice - gridStep * 1.5m)
			{
				SellMarket();
				_referencePrice = close;
				_gridLevel = 0;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice > 0 && close <= _entryPrice - gridStep * 2)
			{
				BuyMarket();
				_referencePrice = close;
				_gridLevel = 0;
			}
			else if (_entryPrice > 0 && close >= _entryPrice + gridStep * 1.5m)
			{
				BuyMarket();
				_referencePrice = close;
				_gridLevel = 0;
			}
		}

		// Entry: price moves a full grid step from reference
		if (Position == 0)
		{
			if (close > _referencePrice + gridStep && close > emaValue)
			{
				_entryPrice = close;
				_referencePrice = close;
				_gridLevel = 1;
				BuyMarket();
			}
			else if (close < _referencePrice - gridStep && close < emaValue)
			{
				_entryPrice = close;
				_referencePrice = close;
				_gridLevel = 1;
				SellMarket();
			}
		}
	}
}
