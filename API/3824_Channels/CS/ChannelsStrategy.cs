namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Port of the MetaTrader 4 "Channels" strategy built around EMA envelopes.
/// </summary>
public class ChannelsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _envelopeLarge;
	private readonly StrategyParam<decimal> _envelopeMedium;
	private readonly StrategyParam<decimal> _envelopeSmall;
	private readonly StrategyParam<decimal> _buyStopLossPoints;
	private readonly StrategyParam<decimal> _sellStopLossPoints;
	private readonly StrategyParam<decimal> _buyTakeProfitPoints;
	private readonly StrategyParam<decimal> _sellTakeProfitPoints;
	private readonly StrategyParam<decimal> _buyTrailingPoints;
	private readonly StrategyParam<decimal> _sellTrailingPoints;
	private readonly StrategyParam<bool> _useTradingHours;
	private readonly StrategyParam<int> _fromHour;
	private readonly StrategyParam<int> _toHour;

	private decimal? _fastCloseEma;
	private decimal? _fastOpenEma;
	private decimal? _slowCloseEma;
	private decimal? _prevFastClose;
	private decimal? _prevFastOpen;
	private decimal? _prevSlowClose;
	private decimal? _prevLowerLarge;
	private decimal? _prevLowerMedium;
	private decimal? _prevLowerSmall;
	private decimal? _prevUpperSmall;
	private decimal? _prevUpperMedium;
	private decimal? _prevUpperLarge;

	private decimal _fastAlpha;
	private decimal _slowAlpha;
	private int _samples;

	private Order _stopOrder;
	private Order _takeProfitOrder;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChannelsStrategy"/> class.
	/// </summary>
	public ChannelsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for the envelope calculations.", "General");

		_volume = Param(nameof(Volume), 0.1m)
			.SetDisplay("Volume", "Lot size used for market orders.", "General");

		_fastPeriod = Param(nameof(FastPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Period for the fast exponential moving averages.", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 220)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Period for the envelope base EMA.", "Indicators");

		_envelopeLarge = Param(nameof(EnvelopeLargePercent), 1.0m)
			.SetDisplay("Envelope 1%", "Outer envelope deviation in percent.", "Indicators");

		_envelopeMedium = Param(nameof(EnvelopeMediumPercent), 0.7m)
			.SetDisplay("Envelope 0.7%", "Middle envelope deviation in percent.", "Indicators");

		_envelopeSmall = Param(nameof(EnvelopeSmallPercent), 0.3m)
			.SetDisplay("Envelope 0.3%", "Inner envelope deviation in percent.", "Indicators");

		_buyStopLossPoints = Param(nameof(BuyStopLossPoints), 0m)
			.SetDisplay("Buy Stop-Loss", "Distance in points for long stop-loss orders.", "Risk");

		_sellStopLossPoints = Param(nameof(SellStopLossPoints), 0m)
			.SetDisplay("Sell Stop-Loss", "Distance in points for short stop-loss orders.", "Risk");

		_buyTakeProfitPoints = Param(nameof(BuyTakeProfitPoints), 0m)
			.SetDisplay("Buy Take-Profit", "Distance in points for long take-profit orders.", "Risk");

		_sellTakeProfitPoints = Param(nameof(SellTakeProfitPoints), 0m)
			.SetDisplay("Sell Take-Profit", "Distance in points for short take-profit orders.", "Risk");

		_buyTrailingPoints = Param(nameof(BuyTrailingPoints), 30m)
			.SetDisplay("Buy Trailing", "Trailing distance in points for long positions.", "Risk");

		_sellTrailingPoints = Param(nameof(SellTrailingPoints), 30m)
			.SetDisplay("Sell Trailing", "Trailing distance in points for short positions.", "Risk");

		_useTradingHours = Param(nameof(UseTradingHours), false)
			.SetDisplay("Use Trading Hours", "Restrict trading to the configured time window.", "Trading");

		_fromHour = Param(nameof(FromHour), 0)
			.SetDisplay("From Hour", "Hour of day when trading is allowed to start.", "Trading");

		_toHour = Param(nameof(ToHour), 23)
			.SetDisplay("To Hour", "Hour of day when trading must stop.", "Trading");
	}

	/// <summary>
	/// Trading timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Outer envelope deviation in percent.
	/// </summary>
	public decimal EnvelopeLargePercent
	{
		get => _envelopeLarge.Value;
		set => _envelopeLarge.Value = value;
	}

	/// <summary>
	/// Middle envelope deviation in percent.
	/// </summary>
	public decimal EnvelopeMediumPercent
	{
		get => _envelopeMedium.Value;
		set => _envelopeMedium.Value = value;
	}

	/// <summary>
	/// Inner envelope deviation in percent.
	/// </summary>
	public decimal EnvelopeSmallPercent
	{
		get => _envelopeSmall.Value;
		set => _envelopeSmall.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades in price points.
	/// </summary>
	public decimal BuyStopLossPoints
	{
		get => _buyStopLossPoints.Value;
		set => _buyStopLossPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades in price points.
	/// </summary>
	public decimal SellStopLossPoints
	{
		get => _sellStopLossPoints.Value;
		set => _sellStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long trades in price points.
	/// </summary>
	public decimal BuyTakeProfitPoints
	{
		get => _buyTakeProfitPoints.Value;
		set => _buyTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short trades in price points.
	/// </summary>
	public decimal SellTakeProfitPoints
	{
		get => _sellTakeProfitPoints.Value;
		set => _sellTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing distance in points for long positions.
	/// </summary>
	public decimal BuyTrailingPoints
	{
		get => _buyTrailingPoints.Value;
		set => _buyTrailingPoints.Value = value;
	}

	/// <summary>
	/// Trailing distance in points for short positions.
	/// </summary>
	public decimal SellTrailingPoints
	{
		get => _sellTrailingPoints.Value;
		set => _sellTrailingPoints.Value = value;
	}

	/// <summary>
	/// Enables the trading window restriction.
	/// </summary>
	public bool UseTradingHours
	{
		get => _useTradingHours.Value;
		set => _useTradingHours.Value = value;
	}

	/// <summary>
	/// Allowed trading start hour.
	/// </summary>
	public int FromHour
	{
		get => _fromHour.Value;
		set => _fromHour.Value = value;
	}

	/// <summary>
	/// Allowed trading end hour.
	/// </summary>
	public int ToHour
	{
		get => _toHour.Value;
		set => _toHour.Value = value;
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

		_fastCloseEma = null;
		_fastOpenEma = null;
		_slowCloseEma = null;
		_prevFastClose = null;
		_prevFastOpen = null;
		_prevSlowClose = null;
		_prevLowerLarge = null;
		_prevLowerMedium = null;
		_prevLowerSmall = null;
		_prevUpperSmall = null;
		_prevUpperMedium = null;
		_prevUpperLarge = null;
		_stopOrder = null;
		_takeProfitOrder = null;
		_samples = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastAlpha = CalculateAlpha(FastPeriod);
		_slowAlpha = CalculateAlpha(SlowPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		CancelProtection();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			CancelProtection();
			return;
		}

		if (Position > 0)
		{
			EnsureProtection(true);
		}
		else
		{
			EnsureProtection(false);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_samples++;

		// Update EMA streams for the candle close, candle open and the slow baseline.
		var fastClose = UpdateEma(candle.ClosePrice, ref _fastCloseEma, _fastAlpha);
		var fastOpen = UpdateEma(candle.OpenPrice, ref _fastOpenEma, _fastAlpha);
		var slowClose = UpdateEma(candle.ClosePrice, ref _slowCloseEma, _slowAlpha);

		// Convert deviations from percent to decimal multipliers.
		var largeFactor = EnvelopeLargePercent / 100m;
		var mediumFactor = EnvelopeMediumPercent / 100m;
		var smallFactor = EnvelopeSmallPercent / 100m;

		// Recreate the envelope values that were used inside the original MQL strategy.
		var lowerLarge = slowClose * (1m - largeFactor);
		var lowerMedium = slowClose * (1m - mediumFactor);
		var lowerSmall = slowClose * (1m - smallFactor);
		var upperSmall = slowClose * (1m + smallFactor);
		var upperMedium = slowClose * (1m + mediumFactor);
		var upperLarge = slowClose * (1m + largeFactor);

		if (_samples <= SlowPeriod)
		{
			StorePrevious(fastClose, fastOpen, slowClose, lowerLarge, lowerMedium, lowerSmall, upperSmall, upperMedium, upperLarge);
			return;
		}

		if (_prevFastClose is null || _prevFastOpen is null || _prevSlowClose is null)
		{
			StorePrevious(fastClose, fastOpen, slowClose, lowerLarge, lowerMedium, lowerSmall, upperSmall, upperMedium, upperLarge);
			return;
		}

		var canTrade = IsFormedAndOnlineAndAllowTrading() && IsWithinTradingHours(candle.OpenTime);
		if (canTrade && Position == 0)
		{
			// Buy entries follow the same six envelope cross checks from the MQL code.
			var buySignal =
				(fastClose > lowerLarge && _prevFastClose < _prevLowerLarge) ||
				(fastClose > lowerMedium && _prevFastClose < _prevLowerMedium) ||
				(fastClose < lowerSmall && _prevFastClose < _prevLowerSmall) ||
				(fastClose > slowClose && _prevFastClose < _prevSlowClose) ||
				(fastClose > upperSmall && _prevFastClose < _prevUpperSmall) ||
				(fastClose > upperMedium && _prevFastClose < _prevUpperMedium);

			if (buySignal)
			{
				// Only a single market position is allowed, just like the source expert advisor.
				BuyMarket(Volume);
			}

			// Sell entries mirror the six exit checks performed on the fast open EMA.
			var sellSignal =
				(fastOpen < upperLarge && _prevFastOpen > _prevUpperLarge) ||
				(fastOpen < upperMedium && _prevFastOpen > _prevUpperMedium) ||
				(fastOpen < upperSmall && _prevFastOpen > _prevUpperSmall) ||
				(fastOpen < slowClose && _prevFastOpen > _prevSlowClose) ||
				(fastOpen < lowerSmall && _prevFastOpen > _prevLowerSmall) ||
				(fastOpen < lowerMedium && _prevFastOpen > _prevLowerMedium);

			if (sellSignal)
			{
				SellMarket(Volume);
			}
		}

		if (Position > 0)
		{
			// Update the trailing stop for long positions when the closing price advances.
			UpdateTrailing(true, candle.ClosePrice);
		}
		else if (Position < 0)
		{
			// Update the trailing stop for short positions when the closing price declines.
			UpdateTrailing(false, candle.ClosePrice);
		}

		StorePrevious(fastClose, fastOpen, slowClose, lowerLarge, lowerMedium, lowerSmall, upperSmall, upperMedium, upperLarge);
	}

	private void StorePrevious(decimal fastClose, decimal fastOpen, decimal slowClose, decimal lowerLarge, decimal lowerMedium, decimal lowerSmall, decimal upperSmall, decimal upperMedium, decimal upperLarge)
	{
		_prevFastClose = fastClose;
		_prevFastOpen = fastOpen;
		_prevSlowClose = slowClose;
		_prevLowerLarge = lowerLarge;
		_prevLowerMedium = lowerMedium;
		_prevLowerSmall = lowerSmall;
		_prevUpperSmall = upperSmall;
		_prevUpperMedium = upperMedium;
		_prevUpperLarge = upperLarge;
	}

	private void UpdateTrailing(bool isLong, decimal price)
	{
		var trailingPoints = isLong ? BuyTrailingPoints : SellTrailingPoints;
		if (trailingPoints <= 0 || PositionPrice is null)
			return;

		var step = GetPointValue();
		if (step <= 0)
			return;

		var trailingDistance = trailingPoints * step;
		if (isLong)
		{
			var desiredStop = price - trailingDistance;
			if (_stopOrder == null || desiredStop > _stopOrder.Price)
				MoveStop(Sides.Sell, desiredStop);
		}
		else
		{
			var desiredStop = price + trailingDistance;
			if (_stopOrder == null || desiredStop < _stopOrder.Price)
				MoveStop(Sides.Buy, desiredStop);
		}
	}

	private void EnsureProtection(bool isLong)
	{
		var step = GetPointValue();
		if (step <= 0 || PositionPrice is not decimal entryPrice)
			return;

		if (isLong)
		{
			// Replicate the optional long stop-loss and take-profit levels.
			if (BuyStopLossPoints > 0)
			{
				var stopPrice = entryPrice - BuyStopLossPoints * step;
				if (_stopOrder == null)
					MoveStop(Sides.Sell, stopPrice);
			}

			if (BuyTakeProfitPoints > 0 && _takeProfitOrder == null)
			{
				var takePrice = entryPrice + BuyTakeProfitPoints * step;
				_takeProfitOrder = SellLimit(Math.Abs(Position), takePrice);
			}
		}
		else
		{
			// Replicate the optional short stop-loss and take-profit levels.
			if (SellStopLossPoints > 0)
			{
				var stopPrice = entryPrice + SellStopLossPoints * step;
				if (_stopOrder == null)
					MoveStop(Sides.Buy, stopPrice);
			}

			if (SellTakeProfitPoints > 0 && _takeProfitOrder == null)
			{
				var takePrice = entryPrice - SellTakeProfitPoints * step;
				_takeProfitOrder = BuyLimit(Math.Abs(Position), takePrice);
			}
		}
	}

	private void MoveStop(Sides side, decimal price)
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = side == Sides.Sell
			? SellStop(Math.Abs(Position), price)
			: BuyStop(Math.Abs(Position), price);
	}

	private void CancelProtection()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_stopOrder = null;
		_takeProfitOrder = null;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		if (!UseTradingHours)
			return true;

		var hour = time.Hour;
		if (FromHour <= ToHour)
			return hour >= FromHour && hour <= ToHour;

		return hour >= FromHour || hour <= ToHour;
	}

	private static decimal CalculateAlpha(int length)
	{
		return 2m / (length + 1m);
	}

	private static decimal UpdateEma(decimal value, ref decimal? ema, decimal alpha)
	{
		if (ema is null)
		{
			ema = value;
		}
		else
		{
			ema = alpha * value + (1m - alpha) * ema.Value;
		}

		return ema.Value;
	}

	private decimal GetPointValue()
	{
		return Security?.PriceStep ?? 0m;
	}
}
