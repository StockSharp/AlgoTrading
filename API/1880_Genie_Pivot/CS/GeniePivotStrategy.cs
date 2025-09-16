namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Pivot point reversal scalping strategy.
/// </summary>
public class GeniePivotStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _lows = new decimal[8];
	private readonly decimal[] _highs = new decimal[8];
	private int _filled;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;
	private int _lossCount;

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Maximum risk used to calculate volume.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Factor to decrease volume after consecutive losses.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Base volume used when account value is unknown.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="GeniePivotStrategy"/>.
	/// </summary>
	public GeniePivotStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 500m).SetDisplay("Take Profit", "Profit target in points", "Risk");

		_trailingStop =
			Param(nameof(TrailingStop), 200m).SetDisplay("Trailing Stop", "Trailing distance in points", "Risk");

		_maximumRisk = Param(nameof(MaximumRisk), 0.02m).SetDisplay("Maximum Risk", "Risk per trade", "Risk");

		_decreaseFactor =
			Param(nameof(DecreaseFactor), 3m).SetDisplay("Decrease Factor", "Volume reduction after losses", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 0.1m).SetDisplay("Base Volume", "Fallback volume", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_filled = 0;
		_entryPrice = default;
		_stopPrice = default;
		_targetPrice = default;
		_lossCount = 0;

		Array.Clear(_lows);
		Array.Clear(_highs);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private decimal GetVolume()
	{
		var lot = BaseVolume;

		if (MaximumRisk > 0m && Portfolio.CurrentValue is decimal value)
			lot = Math.Round(value * MaximumRisk / 1000m, 1);

		if (DecreaseFactor > 0m && _lossCount > 1)
			lot -= lot * _lossCount / DecreaseFactor;

		return lot < 0.1m ? 0.1m : lot;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = Security.PriceStep ?? 1m;
		var close = candle.ClosePrice;
		var open = candle.OpenPrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		// shift history
		for (var i = _lows.Length - 1; i > 0; i--)
		{
			_lows[i] = _lows[i - 1];
			_highs[i] = _highs[i - 1];
		}
		_lows[0] = low;
		_highs[0] = high;

		if (_filled < 7)
		{
			_filled++;
			return;
		}

		if (Position == 0)
		{
			var buyCond = _lows[7] > _lows[6] && _lows[6] > _lows[5] && _lows[5] > _lows[4] && _lows[4] > _lows[3] &&
						  _lows[3] > _lows[2] && _lows[2] > _lows[1] && _lows[1] < _lows[0] && _highs[1] < close;

			var sellCond = _highs[7] < _highs[6] && _highs[6] < _highs[5] && _highs[5] < _highs[4] &&
						   _highs[4] < _highs[3] && _highs[3] < _highs[2] && _highs[2] < _highs[1] &&
						   _highs[1] > _highs[0] && _lows[1] > close;

			if (buyCond)
			{
				BuyMarket(GetVolume());
				_entryPrice = close;
				_stopPrice = _entryPrice - TrailingStop * step;
				_targetPrice = _entryPrice + TakeProfit * step;
			}
			else if (sellCond)
			{
				SellMarket(GetVolume());
				_entryPrice = close;
				_stopPrice = _entryPrice + TrailingStop * step;
				_targetPrice = _entryPrice - TakeProfit * step;
			}
		}
		else if (Position > 0)
		{
			if (close >= _targetPrice)
			{
				ClosePosition();
				_lossCount = 0;
			}
			else
			{
				if (close - _entryPrice > TrailingStop * step)
				{
					var newStop = close - TrailingStop * step;
					if (newStop > _stopPrice)
						_stopPrice = newStop;
				}

				if (low <= _stopPrice || open > close)
				{
					ClosePosition();
					_lossCount++;
				}
			}
		}
		else if (Position < 0)
		{
			if (close <= _targetPrice)
			{
				ClosePosition();
				_lossCount = 0;
			}
			else
			{
				if (_entryPrice - close > TrailingStop * step)
				{
					var newStop = close + TrailingStop * step;
					if (newStop < _stopPrice)
						_stopPrice = newStop;
				}

				if (high >= _stopPrice || open < close)
				{
					ClosePosition();
					_lossCount++;
				}
			}
		}
	}
}
