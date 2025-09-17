using System;

using StockSharp;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Envelope breakout strategy converted from the MetaTrader "5Mins Envelopes" expert.
/// </summary>
public class FiveMinsEnvelopesStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _distancePoints;
	private readonly StrategyParam<int> _envelopePeriod;
	private readonly StrategyParam<decimal> _envelopeDeviationPercent;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<int> _maxSpreadPoints;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _medianLwma = null!;
	private ICandleMessage? _previousCandle;
	private decimal? _previousUpper;
	private decimal? _previousLower;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _priceStep;

	/// <summary>
	/// Order volume submitted for each entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Minimum distance between price and envelope in points.
	/// </summary>
	public int DistancePoints
	{
		get => _distancePoints.Value;
		set => _distancePoints.Value = value;
	}

	/// <summary>
	/// Period for the weighted moving average base.
	/// </summary>
	public int EnvelopePeriod
	{
		get => _envelopePeriod.Value;
		set => _envelopePeriod.Value = value;
	}

	/// <summary>
	/// Envelope deviation expressed in percent.
	/// </summary>
	public decimal EnvelopeDeviationPercent
	{
		get => _envelopeDeviationPercent.Value;
		set => _envelopeDeviationPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance in points.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Maximum spread filter in points.
	/// </summary>
	public int MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FiveMinsEnvelopesStrategy"/>.
	/// </summary>
	public FiveMinsEnvelopesStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Volume", "Order volume per signal", "Trading")
			.SetGreaterThanZero();

		_distancePoints = Param(nameof(DistancePoints), 140)
			.SetDisplay("Distance", "Minimal distance from envelope to trigger entries", "Signals")
			.SetCanOptimize(true);

		_envelopePeriod = Param(nameof(EnvelopePeriod), 3)
			.SetDisplay("Envelope Period", "Length of the LWMA basis", "Indicator")
			.SetRange(1, 50);

		_envelopeDeviationPercent = Param(nameof(EnvelopeDeviationPercent), 0.05m)
			.SetDisplay("Envelope %", "Envelope deviation in percent", "Indicator")
			.SetRange(0.01m, 1m);

		_stopLossPoints = Param(nameof(StopLossPoints), 250)
			.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk")
			.SetRange(0, 5000);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0)
			.SetDisplay("Take Profit", "Take-profit distance in points", "Risk")
			.SetRange(0, 5000);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 120)
			.SetDisplay("Trailing Stop", "Trailing-stop distance in points", "Risk")
			.SetRange(0, 5000);

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 25)
			.SetDisplay("Max Spread", "Maximum allowed bid/ask spread in points", "Filters")
			.SetRange(0, 1000);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousCandle = null;
		_previousUpper = null;
		_previousLower = null;
		_bestBid = null;
		_bestAsk = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;

		_medianLwma = new WeightedMovingAverage
		{
			Length = EnvelopePeriod,
			CandlePrice = CandlePrice.Median,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_medianLwma, ProcessCandle)
			.Start();

		if (Security is not null)
		{
			SubscribeLevel1(Security)
				.Bind(OnLevel1)
				.Start();
		}

		Volume = TradeVolume;

		StartProtection(
			takeProfit: TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * _priceStep, UnitTypes.Price) : null,
			stopLoss: StopLossPoints > 0 ? new Unit(StopLossPoints * _priceStep, UnitTypes.Price) : null,
			trailingStop: TrailingStopPoints > 0 ? new Unit(TrailingStopPoints * _priceStep, UnitTypes.Price) : null,
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _medianLwma);
			DrawOwnTrades(area);
		}
	}

	private void OnLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BidPrice) is decimal bid && bid > 0m)
		{
			_bestBid = bid;
		}

		if (message.TryGetDecimal(Level1Fields.AskPrice) is decimal ask && ask > 0m)
		{
			_bestAsk = ask;
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal basis)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		_medianLwma.Length = EnvelopePeriod;

		var deviation = EnvelopeDeviationPercent / 100m;
		var upper = basis * (1m + deviation);
		var lower = basis * (1m - deviation);

		if (!_medianLwma.IsFormed || !IsFormedAndOnlineAndAllowTrading())
		{
			_previousCandle = candle;
			_previousUpper = upper;
			_previousLower = lower;
			return;
		}

		if (_previousCandle is not null &&
			_previousUpper is decimal prevUpper &&
			_previousLower is decimal prevLower)
		{
			var distance = DistancePoints * _priceStep;
			var bidPrice = _bestBid ?? candle.ClosePrice;
			var spreadOk = true;

			if (MaxSpreadPoints > 0 && _bestAsk is decimal askPrice && _bestBid is decimal bid)
			{
				var spread = askPrice - bid;
				spreadOk = spread < MaxSpreadPoints * _priceStep;
			}

			if (spreadOk)
			{
				var longSignal = prevLower - _previousCandle.LowPrice > distance && prevLower - bidPrice > distance;
				var shortSignal = _previousCandle.HighPrice - prevUpper > distance && bidPrice - prevUpper > distance;

				if (longSignal && Position == 0 && TradeVolume > 0m)
				{
					// Price bounced well below the lower envelope, open a long position.
					BuyMarket(TradeVolume);
				}
				else if (shortSignal && Position == 0 && TradeVolume > 0m)
				{
					// Price extended far above the upper envelope, open a short position.
					SellMarket(TradeVolume);
				}
			}
		}

		_previousCandle = candle;
		_previousUpper = upper;
		_previousLower = lower;
	}
}
