using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader Trendcapture expert advisor.
/// Trades in the SAR trend direction when ADX shows a weak trend and flips
/// orientation after losing trades.
/// </summary>
public class TrendcaptureStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _guardPoints;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _entryPrice;
	private int _positionDirection;
	private int _desiredDirection;
	private Order _stopOrder;
	private Order _takeProfitOrder;
	private ParabolicSar _sar;
	private AverageDirectionalIndex _adx;

	/// <summary>
	/// Initializes a new instance of the <see cref="TrendcaptureStrategy"/> class.
	/// </summary>
	public TrendcaptureStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 180m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (points)", "Distance to the take-profit in price steps.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(60m, 240m, 30m);

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (points)", "Distance to the stop-loss in price steps.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(20m, 120m, 10m);

		_maximumRisk = Param(nameof(MaximumRisk), 0.03m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Factor", "Scaling factor applied to the base volume (0.03 keeps the original lot size).", "Money management")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 0.10m, 0.01m);

		_guardPoints = Param(nameof(GuardPoints), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Break-even Guard (points)", "Profit distance required before the stop is moved to break-even.", "Risk");

		_sarStep = Param(nameof(SarStep), 0.02m)
		.SetGreaterThanZero()
		.SetDisplay("SAR Step", "Initial acceleration factor for Parabolic SAR.", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 0.05m, 0.005m);

		_sarMax = Param(nameof(SarMax), 0.2m)
		.SetGreaterThanZero()
		.SetDisplay("SAR Max", "Maximum acceleration factor for Parabolic SAR.", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 0.4m, 0.05m);

		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Period", "Smoothing period of the Average Directional Index.", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(7, 28, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used to evaluate the indicators.", "General");
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Risk multiplier applied to the base volume.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Distance in points before the stop is pulled to break-even.
	/// </summary>
	public decimal GuardPoints
	{
		get => _guardPoints.Value;
		set => _guardPoints.Value = value;
	}

	/// <summary>
	/// Initial Parabolic SAR acceleration step.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum Parabolic SAR acceleration.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// ADX smoothing length.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
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

		_entryPrice = null;
		_positionDirection = 0;
		_desiredDirection = 1;
		_stopOrder = null;
		_takeProfitOrder = null;
		_sar = null;
		_adx = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationStep = SarStep,
			AccelerationMax = SarMax
		};

		_adx = new AverageDirectionalIndex
		{
			Length = AdxPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_sar, _adx, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);

			if (_sar != null)
			{
				DrawIndicator(area, _sar);
			}

			DrawOwnTrades(area);

			if (_adx != null)
			{
				var adxArea = CreateChartArea();
				if (adxArea != null)
				{
					DrawIndicator(adxArea, _adx);
				}
			}
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CancelProtectionOrders();
		base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade == null)
		return;

		if (Position > 0m && trade.OrderDirection == Sides.Buy)
		{
			_entryPrice = trade.Price;
			_positionDirection = 1;
			PlaceProtectionOrders(true);
		}
		else if (Position < 0m && trade.OrderDirection == Sides.Sell)
		{
			_entryPrice = trade.Price;
			_positionDirection = -1;
			PlaceProtectionOrders(false);
		}
		else if (Position == 0m && _positionDirection != 0)
		{
			var exitPrice = trade.Price;
			FinalizeClosedPosition(exitPrice);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateBreakEven(candle.ClosePrice);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!sarValue.IsFinal || !adxValue.IsFinal)
		return;

		var sarPrice = sarValue.ToDecimal();
		var adxData = (AverageDirectionalIndexValue)adxValue;
		var adxMain = adxData.MovingAverage;

		const decimal adxThreshold = 20m;
		if (adxMain >= adxThreshold)
		return;

		if (Position != 0m)
		return;

		var volume = GetTradeVolume();
		if (volume <= 0m)
		return;

		if (_desiredDirection >= 0 && candle.ClosePrice > sarPrice)
		{
			CancelProtectionOrders();
			BuyMarket(volume);
			LogInfo($"Opening long at {candle.ClosePrice} because price is above SAR {sarPrice} with low ADX {adxMain}.");
		}
		else if (_desiredDirection < 0 && candle.ClosePrice < sarPrice)
		{
			CancelProtectionOrders();
			SellMarket(volume);
			LogInfo($"Opening short at {candle.ClosePrice} because price is below SAR {sarPrice} with low ADX {adxMain}.");
		}
	}

	private decimal GetTradeVolume()
	{
		var baseVolume = Volume;
		if (baseVolume <= 0m)
		baseVolume = 1m;

		var risk = MaximumRisk;
		if (risk <= 0m)
		return baseVolume;

		var factor = risk / 0.03m;
		if (factor <= 0m)
		factor = 1m;

		return baseVolume * factor;
	}

	private void PlaceProtectionOrders(bool isLong)
	{
		CancelProtectionOrders();

		if (_entryPrice is not decimal entry)
		return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		var step = GetPriceStep();
		if (step <= 0m)
		return;

		if (StopLossPoints > 0m)
		{
			var stopPrice = isLong
			? entry - StopLossPoints * step
			: entry + StopLossPoints * step;

			_stopOrder = isLong
			? SellStop(volume, stopPrice)
			: BuyStop(volume, stopPrice);
		}

		if (TakeProfitPoints > 0m)
		{
			var takePrice = isLong
			? entry + TakeProfitPoints * step
			: entry - TakeProfitPoints * step;

			_takeProfitOrder = isLong
			? SellLimit(volume, takePrice)
			: BuyLimit(volume, takePrice);
		}
	}

	private void UpdateBreakEven(decimal currentPrice)
	{
		if (_entryPrice is not decimal entry)
			return;

		var step = GetPriceStep();
		if (step <= 0m)
			return;

		var guardDistance = GuardPoints * step;
		if (guardDistance <= 0m)
			return;

		if (_positionDirection > 0 && currentPrice - entry >= guardDistance)
		{
			if (_stopOrder?.Price is decimal stopPrice && stopPrice >= entry)
				return;

			MoveStop(Sides.Sell, entry);
		}
		else if (_positionDirection < 0 && entry - currentPrice >= guardDistance)
		{
			if (_stopOrder?.Price is decimal stopPrice && stopPrice <= entry)
				return;

			MoveStop(Sides.Buy, entry);
		}
	}

	private void MoveStop(Sides side, decimal price)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		if (_stopOrder != null)
		{
			CancelOrder(_stopOrder);
			_stopOrder = null;
		}

		_stopOrder = side == Sides.Sell
		? SellStop(volume, price)
		: BuyStop(volume, price);
	}

	private void CancelProtectionOrders()
	{
		if (_stopOrder != null)
		{
			CancelOrder(_stopOrder);
			_stopOrder = null;
		}

		if (_takeProfitOrder != null)
		{
			CancelOrder(_takeProfitOrder);
			_takeProfitOrder = null;
		}
	}

	private void FinalizeClosedPosition(decimal exitPrice)
	{
		if (_entryPrice is not decimal entry)
		return;

		var profit = _positionDirection > 0
		? exitPrice - entry
		: entry - exitPrice;

		_desiredDirection = profit > 0m
		? _positionDirection
		: -_positionDirection;

		CancelProtectionOrders();

		_entryPrice = null;
		_positionDirection = 0;
	}

	private decimal GetPriceStep()
	{
		var security = Security;
		if (security == null)
		return 0.0001m;

		if (security.PriceStep > 0m)
		return security.PriceStep;

		if (security.MinStep > 0m)
		return security.MinStep;

		return 0.0001m;
	}
}
