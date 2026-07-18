using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy with automatic take-profit and stop-loss based on ATR.
/// Uses EMA crossover for entry signals with ATR-based TP/SL management.
/// </summary>
public class AutostopStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _tpMultiplier;
	private readonly StrategyParam<decimal> _slMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _takePrice;
	private decimal _stopPrice;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal TpMultiplier { get => _tpMultiplier.Value; set => _tpMultiplier.Value = value; }
	public decimal SlMultiplier { get => _slMultiplier.Value; set => _slMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="AutostopStrategy"/>.
	/// </summary>
	public AutostopStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "General");
		_slowLength = Param(nameof(SlowLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "General");
		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for TP/SL", "Risk");
		_tpMultiplier = Param(nameof(TpMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("TP Multiplier", "ATR multiplier for take profit", "Risk");
		_slMultiplier = Param(nameof(SlMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("SL Multiplier", "ATR multiplier for stop loss", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_takePrice = 0m;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };
		var atr = new StandardDeviation { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastEma, slowEma, atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Check TP/SL for existing positions
		if (Position > 0)
		{
			if (candle.ClosePrice >= _takePrice || candle.ClosePrice <= _stopPrice)
			{
				SellMarket();
				return;
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice <= _takePrice || candle.ClosePrice >= _stopPrice)
			{
				BuyMarket();
				return;
			}
		}

		// Entry signals
		if (fast > slow && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_takePrice = _entryPrice + atrValue * TpMultiplier;
			_stopPrice = _entryPrice - atrValue * SlMultiplier;
		}
		else if (fast < slow && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_takePrice = _entryPrice - atrValue * TpMultiplier;
			_stopPrice = _entryPrice + atrValue * SlMultiplier;
		}
	}
}
