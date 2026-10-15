using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Nirvaman Imax: Dual EMA crossover with trend filter and timed exit.
/// Fast EMA crosses slow EMA for direction, filter EMA confirms trend.
/// </summary>
public class NirvamanImaxStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _filterLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;

	public NirvamanImaxStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_fastLength = Param(nameof(FastLength), 10)
			.SetDisplay("Fast EMA", "Fast EMA period.", "Indicators");

		_slowLength = Param(nameof(SlowLength), 21)
			.SetDisplay("Slow EMA", "Slow EMA period.", "Indicators");

		_filterLength = Param(nameof(FilterLength), 50)
			.SetDisplay("Filter EMA", "Trend filter EMA period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period for stops.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int FilterLength
	{
		get => _filterLength.Value;
		set => _filterLength.Value = value;
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

		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;

		var fast = new ExponentialMovingAverage { Length = FastLength };
		var slow = new ExponentialMovingAverage { Length = SlowLength };
		var filter = new ExponentialMovingAverage { Length = FilterLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, filter, atr, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal filterVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0 || _prevSlow == 0 || atrVal <= 0)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		var close = candle.ClosePrice;

		// Exit management
		if (Position > 0)
		{
			if (close <= _entryPrice - atrVal * 2m || close >= _entryPrice + atrVal * 3m)
			{
				SellMarket();
				_entryPrice = 0;
			}
			else if (_prevFast >= _prevSlow && fastVal < slowVal)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close >= _entryPrice + atrVal * 2m || close <= _entryPrice - atrVal * 3m)
			{
				BuyMarket();
				_entryPrice = 0;
			}
			else if (_prevFast <= _prevSlow && fastVal > slowVal)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry: EMA crossover confirmed by filter EMA trend
		if (Position == 0)
		{
			if (_prevFast <= _prevSlow && fastVal > slowVal && close > filterVal)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (_prevFast >= _prevSlow && fastVal < slowVal && close < filterVal)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
