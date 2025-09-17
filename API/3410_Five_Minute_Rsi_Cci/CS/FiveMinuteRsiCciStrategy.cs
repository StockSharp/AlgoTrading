using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MetaTrader expert advisor "5Mins Rsi Cci EA".
/// Combines RSI momentum with smoothed/EMA filters and dual CCI confirmation on five-minute candles.
/// </summary>
public class FiveMinuteRsiCciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _fastSmmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _fastCciPeriod;
	private readonly StrategyParam<int> _slowCciPeriod;
	private readonly StrategyParam<decimal> _bullishRsiLevel;
	private readonly StrategyParam<decimal> _bearishRsiLevel;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _lotCoefficient;
	private readonly StrategyParam<decimal> _equityDivisor;
	private readonly StrategyParam<decimal> _maxSpreadPoints;

	private RelativeStrengthIndex? _rsi;
	private SmoothedMovingAverage? _fastSmma;
	private ExponentialMovingAverage? _slowEma;
	private CommodityChannelIndex? _fastCci;
	private CommodityChannelIndex? _slowCci;
	private decimal? _previousRsi;
	private decimal _pointValue;
	private decimal? _bestBid;
	private decimal? _bestAsk;

	/// <summary>
	/// Initializes a new instance of the <see cref="FiveMinuteRsiCciStrategy"/> class.
	/// </summary>
	public FiveMinuteRsiCciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for indicator calculations", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Number of candles used to calculate RSI", "Indicators")
			.SetCanOptimize(true);

		_fastSmmaPeriod = Param(nameof(FastSmmaPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMMA", "Length of the fast smoothed moving average applied to opens", "Indicators")
			.SetCanOptimize(true);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Length of the slow exponential moving average applied to opens", "Indicators")
			.SetCanOptimize(true);

		_fastCciPeriod = Param(nameof(FastCciPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("Fast CCI", "Length of the fast CCI calculated on typical price", "Indicators")
			.SetCanOptimize(true);

		_slowCciPeriod = Param(nameof(SlowCciPeriod), 175)
			.SetGreaterThanZero()
			.SetDisplay("Slow CCI", "Length of the slow CCI calculated on typical price", "Indicators")
			.SetCanOptimize(true);

		_bullishRsiLevel = Param(nameof(BullishRsiLevel), 55m)
			.SetDisplay("Bullish RSI", "RSI threshold that must be crossed upward to validate buys", "Signals")
			.SetCanOptimize(true);

		_bearishRsiLevel = Param(nameof(BearishRsiLevel), 45m)
			.SetDisplay("Bearish RSI", "RSI threshold that must be crossed downward to validate sells", "Signals")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 60m)
			.SetDisplay("Stop Loss (points)", "Fixed stop-loss distance expressed in MetaTrader points", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetDisplay("Take Profit (points)", "Fixed take-profit distance expressed in MetaTrader points", "Risk")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 20m)
			.SetDisplay("Trailing Stop (points)", "Trailing stop distance expressed in MetaTrader points", "Risk")
			.SetCanOptimize(true);

		_lotCoefficient = Param(nameof(LotCoefficient), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Coefficient", "Base coefficient used by the dynamic position sizing formula", "Money Management")
			.SetCanOptimize(true);

		_equityDivisor = Param(nameof(EquityDivisor), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Equity Divisor", "Divisor applied inside the square root for equity-based sizing", "Money Management")
			.SetCanOptimize(true);

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 18m)
			.SetDisplay("Max Spread (points)", "Maximum allowed bid-ask spread before opening a trade", "Filters")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type used for signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of candles used to calculate RSI.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Length of the fast smoothed moving average.
	/// </summary>
	public int FastSmmaPeriod
	{
		get => _fastSmmaPeriod.Value;
		set => _fastSmmaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow exponential moving average.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the fast CCI indicator.
	/// </summary>
	public int FastCciPeriod
	{
		get => _fastCciPeriod.Value;
		set => _fastCciPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow CCI indicator.
	/// </summary>
	public int SlowCciPeriod
	{
		get => _slowCciPeriod.Value;
		set => _slowCciPeriod.Value = value;
	}

	/// <summary>
	/// RSI level used to confirm long entries.
	/// </summary>
	public decimal BullishRsiLevel
	{
		get => _bullishRsiLevel.Value;
		set => _bullishRsiLevel.Value = value;
	}

	/// <summary>
	/// RSI level used to confirm short entries.
	/// </summary>
	public decimal BearishRsiLevel
	{
		get => _bearishRsiLevel.Value;
		set => _bearishRsiLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in MetaTrader points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Base coefficient for dynamic position sizing.
	/// </summary>
	public decimal LotCoefficient
	{
		get => _lotCoefficient.Value;
		set => _lotCoefficient.Value = value;
	}

	/// <summary>
	/// Divisor used in the equity-based sizing formula.
	/// </summary>
	public decimal EquityDivisor
	{
		get => _equityDivisor.Value;
		set => _equityDivisor.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread before new orders are blocked.
	/// </summary>
	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsi = null;
		_fastSmma = null;
		_slowEma = null;
		_fastCci = null;
		_slowCci = null;
		_previousRsi = null;
		_pointValue = 0m;
		_bestBid = null;
		_bestAsk = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = CalculatePointValue();

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_fastSmma = new SmoothedMovingAverage { Length = FastSmmaPeriod, CandlePrice = CandlePrice.Open };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod, CandlePrice = CandlePrice.Open };
		_fastCci = new CommodityChannelIndex { Length = FastCciPeriod, CandlePrice = CandlePrice.Typical };
		_slowCci = new CommodityChannelIndex { Length = SlowCciPeriod, CandlePrice = CandlePrice.Typical };

		SubscribeLevel1()?
			.Bind(ProcessLevel1)
			.Start();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _fastSmma, _slowEma, _fastCci, _slowCci, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: TakeProfitPoints > 0m && _pointValue > 0m ? new Unit(TakeProfitPoints * _pointValue, UnitTypes.Absolute) : null,
			stopLoss: StopLossPoints > 0m && _pointValue > 0m ? new Unit(StopLossPoints * _pointValue, UnitTypes.Absolute) : null,
			trailingStop: TrailingStopPoints > 0m && _pointValue > 0m ? new Unit(TrailingStopPoints * _pointValue, UnitTypes.Absolute) : null,
			trailingStep: TrailingStopPoints > 0m && _pointValue > 0m ? new Unit(TrailingStopPoints * _pointValue, UnitTypes.Absolute) : null,
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _fastSmma);
			DrawIndicator(area, _slowEma);
			DrawIndicator(area, _fastCci);
			DrawIndicator(area, _slowCci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_bestBid = (decimal)bid!;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_bestAsk = (decimal)ask!;
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal fastSmmaValue, decimal slowEmaValue, decimal fastCciValue, decimal slowCciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_rsi == null || _fastSmma == null || _slowEma == null || _fastCci == null || _slowCci == null)
			return;

		_rsi.Length = RsiPeriod;
		_fastSmma.Length = FastSmmaPeriod;
		_slowEma.Length = SlowEmaPeriod;
		_fastCci.Length = FastCciPeriod;
		_slowCci.Length = SlowCciPeriod;

		var previousRsi = _previousRsi;
		_previousRsi = rsiValue;

		if (previousRsi is null)
			return; // Need previous bar to evaluate the RSI cross.

		if (!_rsi.IsFormed || !_fastSmma.IsFormed || !_slowEma.IsFormed || !_fastCci.IsFormed || !_slowCci.IsFormed)
			return;

		if (Position != 0m)
			return; // Original expert opens at most one position at a time.

		if (!IsSpreadAcceptable())
			return; // Skip entries until the market spread is within the configured limit.

		var longSignal = fastSmmaValue > slowEmaValue
			&& previousRsi < BullishRsiLevel && rsiValue > BullishRsiLevel
			&& fastCciValue > 0m && slowCciValue > 0m;

		if (longSignal)
		{
			EnterLong(candle);
			return;
		}

		var shortSignal = fastSmmaValue < slowEmaValue
			&& previousRsi > BearishRsiLevel && rsiValue < BearishRsiLevel
			&& fastCciValue < 0m && slowCciValue < 0m;

		if (shortSignal)
		{
			EnterShort(candle);
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		BuyMarket(volume); // Enter long using the dynamically sized volume.
	}

	private void EnterShort(ICandleMessage candle)
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		SellMarket(volume); // Enter short using the dynamically sized volume.
	}

	private decimal CalculateOrderVolume()
	{
		var baseVolume = LotCoefficient;
		var divisor = EquityDivisor;
		if (divisor <= 0m)
			return NormalizeVolume(baseVolume);

		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (equity <= 0m)
			return NormalizeVolume(baseVolume);

		var scaled = equity / divisor;
		if (scaled <= 0m)
			return NormalizeVolume(baseVolume);

		var sqrt = (decimal)Math.Sqrt((double)scaled);
		var volume = baseVolume * sqrt;
		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return Math.Max(volume, 0m);

		if (security.VolumeStep is decimal step && step > 0m)
			volume = Math.Floor(volume / step) * step;

		if (security.VolumeMin is decimal min && min > 0m && volume < min)
			volume = min;

		if (security.VolumeMax is decimal max && max > 0m && volume > max)
			volume = max;

		return Math.Max(volume, 0m);
	}

	private bool IsSpreadAcceptable()
	{
		if (MaxSpreadPoints <= 0m)
			return true;

		if (_pointValue <= 0m)
			return true; // Without price step information we cannot evaluate the spread.

		if (_bestBid is not decimal bid || _bestAsk is not decimal ask)
			return false; // Wait for quotes before trading.

		var spread = ask - bid;
		var limit = MaxSpreadPoints * _pointValue;
		return spread <= limit;
	}

	private decimal CalculatePointValue()
	{
		var security = Security;
		if (security == null)
			return 0m;

		if (security.PriceStep is decimal step && step > 0m)
		{
			var decimals = security.Decimals;
			var multiplier = decimals is 3 or 5 ? 10m : 1m;
			return step * multiplier;
		}

		if (security.Decimals is int digits && digits > 0)
		{
			var multiplier = digits is 3 or 5 ? 10m : 1m;
			return (decimal)Math.Pow(10, -digits) * multiplier;
		}

		return 0.0001m;
	}
}
