using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA 34 crossover with break even stop loss strategy.
/// Long only: buys when the close crosses above the EMA from below. The stop is the previous candle's low, the take profit lies
/// TakeProfitMultiplier times the risk above the entry, and the stop moves to the entry price once price has gone
/// BreakEvenMultiplier times the risk in favour.
/// </summary>
public class Ema34CrossoverWithBreakEvenStopLossStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<decimal> _breakEvenMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevEma;
	private decimal? _prevLow;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _breakEvenPrice;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Take profit distance in multiples of the risk.
	/// </summary>
	public decimal TakeProfitMultiplier
	{
		get => _takeProfitMultiplier.Value;
		set => _takeProfitMultiplier.Value = value;
	}

	/// <summary>
	/// Favourable move in multiples of the risk that moves the stop to break even.
	/// </summary>
	public decimal BreakEvenMultiplier
	{
		get => _breakEvenMultiplier.Value;
		set => _breakEvenMultiplier.Value = value;
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
	/// Constructor.
	/// </summary>
	public Ema34CrossoverWithBreakEvenStopLossStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Multiplier", "Take profit distance in multiples of the risk", "Risk");

		_breakEvenMultiplier = Param(nameof(BreakEvenMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Break Even Multiplier", "Favourable move in multiples of the risk that moves the stop to entry", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_prevClose = null;
		_prevEma = null;
		_prevLow = null;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_breakEvenPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevClose = _prevClose;
		var prevEma = _prevEma;
		var prevLow = _prevLow;
		_prevClose = candle.ClosePrice;
		_prevEma = ema;
		_prevLow = candle.LowPrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				return;
			}

			if (_stopPrice < _entryPrice && candle.HighPrice >= _breakEvenPrice)
				_stopPrice = _entryPrice;

			return;
		}

		if (prevClose is not decimal pc || prevEma is not decimal pe || prevLow is not decimal stop)
			return;

		var close = candle.ClosePrice;

		if (pc <= pe && close > ema)
		{
			var risk = close - stop;
			if (risk <= 0m)
				return;

			_entryPrice = close;
			_stopPrice = stop;
			_takePrice = close + risk * TakeProfitMultiplier;
			_breakEvenPrice = close + risk * BreakEvenMultiplier;

			BuyMarket(Volume);
		}
	}
}
