using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stairs grid strategy: places trades at regular ATR-based intervals,
/// adding to position on trending moves, closing on profit target.
/// </summary>
public class StairsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _gridMultiplier;
	private readonly StrategyParam<int> _maxLayers;
	private readonly StrategyParam<decimal> _profitMultiplier;
	private readonly StrategyParam<int> _emaLength;

	private decimal _entryPrice;
	private decimal _lastGridPrice;
	private int _gridCount;
	private decimal _prevEma;

	public StairsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period for grid step.", "Indicators");

		_gridMultiplier = Param(nameof(GridMultiplier), 1.5m)
			.SetDisplay("Grid Multiplier", "ATR multiplier for grid step.", "Grid");

		_maxLayers = Param(nameof(MaxLayers), 5)
			.SetDisplay("Max Layers", "Maximum grid layers.", "Grid");

		_profitMultiplier = Param(nameof(ProfitMultiplier), 2.0m)
			.SetDisplay("Profit Multiplier", "ATR multiplier for profit target.", "Grid");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetDisplay("EMA Length", "EMA for trend direction.", "Indicators");
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

	public int MaxLayers
	{
		get => _maxLayers.Value;
		set => _maxLayers.Value = value;
	}

	public decimal ProfitMultiplier
	{
		get => _profitMultiplier.Value;
		set => _profitMultiplier.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_lastGridPrice = 0;
		_gridCount = 0;
		_prevEma = 0;

		var atr = new AverageTrueRange { Length = AtrLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

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

	private void ProcessCandle(ICandleMessage candle, decimal atrVal, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrVal <= 0 || _prevEma == 0)
		{
			_prevEma = emaVal;
			return;
		}

		var close = candle.ClosePrice;
		var gridStep = atrVal * GridMultiplier;
		var profitTarget = atrVal * ProfitMultiplier;

		// Check profit target
		if (Position > 0 && _entryPrice > 0)
		{
			if (close - _entryPrice >= profitTarget || close < emaVal)
			{
				SellMarket();
				_gridCount = 0;
				_entryPrice = 0;
				_lastGridPrice = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (_entryPrice - close >= profitTarget || close > emaVal)
			{
				BuyMarket();
				_gridCount = 0;
				_entryPrice = 0;
				_lastGridPrice = 0;
			}
		}

		// Grid: add to winning direction
		if (Position > 0 && _lastGridPrice > 0 && _gridCount < MaxLayers)
		{
			if (close - _lastGridPrice >= gridStep)
			{
				BuyMarket();
				_lastGridPrice = close;
				_gridCount++;
			}
		}
		else if (Position < 0 && _lastGridPrice > 0 && _gridCount < MaxLayers)
		{
			if (_lastGridPrice - close >= gridStep)
			{
				SellMarket();
				_lastGridPrice = close;
				_gridCount++;
			}
		}

		// Initial entry based on trend
		if (Position == 0)
		{
			var emaRising = emaVal > _prevEma;
			var emaFalling = emaVal < _prevEma;

			if (emaRising && close > emaVal)
			{
				_entryPrice = close;
				_lastGridPrice = close;
				_gridCount = 0;
				BuyMarket();
			}
			else if (emaFalling && close < emaVal)
			{
				_entryPrice = close;
				_lastGridPrice = close;
				_gridCount = 0;
				SellMarket();
			}
		}

		_prevEma = emaVal;
	}
}
