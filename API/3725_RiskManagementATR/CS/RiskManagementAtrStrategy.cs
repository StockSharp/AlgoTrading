namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class RiskManagementAtrStrategy : Strategy
{
	private const int FastMaPeriod = 10;
	private const int SlowMaPeriod = 20;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _riskPercentage;
	private readonly StrategyParam<bool> _useAtrStopLoss;
	private readonly StrategyParam<int> _fixedStopLossPoints;

	private AverageTrueRange? _atr;
	private SimpleMovingAverage? _fastMovingAverage;
	private SimpleMovingAverage? _slowMovingAverage;

	private decimal? _lastAtrValue;
	private Order? _stopLossOrder;
	private decimal _priceStep;

	public RiskManagementAtrStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR period", "Number of candles used to smooth the ATR volatility measure.", "Indicator");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR multiplier", "Distance multiplier applied to the ATR for stop-loss placement.", "Risk");

		_riskPercentage = Param(nameof(RiskPercentage), 1m)
			.SetNotNegative()
			.SetDisplay("Risk %", "Percentage of portfolio value risked on every trade.", "Risk");

		_useAtrStopLoss = Param(nameof(UseAtrStopLoss), true)
			.SetDisplay("Use ATR stop", "Switch between ATR-based and fixed-distance stop-loss modes.", "Risk");

		_fixedStopLossPoints = Param(nameof(FixedStopLossPoints), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fixed stop (points)", "Stop-loss distance expressed in price steps when ATR mode is disabled.", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public decimal RiskPercentage
	{
		get => _riskPercentage.Value;
		set => _riskPercentage.Value = value;
	}

	public bool UseAtrStopLoss
	{
		get => _useAtrStopLoss.Value;
		set => _useAtrStopLoss.Value = value;
	}

	public int FixedStopLossPoints
	{
		get => _fixedStopLossPoints.Value;
		set => _fixedStopLossPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = Volume > 0m ? Volume : 1m; // Provide a default lot size when no risk-based sizing is used

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
			_priceStep = 1m; // Fallback to a single currency unit when the instrument does not expose a price step

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		_fastMovingAverage = new SimpleMovingAverage
		{
			Length = FastMaPeriod
		};

		_slowMovingAverage = new SimpleMovingAverage
		{
			Length = SlowMaPeriod
		};

		_lastAtrValue = null;
		CancelStopLossOrder();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_atr, _fastMovingAverage, _slowMovingAverage, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawIndicator(area, _fastMovingAverage);
			DrawIndicator(area, _slowMovingAverage);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal fastMaValue, decimal slowMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return; // Work exclusively with closed candles to avoid premature entries

		_lastAtrValue = atrValue;

		if (!IsFormedAndOnlineAndAllowTrading())
			return; // Wait until the strategy is running in real time and trading is allowed

		if (_atr == null || _fastMovingAverage == null || _slowMovingAverage == null)
			return;

		if (!_atr.IsFormed || !_fastMovingAverage.IsFormed || !_slowMovingAverage.IsFormed)
			return; // Ensure all indicators accumulated enough history

		if (fastMaValue <= slowMaValue)
			return; // The simple moving average crossover only buys when the fast average is above the slow one

		if (Position != 0m)
			return; // Mimic the MetaTrader expert: enter only when there is no open position

		var volume = CalculateOrderVolume(atrValue);
		if (volume <= 0m)
			return;

		CancelStopLossOrder();

		BuyMarket(volume);
	}

	private decimal CalculateOrderVolume(decimal atrValue)
	{
		var volume = Volume > 0m ? Volume : 0m;

		var stopDistance = CalculateStopDistance(atrValue);
		if (stopDistance <= 0m)
			return 0m; // Skip trading when the stop distance cannot be computed

		var riskPercent = RiskPercentage;
		if (riskPercent > 0m)
		{
			var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
			if (portfolioValue <= 0m)
				return 0m; // Unable to size the trade without a portfolio valuation

			var riskAmount = portfolioValue * riskPercent / 100m;
			if (riskAmount <= 0m)
				return 0m;

			volume = riskAmount / stopDistance;
		}

		volume = RoundVolume(volume);
		volume = ClampVolume(volume);

		return volume > 0m ? volume : 0m;
	}

	private decimal CalculateStopDistance(decimal atrValue)
	{
		if (UseAtrStopLoss)
		{
			if (atrValue <= 0m)
				return 0m;

			var distance = atrValue * AtrMultiplier;
			return distance > 0m ? distance : 0m;
		}

		var steps = FixedStopLossPoints;
		if (steps <= 0)
			return 0m;

		return steps * _priceStep;
	}

	private decimal RoundVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			if (steps <= 0m)
				return step; // Use the minimum tradable lot when the calculated volume is below one step

			return steps * step;
		}

		return Math.Round(volume, 2, MidpointRounding.ToZero);
	}

	private decimal ClampVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var minVolume = Security?.MinVolume;
		if (minVolume != null && minVolume.Value > 0m && volume < minVolume.Value)
			volume = minVolume.Value;

		var maxVolume = Security?.MaxVolume;
		if (maxVolume != null && maxVolume.Value > 0m && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal AdjustPrice(decimal price)
	{
		if (price <= 0m)
			return 0m;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return Math.Round(price, 4, MidpointRounding.AwayFromZero);

		var steps = Math.Floor(price / step);
		if (steps <= 0m)
			return step; // Never place protective stops at non-positive prices

		return steps * step;
	}

	private void CancelStopLossOrder()
	{
		if (_stopLossOrder == null)
			return;

		if (_stopLossOrder.State == OrderStates.Active)
			CancelOrder(_stopLossOrder);

		_stopLossOrder = null;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order?.Security != Security)
			return;

		if (Position <= 0m)
			CancelStopLossOrder();

		if (trade.Order.Side != Sides.Buy)
			return; // The expert only opens long trades; sell trades come from stop-loss execution

		var atrValue = _lastAtrValue ?? 0m;
		var stopDistance = CalculateStopDistance(atrValue);
		if (stopDistance <= 0m)
			return;

		var stopPrice = trade.Trade.Price - stopDistance;
		stopPrice = AdjustPrice(stopPrice);

		if (stopPrice <= 0m || stopPrice >= trade.Trade.Price)
			return; // Do not place invalid protective stops

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		CancelStopLossOrder();

		_stopLossOrder = SellStop(volume, stopPrice);
	}
}
