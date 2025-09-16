using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Divergence-based strategy converted from the Divergence Trader MQL expert advisor.
/// </summary>
public class DivergenceTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<CandlePrice> _fastPriceType;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<CandlePrice> _slowPriceType;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _stayOutThreshold;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _breakEvenPips;
	private readonly StrategyParam<decimal> _breakEvenBufferPips;
	private readonly StrategyParam<decimal> _basketProfit;
	private readonly StrategyParam<decimal> _basketLoss;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage? _fastMa;
	private SimpleMovingAverage? _slowMa;
	private decimal? _previousDifference;
	private decimal? _entryPrice;
	private decimal? _breakEvenPrice;
	private decimal? _trailingStopPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private decimal _pipSize;
	private decimal _maxBasketPnL;
	private decimal _minBasketPnL;

	public DivergenceTraderStrategy()
	{
		_lotSize = Param(nameof(LotSize), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Size", "Trading volume for new positions.", "Trading")
			.SetCanOptimize(true);

		_fastPeriod = Param(nameof(FastPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA Period", "Length of the fast simple moving average.", "Indicators")
			.SetCanOptimize(true);

		_fastPriceType = Param(nameof(FastPriceType), CandlePrice.Open)
			.SetDisplay("Fast SMA Price", "Applied price for the fast moving average.", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 88)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Period", "Length of the slow simple moving average.", "Indicators")
			.SetCanOptimize(true);

		_slowPriceType = Param(nameof(SlowPriceType), CandlePrice.Open)
			.SetDisplay("Slow SMA Price", "Applied price for the slow moving average.", "Indicators");

		_buyThreshold = Param(nameof(BuyThreshold), 0.0011m)
			.SetDisplay("Buy Threshold", "Minimum divergence value required before buying.", "Signals")
			.SetCanOptimize(true);

		_stayOutThreshold = Param(nameof(StayOutThreshold), 0.0079m)
			.SetDisplay("Stay-Out Threshold", "Upper divergence limit that disables new entries.", "Signals")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
			.SetDisplay("Take Profit (pips)", "Distance in pips used to secure profits.", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 0m)
			.SetDisplay("Stop Loss (pips)", "Distance in pips allowed against the position.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0m)
			.SetDisplay("Trailing Stop (pips)", "Trailing distance maintained once the trade is profitable.", "Risk");

		_breakEvenPips = Param(nameof(BreakEvenPips), 0m)
			.SetDisplay("Break-Even Trigger (pips)", "Profit in pips required before moving the stop to break-even.", "Risk");

		_breakEvenBufferPips = Param(nameof(BreakEvenBufferPips), 2m)
			.SetDisplay("Break-Even Buffer (pips)", "Offset applied when shifting the stop to break-even.", "Risk");

		_basketProfit = Param(nameof(BasketProfit), 75m)
			.SetDisplay("Basket Profit", "Equity increase that forces a global exit.", "Risk");

		_basketLoss = Param(nameof(BasketLoss), 0m)
			.SetDisplay("Basket Loss", "Equity drawdown that forces a global exit.", "Risk");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Hour of the day (local time) when trading becomes active.", "Schedule");

		_stopHour = Param(nameof(StopHour), 24)
			.SetDisplay("Stop Hour", "Hour of the day (local time) when trading is disabled.", "Schedule");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for calculations.", "General");
	}

	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public CandlePrice FastPriceType
	{
		get => _fastPriceType.Value;
		set => _fastPriceType.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public CandlePrice SlowPriceType
	{
		get => _slowPriceType.Value;
		set => _slowPriceType.Value = value;
	}

	public decimal BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	public decimal StayOutThreshold
	{
		get => _stayOutThreshold.Value;
		set => _stayOutThreshold.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public decimal BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
	}

	public decimal BreakEvenBufferPips
	{
		get => _breakEvenBufferPips.Value;
		set => _breakEvenBufferPips.Value = value;
	}

	public decimal BasketProfit
	{
		get => _basketProfit.Value;
		set => _basketProfit.Value = value;
	}

	public decimal BasketLoss
	{
		get => _basketLoss.Value;
		set => _basketLoss.Value = value;
	}

	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
		_fastMa = null;
		_slowMa = null;
		_previousDifference = null;
		_maxBasketPnL = 0m;
		_minBasketPnL = 0m;
		_pipSize = 0m;
		ResetTradeTracking();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetTradeTracking();
		_pipSize = CalculatePipSize();

		_fastMa = new SimpleMovingAverage
		{
			Length = FastPeriod,
			CandlePrice = FastPriceType
		};

		_slowMa = new SimpleMovingAverage
		{
			Length = SlowPeriod,
			CandlePrice = SlowPriceType
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageOpenPosition(candle);

		if (CheckBasketLimits())
		{
			_previousDifference = fastValue - slowValue;
			return;
		}

		if (_fastMa == null || _slowMa == null)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
		{
			_previousDifference = fastValue - slowValue;
			return;
		}

		var divergence = _previousDifference ?? fastValue - slowValue;
		_previousDifference = fastValue - slowValue;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		if (!IsWithinTradingHours(candle.OpenTime))
			return;

		var volume = AdjustVolume(LotSize);
		if (volume <= 0m)
			return;

		if (divergence >= BuyThreshold && divergence <= StayOutThreshold)
		{
			ResetTradeTracking();
			BuyMarket(volume);
		}
		else if (divergence <= -BuyThreshold && divergence >= -StayOutThreshold)
		{
			ResetTradeTracking();
			SellMarket(volume);
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0)
		{
			ResetTradeTracking();
			return;
		}

		if (_entryPrice == null || _entryPrice == 0m)
		{
			if (PositionPrice != 0m)
			{
				_entryPrice = PositionPrice;
				_highestPrice = PositionPrice;
				_lowestPrice = PositionPrice;
			}
			else
			{
				return;
			}
		}

		var entry = _entryPrice!.Value;
		var pipSize = EnsurePipSize();
		var absPosition = Math.Abs(Position);
		var breakEvenDistance = BreakEvenPips > 0m ? BreakEvenPips * pipSize : 0m;
		var breakEvenBuffer = BreakEvenBufferPips > 0m ? BreakEvenBufferPips * pipSize : 0m;
		var trailingDistance = TrailingStopPips > 0m ? TrailingStopPips * pipSize : 0m;
		var takeProfitDistance = TakeProfitPips > 0m ? TakeProfitPips * pipSize : 0m;
		var stopLossDistance = StopLossPips > 0m ? StopLossPips * pipSize : 0m;

		if (Position > 0)
		{
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
			var currentProfit = candle.ClosePrice - entry;

			if (breakEvenDistance > 0m && currentProfit >= breakEvenDistance && _breakEvenPrice == null)
				_breakEvenPrice = entry + breakEvenBuffer;

			if (_breakEvenPrice != null && candle.LowPrice <= _breakEvenPrice.Value)
			{
				SellMarket(absPosition);
				ResetTradeTracking();
				return;
			}

			if (trailingDistance > 0m && currentProfit >= trailingDistance)
			{
				var candidate = _highestPrice - trailingDistance;
				if (_trailingStopPrice == null || candidate > _trailingStopPrice.Value)
					_trailingStopPrice = candidate;

				if (_trailingStopPrice != null && candle.LowPrice <= _trailingStopPrice.Value)
				{
					SellMarket(absPosition);
					ResetTradeTracking();
					return;
				}
			}

			if (takeProfitDistance > 0m && currentProfit >= takeProfitDistance)
			{
				SellMarket(absPosition);
				ResetTradeTracking();
				return;
			}

			if (stopLossDistance > 0m && candle.LowPrice <= entry - stopLossDistance)
			{
				SellMarket(absPosition);
				ResetTradeTracking();
			}
		}
		else if (Position < 0)
		{
			_lowestPrice = Math.Min(_lowestPrice == 0m ? entry : _lowestPrice, candle.LowPrice);
			var currentProfit = entry - candle.ClosePrice;

			if (breakEvenDistance > 0m && currentProfit >= breakEvenDistance && _breakEvenPrice == null)
				_breakEvenPrice = entry - breakEvenBuffer;

			if (_breakEvenPrice != null && candle.HighPrice >= _breakEvenPrice.Value)
			{
				BuyMarket(absPosition);
				ResetTradeTracking();
				return;
			}

			if (trailingDistance > 0m && currentProfit >= trailingDistance)
			{
				var candidate = (_lowestPrice == 0m ? entry : _lowestPrice) + trailingDistance;
				if (_trailingStopPrice == null || candidate < _trailingStopPrice.Value)
					_trailingStopPrice = candidate;

				if (_trailingStopPrice != null && candle.HighPrice >= _trailingStopPrice.Value)
				{
					BuyMarket(absPosition);
					ResetTradeTracking();
					return;
				}
			}

			if (takeProfitDistance > 0m && currentProfit >= takeProfitDistance)
			{
				BuyMarket(absPosition);
				ResetTradeTracking();
				return;
			}

			if (stopLossDistance > 0m && candle.HighPrice >= entry + stopLossDistance)
			{
				BuyMarket(absPosition);
				ResetTradeTracking();
			}
		}
	}

	private bool CheckBasketLimits()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return false;

		var current = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		var initial = portfolio.BeginValue ?? current;
		var basketPnL = current - initial;

		if (basketPnL > _maxBasketPnL)
			_maxBasketPnL = basketPnL;

		if (basketPnL < _minBasketPnL)
			_minBasketPnL = basketPnL;

		if (BasketProfit > 0m && basketPnL >= BasketProfit)
		{
			CloseAllPositions();
			return true;
		}

		if (BasketLoss > 0m && basketPnL <= -BasketLoss)
		{
			CloseAllPositions();
			return true;
		}

		return false;
	}

	private void CloseAllPositions()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}

		CancelActiveOrders();
		ResetTradeTracking();
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (volume < minVolume)
			return 0m;

		var maxVolume = security.MaxVolume;
		if (maxVolume != null && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal EnsurePipSize()
	{
		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		return _pipSize > 0m ? _pipSize : Security?.PriceStep ?? 0m;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return 0m;

		var bits = decimal.GetBits(priceStep);
		var scale = (bits[3] >> 16) & 0xFF;
		var multiplier = scale == 3 || scale == 5 ? 10m : 1m;

		return priceStep * multiplier;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var start = Math.Min(Math.Max(StartHour, 0), 24);
		var stop = Math.Min(Math.Max(StopHour, 0), 24);
		var hour = time.LocalDateTime.Hour;

		if (start == stop)
			return true;

		return start < stop ? hour >= start && hour < stop : hour >= start || hour < stop;
	}

	private void ResetTradeTracking()
	{
		_entryPrice = null;
		_breakEvenPrice = null;
		_trailingStopPrice = null;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}
}
