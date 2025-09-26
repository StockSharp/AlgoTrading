namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Conversion of the "Extreme EA" expert advisor using StockSharp high level API.
/// </summary>
public class ExtremeEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<int> _historyDays;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciUpperLevel;
	private readonly StrategyParam<decimal> _cciLowerLevel;
	private readonly StrategyParam<DataType> _maCandleType;
	private readonly StrategyParam<DataType> _cciCandleType;
	private readonly StrategyParam<MaMethod> _maMethod;
	private readonly StrategyParam<AppliedPriceMode> _maPriceMode;
	private readonly StrategyParam<AppliedPriceMode> _cciPriceMode;

	private LengthIndicator<decimal> _fastMa;
	private LengthIndicator<decimal> _slowMa;
	private CommodityChannelIndex _cci;

	private decimal? _fastMaCurrent;
	private decimal? _fastMaPrevious;

	private decimal? _slowMaCurrent;
	private decimal? _slowMaPrevious;
	private decimal? _slowMaPrevious2;

	private decimal? _latestCciValue;

	private int _consecutiveLosses;
	private decimal _signedPosition;
	private Sides? _lastEntrySide;
	private decimal _lastEntryPrice;
	private decimal _lastExitPrice;
	private DateTimeOffset? _lastClosedTradeTime;

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public ExtremeEaStrategy()
	{
		_maximumRisk = Param(nameof(MaximumRisk), 0.05m)
			.SetNotNegative()
			.SetDisplay("Maximum risk", "Risk allocated per trade as a fraction of portfolio equity.", "Money management");

		_decreaseFactor = Param(nameof(DecreaseFactor), 6m)
			.SetNotNegative()
			.SetDisplay("Decrease factor", "Reduction factor applied after consecutive losses.", "Money management");

		_historyDays = Param(nameof(HistoryDays), 60)
			.SetNotNegative()
			.SetDisplay("History days", "Number of days kept when tracking the loss streak.", "Money management");

		_maxPositions = Param(nameof(MaxPositions), 3)
			.SetNotNegative()
			.SetDisplay("Max positions", "Maximum number of simultaneous entries per direction.", "Risk");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA period", "Lookback for the fast moving average.", "Indicator");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 75)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA period", "Lookback for the slow moving average.", "Indicator");

		_cciPeriod = Param(nameof(CciPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("CCI period", "Lookback for the Commodity Channel Index.", "Indicator");

		_cciUpperLevel = Param(nameof(CciUpperLevel), 50m)
			.SetDisplay("CCI upper level", "Upper threshold used for sell signals.", "Indicator");

		_cciLowerLevel = Param(nameof(CciLowerLevel), -50m)
			.SetDisplay("CCI lower level", "Lower threshold used for buy signals.", "Indicator");

		_maCandleType = Param(nameof(MaCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("MA timeframe", "Timeframe driving the moving averages and trade execution.", "Data");

		_cciCandleType = Param(nameof(CciCandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("CCI timeframe", "Timeframe used to evaluate the CCI filter.", "Data");

		_maMethod = Param(nameof(MaMethod), MaMethod.Exponential)
			.SetDisplay("MA method", "Smoothing method applied to both moving averages.", "Indicator");

		_maPriceMode = Param(nameof(MaPriceMode), AppliedPriceMode.Median)
			.SetDisplay("MA price", "Price source supplied to the moving averages.", "Indicator");

		_cciPriceMode = Param(nameof(CciPriceMode), AppliedPriceMode.Typical)
			.SetDisplay("CCI price", "Price source supplied to the CCI indicator.", "Indicator");
	}

	/// <summary>
	/// Maximum share of equity risked per trade.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Reduction factor for the position size after a loss streak.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Number of days considered when tracking losses.
	/// </summary>
	public int HistoryDays
	{
		get => _historyDays.Value;
		set => _historyDays.Value = value;
	}

	/// <summary>
	/// Maximum simultaneous positions per direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// CCI indicator length.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Upper CCI threshold for sell entries.
	/// </summary>
	public decimal CciUpperLevel
	{
		get => _cciUpperLevel.Value;
		set => _cciUpperLevel.Value = value;
	}

	/// <summary>
	/// Lower CCI threshold for buy entries.
	/// </summary>
	public decimal CciLowerLevel
	{
		get => _cciLowerLevel.Value;
		set => _cciLowerLevel.Value = value;
	}

	/// <summary>
	/// Timeframe used for moving averages and execution.
	/// </summary>
	public DataType MaCandleType
	{
		get => _maCandleType.Value;
		set => _maCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used to compute the CCI filter.
	/// </summary>
	public DataType CciCandleType
	{
		get => _cciCandleType.Value;
		set => _cciCandleType.Value = value;
	}

	/// <summary>
	/// Moving average smoothing method.
	/// </summary>
	public MaMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price source for moving averages.
	/// </summary>
	public AppliedPriceMode MaPriceMode
	{
		get => _maPriceMode.Value;
		set => _maPriceMode.Value = value;
	}

	/// <summary>
	/// Price source for the CCI indicator.
	/// </summary>
	public AppliedPriceMode CciPriceMode
	{
		get => _cciPriceMode.Value;
		set => _cciPriceMode.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;
		if (security == null)
			yield break;

		yield return (security, MaCandleType);

		if (CciCandleType != MaCandleType)
			yield return (security, CciCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMaCurrent = null;
		_fastMaPrevious = null;
		_slowMaCurrent = null;
		_slowMaPrevious = null;
		_slowMaPrevious2 = null;
		_latestCciValue = null;
		_consecutiveLosses = 0;
		_signedPosition = 0m;
		_lastEntrySide = null;
		_lastEntryPrice = 0m;
		_lastExitPrice = 0m;
		_lastClosedTradeTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = CreateMovingAverage(MaMethod, FastMaPeriod);
		_slowMa = CreateMovingAverage(MaMethod, SlowMaPeriod);
		_cci = new CommodityChannelIndex { Length = CciPeriod };

		var maSubscription = SubscribeCandles(MaCandleType);

		if (CciCandleType == MaCandleType)
		{
			maSubscription
				.Bind(ProcessCciCandle)
				.Bind(ProcessMaCandle)
				.Start();
		}
		else
		{
			maSubscription
				.Bind(ProcessMaCandle)
				.Start();

			var cciSubscription = SubscribeCandles(CciCandleType);
			cciSubscription
				.Bind(ProcessCciCandle)
				.Start();
		}

		StartProtection();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, maSubscription);
			if (_fastMa != null)
				DrawIndicator(priceArea, _fastMa);
			if (_slowMa != null)
				DrawIndicator(priceArea, _slowMa);
			DrawOwnTrades(priceArea);
		}

		var cciArea = CreateChartArea("CCI");
		if (cciArea != null && _cci != null)
			DrawIndicator(cciArea, _cci);
	}

	private void ProcessMaCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastValue = ProcessMovingAverage(_fastMa, candle);
		var slowValue = ProcessMovingAverage(_slowMa, candle);

		if (fastValue is not decimal currentFast || slowValue is not decimal currentSlow)
			return;

		_fastMaPrevious = _fastMaCurrent;
		_fastMaCurrent = currentFast;

		_slowMaPrevious2 = _slowMaPrevious;
		_slowMaPrevious = _slowMaCurrent;
		_slowMaCurrent = currentSlow;

		if (_fastMaPrevious is not decimal previousFast ||
			_slowMaPrevious is not decimal previousSlow ||
			_slowMaPrevious2 is not decimal olderSlow)
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_latestCciValue is not decimal cciValue)
			return;

		var slowIsRising = previousSlow > olderSlow;
		var slowIsFalling = previousSlow < olderSlow;
		var fastIsRising = currentFast > previousFast;
		var fastIsFalling = currentFast < previousFast;

		var shouldBuy = slowIsRising && fastIsRising && cciValue < CciLowerLevel;
		var shouldSell = slowIsFalling && fastIsFalling && cciValue > CciUpperLevel;

		var price = GetDecisionPrice(candle);
		var volume = CalculateTradeVolume(price);
		if (volume <= 0m)
			return;

		decimal? maxExposure = MaxPositions > 0 ? volume * MaxPositions : null;
		var longExposure = Math.Max(Position, 0m);
		var shortExposure = Math.Max(-Position, 0m);
		var tolerance = GetVolumeTolerance();

		if (shouldBuy)
		{
			var canIncreaseLong = maxExposure == null || longExposure + volume <= maxExposure.Value + tolerance;
			if (canIncreaseLong)
				BuyMarket(volume);
		}
		else if (!slowIsRising && longExposure > 0m)
		{
			SellMarket(longExposure);
		}

		if (shouldSell)
		{
			var canIncreaseShort = maxExposure == null || shortExposure + volume <= maxExposure.Value + tolerance;
			if (canIncreaseShort)
				SellMarket(volume);
		}
		else if (!slowIsFalling && shortExposure > 0m)
		{
			BuyMarket(shortExposure);
		}
	}

	private void ProcessCciCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cci == null)
			return;

		var price = GetPrice(candle, CciPriceMode);
		var indicatorValue = _cci.Process(price, candle.OpenTime, true);

		if (!indicatorValue.IsFinal || !_cci.IsFormed)
			return;

		_latestCciValue = indicatorValue.ToDecimal();
	}

	private decimal? ProcessMovingAverage(LengthIndicator<decimal> indicator, ICandleMessage candle)
	{
		if (indicator == null)
			return null;

		var price = GetPrice(candle, MaPriceMode);
		var value = indicator.Process(price, candle.OpenTime, true);

		if (!indicator.IsFormed)
			return null;

		return value.ToDecimal();
	}

	private decimal CalculateTradeVolume(decimal price)
	{
		var baseVolume = Volume > 0m ? Volume : 1m;

		if (price <= 0m)
			return NormalizeVolume(baseVolume);

		var portfolio = Portfolio;
		var equity = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
		if (equity <= 0m)
			return NormalizeVolume(baseVolume);

		var volume = equity * MaximumRisk / price;

		if (DecreaseFactor > 0m && _consecutiveLosses > 1)
		{
			var reduction = volume * _consecutiveLosses / DecreaseFactor;
			volume -= reduction;
		}

		if (volume <= 0m)
			volume = baseVolume;

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security != null)
		{
			var step = security.VolumeStep ?? 1m;
			if (step <= 0m)
				step = 1m;

			var minVolume = security.MinVolume ?? step;
			var maxVolume = security.MaxVolume;

			var steps = decimal.Floor(volume / step);
			if (steps < 1m)
				steps = 1m;

			volume = steps * step;

			if (volume < minVolume)
				volume = minVolume;

			if (maxVolume is decimal max && max > 0m && volume > max)
				volume = max;
		}

		if (volume <= 0m)
			volume = 1m;

		return volume;
	}

	private decimal GetDecisionPrice(ICandleMessage candle)
	{
		var security = Security;
		if (security?.LastPrice is decimal lastPrice && lastPrice > 0m)
			return lastPrice;

		if (candle.ClosePrice > 0m)
			return candle.ClosePrice;

		return candle.OpenPrice;
	}

	private decimal GetVolumeTolerance()
	{
		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
			return step / 2m;

		return 0m;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MaMethod method, int length)
	{
		return method switch
		{
			MaMethod.Simple => new SimpleMovingAverage { Length = length },
			MaMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MaMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MaMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length }
		};
	}

	private static decimal GetPrice(ICandleMessage candle, AppliedPriceMode priceMode)
	{
		return priceMode switch
		{
			AppliedPriceMode.Close => candle.ClosePrice,
			AppliedPriceMode.Open => candle.OpenPrice,
			AppliedPriceMode.High => candle.HighPrice,
			AppliedPriceMode.Low => candle.LowPrice,
			AppliedPriceMode.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceMode.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceMode.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		if (order == null)
			return;

		var delta = trade.Volume * (order.Side == Sides.Buy ? 1m : -1m);
		var previousPosition = _signedPosition;
		_signedPosition += delta;

		if (previousPosition == 0m && _signedPosition != 0m)
		{
			_lastEntrySide = order.Side;
			_lastEntryPrice = trade.Trade.Price;
		}
		else if (previousPosition != 0m && _signedPosition == 0m)
		{
			_lastExitPrice = trade.Trade.Price;

			if (_lastEntrySide != null && _lastEntryPrice != 0m)
			{
				var profit = _lastEntrySide == Sides.Buy
					? _lastExitPrice - _lastEntryPrice
					: _lastEntryPrice - _lastExitPrice;

				if (_lastClosedTradeTime is DateTimeOffset lastTime)
				{
					var limit = TimeSpan.FromDays(Math.Max(HistoryDays, 0));
					if (limit > TimeSpan.Zero && trade.Trade.ServerTime - lastTime > limit)
						_consecutiveLosses = 0;
				}

				if (profit > 0m)
				{
					_consecutiveLosses = 0;
				}
				else if (profit < 0m)
				{
					_consecutiveLosses++;
				}
			}

			_lastEntrySide = null;
			_lastEntryPrice = 0m;
			_lastClosedTradeTime = trade.Trade.ServerTime;
		}
	}
}

/// <summary>
/// Moving average methods supported by the strategy.
/// </summary>
public enum MaMethod
{
	Simple,
	Exponential,
	Smoothed,
	LinearWeighted
}

/// <summary>
/// Price sources compatible with the indicators used by the strategy.
/// </summary>
public enum AppliedPriceMode
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted
}
