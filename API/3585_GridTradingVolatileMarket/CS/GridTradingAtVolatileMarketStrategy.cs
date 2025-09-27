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
/// Grid trading strategy converted from the MetaTrader expert "Gridtrading_at_volatile_market.mq4".
/// Uses Donchian channel touches on a higher timeframe combined with engulfing patterns on the trading timeframe.
/// Adds averaging orders using a geometric progression and exits on portfolio level profit or drawdown limits.
/// </summary>
public class GridTradingAtVolatileMarketStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitFactor;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<decimal> _gridMultiplier;
	private readonly StrategyParam<decimal> _baseOrderVolume;
	private readonly StrategyParam<decimal> _maxDrawdownFraction;
	private readonly StrategyParam<DataType> _candleType;

	private ATR _atr;
	private SMA _slowMa;
	private RSI _rsi;
	private DonchianChannels _donchian;

	private decimal? _atrValue;
	private decimal? _slowMaValue;
	private decimal? _donchianUpper;
	private decimal? _donchianLower;

	private bool _upTouch;
	private bool _downTouch;
	private Sides? _gridDirection;
	private decimal _gridAnchorPrice;
	private decimal _initialOrderVolume;
	private decimal _totalGridVolume;
	private int _gridStepIndex;

	private ICandleMessage _previousCandle;
	private ICandleMessage _previousPreviousCandle;

	private DataType _higherCandleType;

	/// <summary>
	/// Required profit factor relative to invested volume before closing the grid.
	/// </summary>
	public decimal TakeProfitFactor
	{
		get => _takeProfitFactor.Value;
		set => _takeProfitFactor.Value = value;
	}

	/// <summary>
	/// Length of the slow moving average calculated on the higher timeframe.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Multiplier applied to each additional averaging order.
	/// </summary>
	public decimal GridMultiplier
	{
		get => _gridMultiplier.Value;
		set => _gridMultiplier.Value = value;
	}

	/// <summary>
	/// Base volume for the first market order in the grid.
	/// </summary>
	public decimal BaseOrderVolume
	{
		get => _baseOrderVolume.Value;
		set => _baseOrderVolume.Value = value;
	}

	/// <summary>
	/// Maximum fractional drawdown of the initial portfolio allowed for the grid.
	/// </summary>
	public decimal MaxDrawdownFraction
	{
		get => _maxDrawdownFraction.Value;
		set => _maxDrawdownFraction.Value = value;
	}

	/// <summary>
	/// Trading timeframe used for signals and grid management.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public GridTradingAtVolatileMarketStrategy()
	{
		_takeProfitFactor = Param(nameof(TakeProfitFactor), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit Factor", "Profit multiple of invested volume required to close the grid", "Risk");

		_slowMaLength = Param(nameof(SlowMaLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA Length", "Slow moving average length on the higher timeframe", "Indicators");

		_gridMultiplier = Param(nameof(GridMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Grid Multiplier", "Multiplier applied to each additional averaging order", "Grid");

		_baseOrderVolume = Param(nameof(BaseOrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Volume of the first order in the averaging grid", "Grid");

		_maxDrawdownFraction = Param(nameof(MaxDrawdownFraction), 0.8m)
		.SetRange(0.1m, 1m)
		.SetDisplay("Max Drawdown", "Fraction of initial portfolio value tolerated before forced exit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Trading timeframe for signal detection", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (CandleType.Arg is TimeSpan span && span > TimeSpan.Zero)
		{
			var higherFrame = GetBiggerTimeFrame(span);
			yield return (Security, DataType.TimeFrame(higherFrame));
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_atrValue = null;
		_slowMaValue = null;
		_donchianUpper = null;
		_donchianLower = null;

		_upTouch = false;
		_downTouch = false;
		_gridDirection = null;
		_gridAnchorPrice = 0m;
		_initialOrderVolume = 0m;
		_totalGridVolume = 0m;
		_gridStepIndex = 1;

		_previousCandle = null;
		_previousPreviousCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (CandleType.Arg is not TimeSpan baseSpan || baseSpan <= TimeSpan.Zero)
		throw new InvalidOperationException("CandleType must contain a positive TimeSpan argument.");

		var higherSpan = GetBiggerTimeFrame(baseSpan);
		_higherCandleType = DataType.TimeFrame(higherSpan);

		_atr = new ATR { Length = 20 };
		_slowMa = new SMA { Length = SlowMaLength };
		_rsi = new RSI { Length = 14 };
		_donchian = new DonchianChannels { Length = 20 };

		var higherSubscription = SubscribeCandles(_higherCandleType);
		higherSubscription
		.Bind(_atr, _slowMa, ProcessHigher)
		.BindEx(_donchian, ProcessDonchian)
		.Start();

		var tradingSubscription = SubscribeCandles(CandleType);
		tradingSubscription
		.Bind(_rsi, ProcessTradingCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradingSubscription);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessHigher(ICandleMessage candle, decimal atrValue, decimal slowMaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_atr.IsFormed || !_slowMa.IsFormed)
		return;

		// Cache higher timeframe ATR and SMA values for trading decisions.
		_atrValue = atrValue;
		_slowMaValue = slowMaValue;
	}

	private void ProcessDonchian(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsFinal)
		return;

		if (value is not DonchianChannelsValue dcValue)
		return;

		if (dcValue.UpperBand is not decimal upper ||
		dcValue.LowerBand is not decimal lower)
		return;

		// Store Donchian upper and lower boundaries from the higher timeframe.
		_donchianUpper = upper;
		_donchianLower = lower;
	}

	private void ProcessTradingCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdatePreviousCandles(candle);
			return;
		}

		if (!_rsi.IsFormed ||
		_atrValue is not decimal atr ||
		_slowMaValue is not decimal slow ||
		_donchianUpper is not decimal upper ||
		_donchianLower is not decimal lower)
		{
			UpdatePreviousCandles(candle);
			return;
		}

		if (_previousCandle is null || _previousPreviousCandle is null)
		{
			UpdatePreviousCandles(candle);
			return;
		}

		var bullishEngulfing = _previousCandle.ClosePrice > _previousPreviousCandle.HighPrice &&
		_previousCandle.LowPrice < _previousPreviousCandle.LowPrice;

		var bearishEngulfing = _previousCandle.ClosePrice < _previousPreviousCandle.LowPrice &&
		_previousCandle.HighPrice > _previousPreviousCandle.HighPrice;

		// Track Donchian touch states to detect alternating breakouts.
		if (!_upTouch && !_downTouch)
		{
			_upTouch = _previousCandle.HighPrice > upper;
			_downTouch = _previousCandle.LowPrice < lower;
		}
		else if (_upTouch)
		{
			_downTouch = _previousCandle.LowPrice < lower;
			if (_downTouch)
			_upTouch = false;
		}
		else if (_downTouch)
		{
			_upTouch = _previousCandle.HighPrice > upper;
			if (_upTouch)
			_downTouch = false;
		}

		var buySignal = _downTouch && bullishEngulfing;
		var sellSignal = _upTouch && bearishEngulfing;

		if (_gridDirection is null)
		{
			TryStartGrid(buySignal, sellSignal, rsiValue, slow, candle);
		}
		else
		{
			ManageActiveGrid(candle, atr);
		}

		UpdatePreviousCandles(candle);
	}

	private void TryStartGrid(bool buySignal, bool sellSignal, decimal rsiValue, decimal slowMaValue, ICandleMessage candle)
	{
		// Optional trend filter similar to the original expert inputs.
		var trendUp = candle.ClosePrice > slowMaValue && rsiValue < 35m;
		var trendDown = candle.ClosePrice < slowMaValue && rsiValue > 65m;

		if (buySignal && trendUp && Position <= 0)
		{
			var volume = BaseOrderVolume;
			var price = GetAskPrice();
			if (price <= 0m)
			price = candle.ClosePrice;

			BuyMarket(volume);
			InitializeGridState(Sides.Buy, price, volume);
			LogInfo($"Started buy grid at {price} with volume {volume}");
		}
		else if (sellSignal && trendDown && Position >= 0)
		{
			var volume = BaseOrderVolume;
			var price = GetBidPrice();
			if (price <= 0m)
			price = candle.ClosePrice;

			SellMarket(volume);
			InitializeGridState(Sides.Sell, price, volume);
			LogInfo($"Started sell grid at {price} with volume {volume}");
		}
	}

	private void InitializeGridState(Sides direction, decimal anchorPrice, decimal volume)
	{
		_gridDirection = direction;
		_gridAnchorPrice = anchorPrice;
		_initialOrderVolume = volume;
		_totalGridVolume = volume;
		_gridStepIndex = 1;
	}

	private void ManageActiveGrid(ICandleMessage candle, decimal atr)
	{
		if (_gridDirection is null)
		return;

		if (_gridDirection == Sides.Buy)
		{
			var threshold = _gridAnchorPrice - _gridStepIndex * 2m * atr;
			if (candle.LowPrice <= threshold)
			{
				var volume = CalculateGridVolume();
				BuyMarket(volume);
				_totalGridVolume += volume;
				_gridStepIndex++;
				LogInfo($"Added buy grid level {_gridStepIndex - 1} at {candle.LowPrice} with volume {volume}");
			}
		}
		else if (_gridDirection == Sides.Sell)
		{
			var threshold = _gridAnchorPrice + _gridStepIndex * 2m * atr;
			if (candle.HighPrice >= threshold)
			{
				var volume = CalculateGridVolume();
				SellMarket(volume);
				_totalGridVolume += volume;
				_gridStepIndex++;
				LogInfo($"Added sell grid level {_gridStepIndex - 1} at {candle.HighPrice} with volume {volume}");
			}
		}

		CheckExitConditions();
	}

	private decimal CalculateGridVolume()
	{
		var multiplierPower = (decimal)Math.Pow((double)GridMultiplier, _gridStepIndex);
		return _initialOrderVolume * multiplierPower;
	}

	private void CheckExitConditions()
	{
		var totalPnL = CalculateTotalPnL();
		if (totalPnL is not decimal pnl)
		return;

		var profitTarget = _totalGridVolume * TakeProfitFactor;
		if (pnl >= profitTarget)
		{
			LogInfo($"Closing grid due to profit target: {pnl} >= {profitTarget}");
			CloseGrid();
			return;
		}

		if (Portfolio?.BeginValue is decimal startValue && startValue > 0m)
		{
			var lossLimit = -startValue * MaxDrawdownFraction;
			if (pnl <= lossLimit)
			{
				LogInfo($"Closing grid due to drawdown: {pnl} <= {lossLimit}");
				CloseGrid();
			}
		}
	}

	private decimal? CalculateTotalPnL()
	{
		var realizedPnL = PnL;

		if (Position == 0m)
		return realizedPnL;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m)
		return null;

		var marketPrice = _gridDirection == Sides.Sell ? GetAskPrice() : GetBidPrice();
		if (marketPrice <= 0m)
		return null;

		var averagePrice = Position.AveragePrice;
		if (averagePrice is null)
		return realizedPnL;

		var diff = marketPrice - averagePrice.Value;
		var steps = diff / priceStep;
		var openPnL = steps * stepPrice * Position;

		return realizedPnL + openPnL;
	}

	private void CloseGrid()
	{
		CancelActiveOrders();
		ClosePosition();

		_gridDirection = null;
		_totalGridVolume = 0m;
		_gridStepIndex = 1;
		_upTouch = false;
		_downTouch = false;
	}

	private void UpdatePreviousCandles(ICandleMessage candle)
	{
		_previousPreviousCandle = _previousCandle;
		_previousCandle = candle;
	}

	private static TimeSpan GetBiggerTimeFrame(TimeSpan baseFrame)
	{
		var minutes = baseFrame.TotalMinutes;

		return minutes switch
		{
			<= 1 => TimeSpan.FromMinutes(5),
			<= 5 => TimeSpan.FromMinutes(15),
			<= 15 => TimeSpan.FromMinutes(30),
			<= 30 => TimeSpan.FromMinutes(60),
			<= 60 => TimeSpan.FromHours(4),
			<= 240 => TimeSpan.FromDays(1),
			_ => TimeSpan.FromDays(1),
		};
	}

	private decimal GetBidPrice()
	{
		if (Security?.BestBid != null)
		return Security.BestBid.Price;

		if (Security?.LastPrice is decimal last)
		return last;

		return 0m;
	}

	private decimal GetAskPrice()
	{
		if (Security?.BestAsk != null)
		return Security.BestAsk.Price;

		if (Security?.LastPrice is decimal last)
		return last;

		return 0m;
	}
}

