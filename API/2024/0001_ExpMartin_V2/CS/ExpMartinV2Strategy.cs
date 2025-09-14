using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Exponential martingale strategy.
/// </summary>
public class ExpMartinV2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _startVolume;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _limit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _startType;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _currentVolume;
	private decimal _maxVolume;
	private bool _needOpenPosition = true;
	private int _direction;
	private decimal _entryPrice;
	private decimal _longTake;
	private decimal _longStop;
	private decimal _shortTake;
	private decimal _shortStop;

	/// <summary>
	/// Initial order volume.
	/// </summary>
	public decimal StartVolume { get => _startVolume.Value; set => _startVolume.Value = value; }

	/// <summary>
	/// Volume multiplier after a loss.
	/// </summary>
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }

	/// <summary>
	/// Maximum number of volume multiplications.
	/// </summary>
	public int Limit { get => _limit.Value; set => _limit.Value = value; }

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Starting order type: 0 - buy, 1 - sell.
	/// </summary>
	public int StartType { get => _startType.Value; set => _startType.Value = value; }

	/// <summary>
	/// Candle type used for processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExpMartinV2Strategy()
	{
		_startVolume = Param(nameof(StartVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Start Volume", "Initial order volume", "General");

		_factor = Param(nameof(Factor), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Factor", "Volume multiplier", "General");

		_limit = Param(nameof(Limit), 5)
			.SetGreaterThanZero()
			.SetDisplay("Limit", "Max multiplication count", "General");

		_stopLoss = Param(nameof(StopLoss), 100)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Loss limit in points", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 100)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Profit target in points", "Risk");

		_startType = Param(nameof(StartType), 0)
			.SetDisplay("Start Type", "0-Buy, 1-Sell", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_currentVolume = StartVolume;
		_direction = StartType == 0 ? 1 : -1;

		_maxVolume = StartVolume;
		for (var i = 0; i < Limit; i++)
			_maxVolume = RoundVolume(_maxVolume * Factor);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security.PriceStep ?? 1m;

		if (Position > 0)
		{
			if (candle.HighPrice >= _longTake)
			{
				SellMarket(Position);
				PrepareNext(true);
			}
			else if (candle.LowPrice <= _longStop)
			{
				SellMarket(Position);
				PrepareNext(false);
			}
		}
		else if (Position < 0)
		{
			if (candle.LowPrice <= _shortTake)
			{
				BuyMarket(-Position);
				PrepareNext(true);
			}
			else if (candle.HighPrice >= _shortStop)
			{
				BuyMarket(-Position);
				PrepareNext(false);
			}
		}

		if (_needOpenPosition && Position == 0)
		{
			_entryPrice = candle.ClosePrice;
			if (_direction == 1)
			{
				BuyMarket(_currentVolume);
				_longTake = _entryPrice + TakeProfit * step;
				_longStop = _entryPrice - StopLoss * step;
			}
			else
			{
				SellMarket(_currentVolume);
				_shortTake = _entryPrice - TakeProfit * step;
				_shortStop = _entryPrice + StopLoss * step;
			}
			_needOpenPosition = false;
		}
	}

	private void PrepareNext(bool wasProfit)
	{
		if (wasProfit)
		{
			_currentVolume = StartVolume;
		}
		else
		{
			_direction = -_direction;
			_currentVolume = RoundVolume(_currentVolume * Factor);
			if (_currentVolume > _maxVolume)
				_currentVolume = StartVolume;
		}

		_needOpenPosition = true;
	}

	private decimal RoundVolume(decimal volume)
	{
		var step = Security.VolumeStep ?? 1m;
		return Math.Ceiling(volume / step) * step;
	}
}
