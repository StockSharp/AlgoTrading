using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid martingale strategy converted from MQL4 Sophia 1_1.
/// </summary>
public class Sophia11Strategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _pipStep;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<decimal> _trailStart;
	private readonly StrategyParam<decimal> _trailStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prev1;
	private decimal _prev2;
	private decimal _prev3;
	private decimal _lastPrice;
	private decimal _avgPrice;
	private decimal _positionVolume;
	private decimal _currentVolume;
	private int _tradeCount;
	private bool _isLong;
	private decimal _trailingStop;

	/// <summary>Base volume for the first trade.</summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	/// <summary>Price steps before adding a new position.</summary>
	public decimal PipStep { get => _pipStep.Value; set => _pipStep.Value = value; }
	/// <summary>Multiplier for added position volume.</summary>
	public decimal LotExponent { get => _lotExponent.Value; set => _lotExponent.Value = value; }
	/// <summary>Maximum number of trades in the grid.</summary>
	public int MaxTrades { get => _maxTrades.Value; set => _maxTrades.Value = value; }
	/// <summary>Profit target in price steps.</summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	/// <summary>Stop loss in price steps.</summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	/// <summary>Use trailing stop.</summary>
	public bool UseTrailing { get => _useTrailing.Value; set => _useTrailing.Value = value; }
	/// <summary>Profit needed before trailing starts.</summary>
	public decimal TrailStart { get => _trailStart.Value; set => _trailStart.Value = value; }
	/// <summary>Trailing distance in price steps.</summary>
	public decimal TrailStop { get => _trailStop.Value; set => _trailStop.Value = value; }
	/// <summary>Candle timeframe used by the strategy.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Constructor.</summary>
	public Sophia11Strategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base volume for the first trade", "General");

		_pipStep = Param(nameof(PipStep), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Pip Step", "Price steps before adding a new position", "Grid");

		_lotExponent = Param(nameof(LotExponent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Exponent", "Multiplier for added position volume", "Grid");

		_maxTrades = Param(nameof(MaxTrades), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of trades", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Profit target in price steps", "Risk");

		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Loss threshold in price steps", "Risk");

		_useTrailing = Param(nameof(UseTrailing), false)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_trailStart = Param(nameof(TrailStart), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Start", "Profit before trailing starts", "Risk");

		_trailStop = Param(nameof(TrailStop), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Stop", "Trailing distance in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe of candles", "General");
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
		_prev1 = _prev2 = _prev3 = 0m;
		_lastPrice = 0m;
		_avgPrice = 0m;
		_positionVolume = 0m;
		_currentVolume = 0m;
		_tradeCount = 0;
		_isLong = false;
		_trailingStop = 0m;
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

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = Security.PriceStep ?? 1m;

		_prev3 = _prev2;
		_prev2 = _prev1;
		_prev1 = candle.ClosePrice;

		if (_prev3 == 0m)
			return;

		if (Position == 0)
		{
			_currentVolume = Volume;
			_positionVolume = 0m;
			_avgPrice = 0m;
			_tradeCount = 0;

			if (_prev1 > _prev2 && _prev2 > _prev3)
			{
				_isLong = false;
				_lastPrice = candle.ClosePrice;
				_avgPrice = candle.ClosePrice;
				_positionVolume = _currentVolume;
				SellMarket(_currentVolume);
			}
			else if (_prev1 < _prev2 && _prev2 < _prev3)
			{
				_isLong = true;
				_lastPrice = candle.ClosePrice;
				_avgPrice = candle.ClosePrice;
				_positionVolume = _currentVolume;
				BuyMarket(_currentVolume);
			}

			return;
		}

		if (_tradeCount < MaxTrades - 1)
		{
			if (_isLong)
			{
				if ((_lastPrice - candle.ClosePrice) >= PipStep * step)
				{
					_currentVolume *= LotExponent;
					_lastPrice = candle.ClosePrice;
					_tradeCount++;
					_avgPrice = (_avgPrice * _positionVolume + candle.ClosePrice * _currentVolume) / (_positionVolume + _currentVolume);
					_positionVolume += _currentVolume;
					BuyMarket(_currentVolume);
				}
			}
			else
			{
				if ((candle.ClosePrice - _lastPrice) >= PipStep * step)
				{
					_currentVolume *= LotExponent;
					_lastPrice = candle.ClosePrice;
					_tradeCount++;
					_avgPrice = (_avgPrice * _positionVolume + candle.ClosePrice * _currentVolume) / (_positionVolume + _currentVolume);
					_positionVolume += _currentVolume;
					SellMarket(_currentVolume);
				}
			}
		}

		var profit = _isLong ? candle.ClosePrice - _avgPrice : _avgPrice - candle.ClosePrice;

		if (TakeProfit > 0m && profit >= TakeProfit * step)
		{
			ClosePosition();
			return;
		}

		if (StopLoss > 0m && profit <= -StopLoss * step)
		{
			ClosePosition();
			return;
		}

		if (UseTrailing && profit >= TrailStart * step)
		{
			var newStop = _isLong ? candle.ClosePrice - TrailStop * step : candle.ClosePrice + TrailStop * step;

			if (_trailingStop == 0m || (_isLong && newStop > _trailingStop) || (!_isLong && newStop < _trailingStop))
			{
				_trailingStop = newStop;
			}
		}

		if (_trailingStop != 0m)
		{
			if (_isLong && candle.ClosePrice <= _trailingStop)
			{
				ClosePosition();
			}
			else if (!_isLong && candle.ClosePrice >= _trailingStop)
			{
				ClosePosition();
			}
		}
	}

	private void ClosePosition()
	{
		if (_isLong)
			SellMarket();
		else
			BuyMarket();

		_avgPrice = 0m;
		_positionVolume = 0m;
		_currentVolume = 0m;
		_tradeCount = 0;
		_trailingStop = 0m;
	}
}
