namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Grid and martingale strategy converted from the MetaTrader Ilan 1.4 expert advisor.
/// The strategy opens an initial trade based on the last two candle closes and adds
/// averaging positions whenever price moves against the basket by the configured step.
/// </summary>
public class Ilan14GridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<int> _volumeDigits;
	private readonly StrategyParam<MoneyManagementModes> _moneyManagementMode;
	private readonly StrategyParam<bool> _useCloseBeforeAdding;
	private readonly StrategyParam<bool> _useAdd;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<decimal> _pipStep;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailStart;
	private readonly StrategyParam<decimal> _trailStop;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<bool> _useEquityStop;
	private readonly StrategyParam<decimal> _equityRiskPercent;
	private readonly StrategyParam<bool> _useTimeOut;
	private readonly StrategyParam<decimal> _maxTradeOpenHours;
	private readonly StrategyParam<DataType> _candleType;

	private int _tradeCount;
	private decimal _averagePrice;
	private decimal _totalVolume;
	private decimal _lastEntryPrice;
	private decimal _lastEntryVolume;
	private decimal _equityPeak;
	private decimal _lastClosedOrderVolume;
	private bool _lastClosedWasLoss;
	private decimal? _previousClose;
	private decimal? _trailingStopLevel;
	private DateTimeOffset? _basketExpiration;
	private Sides? _basketDirection;

	private enum MoneyManagementModes
	{
		Fixed,
		Geometric,
		RecoverLastLoss,
	}

