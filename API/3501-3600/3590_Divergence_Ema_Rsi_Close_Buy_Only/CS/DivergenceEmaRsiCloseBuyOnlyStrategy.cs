using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Divergence EMA RSI Close Buy Only" MetaTrader expert.
/// Long-only strategy: buys when price pulls back below fast EMA with RSI oversold,
/// exits when RSI reaches overbought level.
/// </summary>
public class DivergenceEmaRsiCloseBuyOnlyStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiEntry;
	private readonly StrategyParam<decimal> _rsiExit;

	private ExponentialMovingAverage _ema;
	private RelativeStrengthIndex _rsi;
	private decimal? _prevRsi;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal RsiEntry
	{
		get => _rsiEntry.Value;
		set => _rsiEntry.Value = value;
	}

	public decimal RsiExit
	{
		get => _rsiExit.Value;
		set => _rsiExit.Value = value;
	}

	public DivergenceEmaRsiCloseBuyOnlyStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signals", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for trend filter", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period for entry/exit", "Indicators");

		_rsiEntry = Param(nameof(RsiEntry), 35m)
			.SetDisplay("RSI Entry", "RSI level to enter long", "Signals");

		_rsiExit = Param(nameof(RsiExit), 65m)
			.SetDisplay("RSI Exit", "RSI level to exit long", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_prevRsi = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed || !_rsi.IsFormed)
		{
			_prevRsi = rsiValue;
			return;
		}

		if (_prevRsi is null)
		{
			_prevRsi = rsiValue;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;
		var close = candle.ClosePrice;

		// Exit: RSI crosses above exit level
		if (Position > 0 && _prevRsi.Value < RsiExit && rsiValue >= RsiExit)
		{
			SellMarket(Position);
		}

		// Entry: RSI crosses below entry level and price is near/below EMA (buy the dip)
		if (Position <= 0 && _prevRsi.Value > RsiEntry && rsiValue <= RsiEntry && close <= emaValue * 1.005m)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			BuyMarket(volume);
		}

		_prevRsi = rsiValue;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_ema = null;
		_rsi = null;
		_prevRsi = null;

		base.OnReseted();
	}
}
