using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// One-hour stochastic trader with Bollinger Band spread filter.
/// Opens trades during specific hours when the stochastic oscillator exits overbought or oversold zones.
/// </summary>
public class OneHrStocTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerSigma;
	private readonly StrategyParam<decimal> _bollingerSpreadLower;
	private readonly StrategyParam<decimal> _bollingerSpreadUpper;
	private readonly StrategyParam<int> _buyHourStart;
	private readonly StrategyParam<int> _sellHourStart;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _stochasticLower;
	private readonly StrategyParam<decimal> _stochasticUpper;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _maxOrdersPerDirection;

	private decimal _pipSize;
	private decimal? _previousStochastic;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private bool? _protectionIsLong;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private int _longEntries;
	private int _shortEntries;

	/// <summary>
	/// Trading volume per entry expressed in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Bollinger Bands look-back period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands sigma.
	/// </summary>
	public decimal BollingerSigma
	{
		get => _bollingerSigma.Value;
		set => _bollingerSigma.Value = value;
	}

	/// <summary>
	/// Lower limit for the Bollinger Band spread measured in pips.
	/// </summary>
	public decimal BollingerSpreadLower
	{
		get => _bollingerSpreadLower.Value;
		set => _bollingerSpreadLower.Value = value;
	}

	/// <summary>
	/// Upper limit for the Bollinger Band spread measured in pips.
	/// </summary>
	public decimal BollingerSpreadUpper
	{
		get => _bollingerSpreadUpper.Value;
		set => _bollingerSpreadUpper.Value = value;
	}

	/// <summary>
	/// Hour of day (0-23) when buy entries are allowed.
	/// </summary>
	public int BuyHourStart
	{
		get => _buyHourStart.Value;
		set => _buyHourStart.Value = value;
	}

	/// <summary>
	/// Hour of day (0-23) when sell entries are allowed.
	/// </summary>
	public int SellHourStart
	{
		get => _sellHourStart.Value;
		set => _sellHourStart.Value = value;
	}

	/// <summary>
	/// %K length for the stochastic oscillator.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// %D smoothing length for the stochastic oscillator.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Additional slowing factor for the stochastic oscillator.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Oversold threshold for the stochastic oscillator.
	/// </summary>
	public decimal StochasticLower
	{
		get => _stochasticLower.Value;
		set => _stochasticLower.Value = value;
	}

	/// <summary>
	/// Overbought threshold for the stochastic oscillator.
	/// </summary>
	public decimal StochasticUpper
	{
		get => _stochasticUpper.Value;
		set => _stochasticUpper.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Maximum number of consecutive entries per direction.
	/// </summary>
	public int MaxOrdersPerDirection
	{
		get => _maxOrdersPerDirection.Value;
		set => _maxOrdersPerDirection.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OneHrStocTraderStrategy"/> class.
	/// </summary>
	public OneHrStocTraderStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
		.SetDisplay("Volume", "Order volume in lots", "General")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle type used for calculations", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
		.SetDisplay("BB Period", "Bollinger Bands period", "Indicator")
		.SetGreaterThanZero();

		_bollingerSigma = Param(nameof(BollingerSigma), 2m)
		.SetDisplay("BB Sigma", "Bollinger Bands sigma", "Indicator")
		.SetGreaterThanZero();

		_bollingerSpreadLower = Param(nameof(BollingerSpreadLower), 56m)
		.SetDisplay("BB Spread Min", "Minimum Bollinger spread in pips", "Filters")
		.SetGreaterThanOrEqual(0m);

		_bollingerSpreadUpper = Param(nameof(BollingerSpreadUpper), 158m)
		.SetDisplay("BB Spread Max", "Maximum Bollinger spread in pips", "Filters")
		.SetGreaterThanOrEqual(0m);

		_buyHourStart = Param(nameof(BuyHourStart), 4)
		.SetDisplay("Buy Hour", "Hour when buy trades are allowed", "Timing")
		.SetRange(0, 23);

		_sellHourStart = Param(nameof(SellHourStart), 0)
		.SetDisplay("Sell Hour", "Hour when sell trades are allowed", "Timing")
		.SetRange(0, 23);

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
		.SetDisplay("Stoch %K", "Stochastic %K period", "Indicator")
		.SetGreaterThanZero();

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
		.SetDisplay("Stoch %D", "Stochastic %D period", "Indicator")
		.SetGreaterThanZero();

		_stochasticSlowing = Param(nameof(StochasticSlowing), 5)
		.SetDisplay("Stoch Slowing", "Stochastic slowing factor", "Indicator")
		.SetGreaterThanZero();

		_stochasticLower = Param(nameof(StochasticLower), 36m)
		.SetDisplay("Stoch Low", "Oversold threshold", "Indicator");

		_stochasticUpper = Param(nameof(StochasticUpper), 70m)
		.SetDisplay("Stoch High", "Overbought threshold", "Indicator");

		_takeProfitPips = Param(nameof(TakeProfitPips), 200m)
		.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk Management")
		.SetGreaterThanOrEqual(0m);

		_stopLossPips = Param(nameof(StopLossPips), 95m)
		.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk Management")
		.SetGreaterThanOrEqual(0m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk Management")
		.SetGreaterThanOrEqual(0m);

		_maxOrdersPerDirection = Param(nameof(MaxOrdersPerDirection), 1)
		.SetDisplay("Max Orders", "Maximum consecutive entries per side", "Risk Management")
		.SetGreaterThanZero();

		Volume = _tradeVolume.Value;
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
		Volume = _tradeVolume.Value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();
		_pipSize = GetPipSize();

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerSigma
		};

		var stochastic = new StochasticOscillator
		{
			KPeriod = StochasticKPeriod,
			DPeriod = StochasticDPeriod,
			Smooth = StochasticSlowing
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
		.BindEx(bollinger, stochastic, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);

			var stochArea = CreateChartArea();
			DrawIndicator(stochArea, stochastic);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue stochasticValue)
	{
		// Work only with completed candles to reproduce MetaTrader behaviour.
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (HandleProtection(candle))
		return;

		UpdateTrailingStops(candle);

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
		return;

		var stoch = (StochasticOscillatorValue)stochasticValue;
		if (stoch.K is not decimal currentStoch)
		return;

		var previousStoch = _previousStochastic;
		_previousStochastic = currentStoch;
		if (previousStoch is not decimal prev)
		return;

		if (_pipSize <= 0m)
		_pipSize = GetPipSize();

		var spread = (upper - lower) / (_pipSize > 0m ? _pipSize : 1m);
		if (spread < BollingerSpreadLower || spread > BollingerSpreadUpper)
		return;

		var hour = candle.CloseTime.Hour;

		var allowBuy = currentStoch < StochasticLower && prev < currentStoch && hour == BuyHourStart;
		var allowSell = currentStoch > StochasticUpper && prev > currentStoch && hour == SellHourStart;

		if (allowBuy)
		{
			HandleLongSignal(candle);
		}
		else if (allowSell)
		{
			HandleShortSignal(candle);
		}
	}

	private void HandleLongSignal(ICandleMessage candle)
	{
		if (TradeVolume <= 0m)
		return;

		if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			ResetProtectionState();
			_shortEntries = 0;
		}

		if (_longEntries >= MaxOrdersPerDirection)
		return;

		BuyMarket(TradeVolume);
		_longEntries++;
		_shortEntries = 0;

		SetProtection(candle.ClosePrice, true);
	}

	private void HandleShortSignal(ICandleMessage candle)
	{
		if (TradeVolume <= 0m)
		return;

		if (Position > 0)
		{
			SellMarket(Position);
			ResetProtectionState();
			_longEntries = 0;
		}

		if (_shortEntries >= MaxOrdersPerDirection)
		return;

		SellMarket(TradeVolume);
		_shortEntries++;
		_longEntries = 0;

		SetProtection(candle.ClosePrice, false);
	}

	private bool HandleProtection(ICandleMessage candle)
	{
		if (_protectionIsLong == true && Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetProtectionState();
				_longEntries = 0;
				return true;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetProtectionState();
				_longEntries = 0;
				return true;
			}
		}
		else if (_protectionIsLong == false && Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtectionState();
				_shortEntries = 0;
				return true;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtectionState();
				_shortEntries = 0;
				return true;
			}
		}

		return false;
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || _pipSize <= 0m)
		return;

		var trailingDistance = TrailingStopPips * _pipSize;

		if (_protectionIsLong == true && Position > 0 && _longEntryPrice is decimal entry)
		{
			if (candle.ClosePrice - entry > trailingDistance)
			{
				var newStop = candle.ClosePrice - trailingDistance;
				if (_stopPrice is decimal stop)
				{
					if (newStop - stop > _pipSize)
					_stopPrice = Security?.ShrinkPrice(newStop);
				}
				else
				{
					_stopPrice = Security?.ShrinkPrice(newStop);
				}
			}
		}
		else if (_protectionIsLong == false && Position < 0 && _shortEntryPrice is decimal entry)
		{
			if (entry - candle.ClosePrice > trailingDistance)
			{
				var newStop = candle.ClosePrice + trailingDistance;
				if (_stopPrice is decimal stop)
				{
					if (stop - newStop > _pipSize)
					_stopPrice = Security?.ShrinkPrice(newStop);
				}
				else
				{
					_stopPrice = Security?.ShrinkPrice(newStop);
				}
			}
		}
	}

	private void SetProtection(decimal entryPrice, bool isLong)
	{
		var stopDistance = StopLossPips > 0m ? StopLossPips * _pipSize : 0m;
		var takeDistance = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;

		_protectionIsLong = isLong;

		if (isLong)
		{
			_longEntryPrice = entryPrice;
			_shortEntryPrice = null;

			_stopPrice = stopDistance > 0m ? Security?.ShrinkPrice(entryPrice - stopDistance) : null;
			_takePrice = takeDistance > 0m ? Security?.ShrinkPrice(entryPrice + takeDistance) : null;
		}
		else
		{
			_shortEntryPrice = entryPrice;
			_longEntryPrice = null;

			_stopPrice = stopDistance > 0m ? Security?.ShrinkPrice(entryPrice + stopDistance) : null;
			_takePrice = takeDistance > 0m ? Security?.ShrinkPrice(entryPrice - takeDistance) : null;
		}
	}

	private void ResetProtectionState()
	{
		_stopPrice = null;
		_takePrice = null;
		_protectionIsLong = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	private void ResetState()
	{
		ResetProtectionState();
		_previousStochastic = null;
		_longEntries = 0;
		_shortEntries = 0;
		_pipSize = 0m;
	}

	private decimal GetPipSize()
	{
		var priceStep = Security?.PriceStep ?? 0.0001m;
		if (priceStep <= 0m)
		priceStep = 0.0001m;

		var decimals = Security?.Decimals ?? 4;
		var multiplier = decimals == 3 || decimals == 5 ? 10m : 1m;
		return priceStep * multiplier;
	}
}

