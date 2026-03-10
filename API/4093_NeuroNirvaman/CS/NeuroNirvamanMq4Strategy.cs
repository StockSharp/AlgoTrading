using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NeuroNirvaman: Perceptron-inspired strategy using RSI momentum
/// with EMA trend filter and weighted signal scoring.
/// </summary>
public class NeuroNirvamanMq4Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevRsi;
	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;

	public NeuroNirvamanMq4Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period.", "Indicators");

		_fastEmaLength = Param(nameof(FastEmaLength), 10)
			.SetDisplay("Fast EMA", "Fast EMA period.", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 30)
			.SetDisplay("Slow EMA", "Slow EMA period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevRsi = 0;
		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = 0;
		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var fast = new ExponentialMovingAverage { Length = FastEmaLength };
		var slow = new ExponentialMovingAverage { Length = SlowEmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, fast, slow, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal fastVal, decimal slowVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0 || _prevSlow == 0 || _prevRsi == 0 || atrVal <= 0)
		{
			_prevRsi = rsiVal;
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		var close = candle.ClosePrice;

		// Perceptron score: weighted RSI momentum + EMA trend alignment
		var rsiSignal = rsiVal > 50 ? 1m : -1m;
		var trendSignal = fastVal > slowVal ? 1m : -1m;
		var rsiAccel = rsiVal - _prevRsi > 0 ? 1m : -1m;
		var score = rsiSignal * 0.4m + trendSignal * 0.4m + rsiAccel * 0.2m;

		// Exit management
		if (Position > 0)
		{
			if (close <= _entryPrice - atrVal * 2m || close >= _entryPrice + atrVal * 3m || score < -0.5m)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close >= _entryPrice + atrVal * 2m || close <= _entryPrice - atrVal * 3m || score > 0.5m)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry: strong perceptron score with RSI crossover confirmation
		if (Position == 0)
		{
			if (score > 0.5m && _prevRsi <= 50 && rsiVal > 50)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (score < -0.5m && _prevRsi >= 50 && rsiVal < 50)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevRsi = rsiVal;
		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
