using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Replica of the MetaTrader ZigAndZag trader that follows a long-term ZigZag trend and short-term swings.
/// </summary>
public class ZigAndZagTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _lots;
	private readonly StrategyParam<int> _trendDepth;
	private readonly StrategyParam<int> _exitDepth;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;

	private Lowest? _longTermLow;
	private Highest? _longTermHigh;
	private Lowest? _shortTermLow;
	private Highest? _shortTermHigh;

	private decimal _pipSize;
	private decimal _volumeStep;
	private decimal _breakoutThreshold;

	private decimal? _lastTrendLow;
	private decimal? _lastTrendHigh;
	private decimal? _lastShortLow;
	private decimal? _lastShortHigh;
	private decimal? _lastSlalomZig;
	private decimal? _lastSlalomZag;

	private bool _trendUp;
	private bool _prevTrendUp;
	private bool _buyArmed;
	private bool _sellArmed;
	private bool _limitArmed;

	private PivotType _lastPivot;

	/// <summary>
	/// Trading candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Requested trade volume in lots.
	/// </summary>
	public decimal Lots
	{
		get => _lots.Value;
		set => _lots.Value = value;
	}

	/// <summary>
	/// Depth of the long-term ZigZag that defines the prevailing trend.
	/// </summary>
	public int TrendDepth
	{
		get => _trendDepth.Value;
		set => _trendDepth.Value = value;
	}

	/// <summary>
	/// Depth of the short-term ZigZag that produces swing entries and exits.
	/// </summary>
	public int ExitDepth
	{
		get => _exitDepth.Value;
		set => _exitDepth.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open orders.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips (0 disables the stop).
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips (0 disables the target).
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public ZigAndZagTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for swing detection", "General");

		_lots = Param(nameof(Lots), 0.1m)
			.SetDisplay("Lots", "Requested trade size in lots", "Trading")
			.SetGreaterThanZero();

		_trendDepth = Param(nameof(TrendDepth), 3)
			.SetDisplay("Trend Depth", "Lookback for the long-term ZigZag", "ZigZag")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_exitDepth = Param(nameof(ExitDepth), 3)
			.SetDisplay("Exit Depth", "Lookback for the short-term swing ZigZag", "ZigZag")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_maxOrders = Param(nameof(MaxOrders), 1)
			.SetDisplay("Max Orders", "Maximum simultaneous positions", "Trading")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 0m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk")
			.SetGreaterOrEqualZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
			.SetDisplay("Take Profit (pips)", "Profit target distance", "Risk")
			.SetGreaterOrEqualZero();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longTermLow = null;
		_longTermHigh = null;
		_shortTermLow = null;
		_shortTermHigh = null;

		_lastTrendLow = null;
		_lastTrendHigh = null;
		_lastShortLow = null;
		_lastShortHigh = null;
		_lastSlalomZig = null;
		_lastSlalomZag = null;

		_trendUp = true;
		_prevTrendUp = true;
		_buyArmed = false;
		_sellArmed = false;
		_limitArmed = false;
		_lastPivot = PivotType.None;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0.0001m;
		_volumeStep = Security?.VolumeStep ?? 1m;
		if (_volumeStep <= 0m)
			_volumeStep = 1m;

		_breakoutThreshold = _pipSize;

		var rawVolume = Lots > 0m ? Lots : _volumeStep;
		if (rawVolume < _volumeStep)
			rawVolume = _volumeStep;

		var steps = Math.Max(1L, (long)Math.Ceiling((double)(rawVolume / _volumeStep)));
		Volume = steps * _volumeStep;

		StartProtection(
			takeProfit: TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null,
			stopLoss: StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null,
			useMarketOrders: true);

		_longTermLow = new Lowest { Length = TrendDepth };
		_longTermHigh = new Highest { Length = TrendDepth };
		_shortTermLow = new Lowest { Length = ExitDepth };
		_shortTermHigh = new Highest { Length = ExitDepth };

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

		var longLow = _longTermLow?.Process(candle.LowPrice).ToDecimal();
		var longHigh = _longTermHigh?.Process(candle.HighPrice).ToDecimal();
		var shortLow = _shortTermLow?.Process(candle.LowPrice).ToDecimal();
		var shortHigh = _shortTermHigh?.Process(candle.HighPrice).ToDecimal();

		var longFormed = _longTermLow?.IsFormed == true && _longTermHigh?.IsFormed == true;
		var shortFormed = _shortTermLow?.IsFormed == true && _shortTermHigh?.IsFormed == true;

		var navel = (5m * candle.ClosePrice + 2m * candle.OpenPrice + candle.HighPrice + candle.LowPrice) / 9m;

		if (longFormed)
		{
			if (candle.LowPrice == longLow && (_lastTrendLow == null || longLow != _lastTrendLow))
			{
				_trendUp = true;
				_lastTrendLow = longLow;
			}

			if (candle.HighPrice == longHigh && (_lastTrendHigh == null || longHigh != _lastTrendHigh))
			{
				_trendUp = false;
				_lastTrendHigh = longHigh;
			}
		}

		if (_trendUp != _prevTrendUp)
		{
			_buyArmed = false;
			_sellArmed = false;
			_limitArmed = false;
			_prevTrendUp = _trendUp;
		}

		if (shortFormed)
		{
			if (candle.LowPrice == shortLow && (_lastShortLow == null || shortLow != _lastShortLow))
			{
				_lastPivot = PivotType.Low;
				_lastShortLow = shortLow;
				_lastSlalomZig = navel;
				_buyArmed = false;
				_sellArmed = false;
				_limitArmed = false;
			}

			if (candle.HighPrice == shortHigh && (_lastShortHigh == null || shortHigh != _lastShortHigh))
			{
				_lastPivot = PivotType.High;
				_lastShortHigh = shortHigh;
				_lastSlalomZag = navel;
				_buyArmed = false;
				_sellArmed = false;
				_limitArmed = false;
			}
		}

		if (!longFormed || !shortFormed)
			return;

		var buySignal = false;
		var sellSignal = false;
		var closeSignal = false;

		switch (_lastPivot)
		{
			case PivotType.Low when _lastSlalomZig != null:
			{
				if (_trendUp)
				{
					var shouldBuy = navel - _lastSlalomZig.Value >= _breakoutThreshold;
					if (shouldBuy && !_buyArmed)
					{
						_buyArmed = true;
						buySignal = true;
					}
					else if (!shouldBuy && _buyArmed && navel <= _lastSlalomZig.Value)
					{
						_buyArmed = false;
					}

					if (_limitArmed && navel <= _lastSlalomZig.Value)
						_limitArmed = false;
				}
				else
				{
					var shouldClose = navel > _lastSlalomZig.Value;
					if (shouldClose && !_limitArmed)
					{
						_limitArmed = true;
						closeSignal = true;
					}
					else if (!shouldClose && _limitArmed)
					{
						_limitArmed = false;
					}

					_buyArmed = false;
				}

				break;
			}
			case PivotType.High when _lastSlalomZag != null:
			{
				if (!_trendUp)
				{
					var shouldSell = _lastSlalomZag.Value - navel >= _breakoutThreshold;
					if (shouldSell && !_sellArmed)
					{
						_sellArmed = true;
						sellSignal = true;
					}
					else if (!shouldSell && _sellArmed && navel >= _lastSlalomZag.Value)
					{
						_sellArmed = false;
					}

					if (_limitArmed && navel >= _lastSlalomZag.Value)
						_limitArmed = false;
				}
				else
				{
					var shouldClose = _lastSlalomZag.Value > navel;
					if (shouldClose && !_limitArmed)
					{
						_limitArmed = true;
						closeSignal = true;
					}
					else if (!shouldClose && _limitArmed)
					{
						_limitArmed = false;
					}

					_sellArmed = false;
				}

				break;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ExecuteSignals(buySignal, sellSignal, closeSignal);
	}

	private void ExecuteSignals(bool buySignal, bool sellSignal, bool closeSignal)
	{
		var volume = Volume;
		if (volume <= 0m || MaxOrders <= 0)
			return;

		var maxVolume = MaxOrders * volume;

		if (buySignal)
		{
			var currentLong = Position > 0m ? Position : 0m;
			var available = maxVolume - currentLong;
			if (available > 0m)
			{
				var tradeVolume = Math.Min(volume, available);
				BuyMarket(tradeVolume);
			}
		}

		if (sellSignal)
		{
			var currentShort = Position < 0m ? -Position : 0m;
			var available = maxVolume - currentShort;
			if (available > 0m)
			{
				var tradeVolume = Math.Min(volume, available);
				SellMarket(tradeVolume);
			}
		}

		if (closeSignal && Position != 0m)
			CloseAll();
	}

	private enum PivotType
	{
		None,
		Low,
		High
	}
}