public Ilan14GridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Timeframe that feeds the strategy logic.", "General");

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial volume", "Base volume used for the first trade in a basket.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 1m, 0.01m);

		_volumeDigits = Param(nameof(VolumeDigits), 2)
			.SetNotNegative()
			.SetDisplay("Volume digits", "Number of decimal places used to round trade volume.", "Trading");

		_moneyManagementMode = Param(nameof(MoneyManagementModes), MoneyManagementModes.Geometric)
			.SetDisplay("Money management", "Volume calculation mode: fixed, geometric martingale or recover last loss.", "Trading");

		_lotExponent = Param(nameof(LotExponent), 1.667m)
			.SetGreaterThanZero()
			.SetDisplay("Lot exponent", "Multiplier applied when calculating the next position size.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1.1m, 2m, 0.1m);

		_useCloseBeforeAdding = Param(nameof(UseCloseBeforeAdding), false)
			.SetDisplay("Close before adding", "Close the current basket before opening the next averaging order.", "Trading");

		_useAdd = Param(nameof(UseAdd), true)
			.SetDisplay("Allow averaging", "Enable opening of additional orders when price moves against the basket.", "Trading");

		_pipStep = Param(nameof(PipStep), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Pip step", "Distance in price steps that triggers a new averaging trade.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 5m);

		_takeProfit = Param(nameof(TakeProfit), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Take profit", "Distance from the average price where the basket is closed in profit.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(5m, 50m, 5m);

		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetNotNegative()
			.SetDisplay("Stop loss", "Maximum adverse distance from the average price before the basket is closed.", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use trailing stop", "Enable dynamic trailing of profits once the basket gains enough points.", "Risk");

		_trailStart = Param(nameof(TrailStart), 10m)
			.SetNotNegative()
			.SetDisplay("Trail start", "Profit distance in points required before the trailing stop activates.", "Risk");

		_trailStop = Param(nameof(TrailStop), 10m)
			.SetNotNegative()
			.SetDisplay("Trail distance", "Gap between current price and trailing stop when it is active.", "Risk");

		_maxTrades = Param(nameof(MaxTrades), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max trades", "Maximum number of averaging orders allowed in one basket.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1, 15, 1);

		_useEquityStop = Param(nameof(UseEquityStop), false)
			.SetDisplay("Use equity stop", "Close the basket if floating loss exceeds a share of the equity peak.", "Risk");

		_equityRiskPercent = Param(nameof(EquityRiskPercent), 20m)
			.SetNotNegative()
			.SetDisplay("Equity risk %", "Percentage of the recorded equity peak tolerated as floating loss.", "Risk");

		_useTimeOut = Param(nameof(UseTimeOut), false)
			.SetDisplay("Use timeout", "Close all trades once the basket has been open for too long.", "Risk");

		_maxTradeOpenHours = Param(nameof(MaxTradeOpenHours), 48m)
			.SetNotNegative()
			.SetDisplay("Max open hours", "Maximum lifetime of the basket before it is forcefully closed.", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	public int VolumeDigits
	{
		get => _volumeDigits.Value;
		set => _volumeDigits.Value = value;
	}

	public MoneyManagementModes MoneyManagementModes
	{
		get => _moneyManagementMode.Value;
		set => _moneyManagementMode.Value = value;
	}

	public bool UseCloseBeforeAdding
	{
		get => _useCloseBeforeAdding.Value;
		set => _useCloseBeforeAdding.Value = value;
	}

	public bool UseAdd
	{
		get => _useAdd.Value;
		set => _useAdd.Value = value;
	}

	public decimal LotExponent
	{
		get => _lotExponent.Value;
		set => _lotExponent.Value = value;
	}

	public decimal PipStep
	{
		get => _pipStep.Value;
		set => _pipStep.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	public decimal TrailStart
	{
		get => _trailStart.Value;
		set => _trailStart.Value = value;
	}

	public decimal TrailStop
	{
		get => _trailStop.Value;
		set => _trailStop.Value = value;
	}

	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	public bool UseEquityStop
	{
		get => _useEquityStop.Value;
		set => _useEquityStop.Value = value;
	}

	public decimal EquityRiskPercent
	{
		get => _equityRiskPercent.Value;
		set => _equityRiskPercent.Value = value;
	}

	public bool UseTimeOut
	{
		get => _useTimeOut.Value;
		set => _useTimeOut.Value = value;
	}

	public decimal MaxTradeOpenHours
	{
		get => _maxTradeOpenHours.Value;
		set => _maxTradeOpenHours.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		ResetState();
		_previousClose = null;
		_lastClosedWasLoss = false;
		var baseVolume = RoundVolume(InitialVolume);
		if (baseVolume <= 0m)
			baseVolume = InitialVolume;
		_lastClosedOrderVolume = baseVolume;

		Volume = baseVolume;
		_equityPeak = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;

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

		var price = candle.ClosePrice;
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		UpdateEquityPeak();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		if (_tradeCount > 0)
		{
			if (UseTimeOut && _basketExpiration != null && candle.CloseTime >= _basketExpiration)
				CloseBasket(price);

			if (_tradeCount > 0 && UseEquityStop)
			{
				var floatingLoss = CalculateFloatingProfit(price);
				var threshold = (EquityRiskPercent / 100m) * _equityPeak;
				if (floatingLoss < 0m && Math.Abs(floatingLoss) > threshold && threshold > 0m)
					CloseBasket(price);
			}

			if (_tradeCount > 0 && UseTrailingStop && TrailStop > 0m)
			{
				if (UpdateTrailingStop(price, step))
					CloseBasket(price);
			}

			if (_tradeCount > 0 && StopLoss > 0m && _basketDirection != null)
			{
				var stopDistance = StopLoss * step;
				if (_basketDirection == Sides.Buy && price <= _averagePrice - stopDistance)
					CloseBasket(price);
				else if (_basketDirection == Sides.Sell && price >= _averagePrice + stopDistance)
					CloseBasket(price);
			}

			if (_tradeCount > 0 && TakeProfit > 0m && _basketDirection != null)
			{
				var targetDistance = TakeProfit * step;
				if (_basketDirection == Sides.Buy && price >= _averagePrice + targetDistance)
					CloseBasket(price);
				else if (_basketDirection == Sides.Sell && price <= _averagePrice - targetDistance)
					CloseBasket(price);
			}

			if (_tradeCount > 0 && UseAdd && _tradeCount <= MaxTrades && _basketDirection != null)
			{
				var trigger = PipStep * step;
				if (trigger > 0m)
				{
					if (_basketDirection == Sides.Buy && _lastEntryPrice - price >= trigger)
						TryAddPosition(price, candle.CloseTime, true);
					else if (_basketDirection == Sides.Sell && price - _lastEntryPrice >= trigger)
						TryAddPosition(price, candle.CloseTime, false);
				}
			}
		}

		if (_tradeCount == 0)
		{
			if (_previousClose is null)
			{
				_previousClose = candle.ClosePrice;
				return;
			}

			var directionIsLong = _previousClose <= candle.ClosePrice;
			var volume = CalculateTradeVolume();
			if (volume > 0m)
				OpenPosition(directionIsLong ? Sides.Buy : Sides.Sell, price, volume, candle.CloseTime);
		}

		_previousClose = candle.ClosePrice;
	}

	private void TryAddPosition(decimal price, DateTimeOffset time, bool isLong)
	{
		if (!UseAdd)
			return;

		if (UseCloseBeforeAdding)
		{
			var referenceVolume = _lastEntryVolume;
			if (referenceVolume <= 0m)
				referenceVolume = CalculateTradeVolume();

			var nextVolume = RoundVolume(referenceVolume * LotExponent);
			if (nextVolume <= 0m)
				return;

			CloseBasket(price);

			OpenPosition(isLong ? Sides.Buy : Sides.Sell, price, nextVolume, time);
		}
		else
		{
			var volume = CalculateTradeVolume();
			if (volume <= 0m)
				return;

			OpenPosition(isLong ? Sides.Buy : Sides.Sell, price, volume, time);
		}
	}

	private void OpenPosition(Sides direction, decimal price, decimal volume, DateTimeOffset time)
	{
		var roundedVolume = RoundVolume(volume);
		if (roundedVolume <= 0m)
			return;

		var isFirstTrade = _tradeCount == 0;

		if (direction == Sides.Buy)
			BuyMarket(roundedVolume);
		else
			SellMarket(roundedVolume);

		if (isFirstTrade)
		{
			_basketDirection = direction;
			_averagePrice = price;
			_totalVolume = roundedVolume;
			_tradeCount = 1;
		}
		else
		{
			var previousVolume = _totalVolume;
			_totalVolume += roundedVolume;
			_averagePrice = ((_averagePrice * previousVolume) + price * roundedVolume) / _totalVolume;
			_tradeCount++;
		}

		_lastEntryPrice = price;
		_lastEntryVolume = roundedVolume;
		_trailingStopLevel = null;

		if (isFirstTrade)
		{
			if (UseTimeOut && MaxTradeOpenHours > 0m)
				_basketExpiration = time + TimeSpan.FromHours((double)MaxTradeOpenHours);
			else
				_basketExpiration = null;
		}
	}

	private void CloseBasket(decimal price)
	{
		if (_tradeCount == 0)
			return;

		var volume = Math.Abs(Position);
		if (volume > 0m)
		{
			if (_basketDirection == Sides.Buy)
			{
				if (Position > 0m)
					SellMarket(Position);
			}
			else if (_basketDirection == Sides.Sell)
			{
				if (Position < 0m)
					BuyMarket(-Position);
			}
			else
			{
				if (Position > 0m)
					SellMarket(Position);
				else if (Position < 0m)
					BuyMarket(-Position);
			}
		}

		if (_basketDirection != null && _totalVolume > 0m)
		{
			var diff = _basketDirection == Sides.Buy
				? price - _averagePrice
				: _averagePrice - price;
			var profit = diff * _totalVolume;
			_lastClosedWasLoss = profit < 0m;
			_lastClosedOrderVolume = _lastEntryVolume > 0m ? _lastEntryVolume : InitialVolume;
		}

		ResetState();
	}

	private void ResetState()
	{
		_tradeCount = 0;
		_averagePrice = 0m;
		_totalVolume = 0m;
		_lastEntryPrice = 0m;
		_lastEntryVolume = 0m;
		_trailingStopLevel = null;
		_basketExpiration = null;
		_basketDirection = null;
	}

	private void UpdateEquityPeak()
	{
		if (Portfolio == null)
			return;

		var current = Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;
		if (_tradeCount == 0 || _equityPeak <= 0m)
			_equityPeak = current;
		else if (current > _equityPeak)
			_equityPeak = current;
	}

	private decimal CalculateFloatingProfit(decimal price)
	{
		if (_tradeCount == 0 || _totalVolume <= 0m || _basketDirection == null)
			return 0m;

		return _basketDirection == Sides.Buy
			? (price - _averagePrice) * _totalVolume
			: (_averagePrice - price) * _totalVolume;
	}

	private bool UpdateTrailingStop(decimal price, decimal step)
	{
		if (_basketDirection == null || step <= 0m)
			return false;

		if (_basketDirection == Sides.Buy)
		{
			var profit = price - _averagePrice;
			if (profit < TrailStart * step)
				return false;

			var candidate = price - TrailStop * step;
			if (_trailingStopLevel is null || candidate > _trailingStopLevel)
				_trailingStopLevel = candidate;

			return _trailingStopLevel is not null && price <= _trailingStopLevel;
		}

		var shortProfit = _averagePrice - price;
		if (shortProfit < TrailStart * step)
			return false;

		var shortCandidate = price + TrailStop * step;
		if (_trailingStopLevel is null || shortCandidate < _trailingStopLevel)
			_trailingStopLevel = shortCandidate;

		return _trailingStopLevel is not null && price >= _trailingStopLevel;
	}

	private decimal CalculateTradeVolume()
	{
		decimal volume;

		switch (MoneyManagementModes)
		{
			case MoneyManagementModes.Fixed:
				volume = InitialVolume;
				break;
			case MoneyManagementModes.Geometric:
				var power = _tradeCount;
				volume = InitialVolume * (decimal)Math.Pow((double)LotExponent, power);
				break;
			case MoneyManagementModes.RecoverLastLoss:
				volume = _lastClosedWasLoss
					? (_lastClosedOrderVolume > 0m ? _lastClosedOrderVolume : InitialVolume) * LotExponent
					: InitialVolume;
				break;
			default:
				volume = InitialVolume;
				break;
		}

		return RoundVolume(volume);
	}

	private decimal RoundVolume(decimal volume)
	{
		var abs = Math.Abs(volume);
		if (abs <= 0m)
			return 0m;

		var rounded = VolumeDigits > 0
			? Math.Round(abs, VolumeDigits, MidpointRounding.AwayFromZero)
			: Math.Round(abs, MidpointRounding.AwayFromZero);

		if (rounded <= 0m)
			return 0m;

		if (Security?.VolumeStep is decimal step && step > 0m)
		{
			var steps = Math.Round(rounded / step, MidpointRounding.AwayFromZero);
			if (steps <= 0m)
				steps = 1m;
			rounded = steps * step;
		}

		return rounded;
	}
}
