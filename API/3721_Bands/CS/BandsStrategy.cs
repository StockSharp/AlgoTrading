using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands breakout strategy confirmed by Donchian channel slope and ATR-based risk management.
/// </summary>
public class BandsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _confirmationPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopAtrMultiplier;
	private readonly StrategyParam<decimal> _takeAtrMultiplier;

	private decimal? _prevOpen;
	private decimal? _prevClose;
	private decimal? _prevLowerBand;
	private decimal? _prevUpperBand;
	private decimal? _prevDonchLower;
	private decimal? _prevDonchUpper;
	private decimal? _prevAtr;

	private int _lowerTrendLength;
	private int _upperTrendLength;

	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	private int _equitySamples;
	private decimal _sumIndices;
	private decimal _sumEquity;
	private decimal _sumIndexEquity;
	private decimal _sumIndexSquared;
	private decimal _sumEquitySquared;

	/// <summary>
	/// Initializes a new instance of <see cref="BandsStrategy"/>.
	/// </summary>
	public BandsStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Net volume in lots sent with every order", "Trading")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for indicator calculations", "Market Data");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Number of candles used for the Bollinger Bands", "Indicators")
			.SetCanOptimize(true);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for the Bollinger Bands", "Indicators")
			.SetCanOptimize(true);

		_donchianPeriod = Param(nameof(DonchianPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Donchian Channel length used as trend filter", "Indicators")
			.SetCanOptimize(true);

		_confirmationPeriod = Param(nameof(ConfirmationPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Slope Confirmation", "Minimum number of bars that must keep the Donchian slope intact", "Indicators")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Length of the Average True Range used for stops", "Indicators")
			.SetCanOptimize(true);

		_stopAtrMultiplier = Param(nameof(StopAtrMultiplier), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Stop ATR Multiplier", "How many ATRs below/above the entry to place the stop", "Risk")
			.SetCanOptimize(true);

		_takeAtrMultiplier = Param(nameof(TakeAtrMultiplier), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Take ATR Multiplier", "How many ATRs below/above the entry to place the target", "Risk")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Trade volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the Bollinger Bands.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Deviation multiplier of the Bollinger Bands.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Period of the Donchian Channel.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// Number of consecutive bars required to confirm the Donchian slope.
	/// </summary>
	public int ConfirmationPeriod
	{
		get => _confirmationPeriod.Value;
		set => _confirmationPeriod.Value = value;
	}

	/// <summary>
	/// Period of the Average True Range indicator.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in ATR multiples.
	/// </summary>
	public decimal StopAtrMultiplier
	{
		get => _stopAtrMultiplier.Value;
		set => _stopAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in ATR multiples.
	/// </summary>
	public decimal TakeAtrMultiplier
	{
		get => _takeAtrMultiplier.Value;
		set => _takeAtrMultiplier.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var donchian = new DonchianChannel
		{
			Length = DonchianPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bollinger, atr, donchian, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal atrValue, decimal donchUpper, decimal donchLower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var lowerTrendLength = CalculateLowerTrendLength(donchLower);
		var upperTrendLength = CalculateUpperTrendLength(donchUpper);

		if (!_prevOpen.HasValue)
		{
			CachePreviousValues(candle, lower, upper, donchLower, donchUpper, atrValue, lowerTrendLength, upperTrendLength);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			CachePreviousValues(candle, lower, upper, donchLower, donchUpper, atrValue, lowerTrendLength, upperTrendLength);
			return;
		}

		var previousOpen = _prevOpen.Value;
		var previousClose = _prevClose!.Value;
		var previousLowerBand = _prevLowerBand!.Value;
		var previousUpperBand = _prevUpperBand!.Value;
		var previousDonchLower = _prevDonchLower!.Value;
		var previousDonchUpper = _prevDonchUpper!.Value;
		var atrForStops = _prevAtr ?? atrValue;

		if (Position == 0m)
		{
			if (previousOpen < previousLowerBand && previousClose > previousLowerBand && lowerTrendLength > ConfirmationPeriod)
			{
				OpenLong(candle.ClosePrice, atrForStops);
			}
			else if (previousOpen > previousUpperBand && previousClose < previousUpperBand && upperTrendLength > ConfirmationPeriod)
			{
				OpenShort(candle.ClosePrice, atrForStops);
			}
		}
		else if (Position > 0m)
		{
			var exitVolume = Position;
			var stopTriggered = _stopLossPrice is decimal stop && candle.LowPrice <= stop;
			var takeTriggered = _takeProfitPrice is decimal take && candle.HighPrice >= take;

			if (stopTriggered || takeTriggered || previousClose > previousDonchUpper || previousClose < previousDonchLower)
			{
				SellMarket(exitVolume);
				ClearProtection();
			}
		}
		else if (Position < 0m)
		{
			var exitVolume = Math.Abs(Position);
			var stopTriggered = _stopLossPrice is decimal stop && candle.HighPrice >= stop;
			var takeTriggered = _takeProfitPrice is decimal take && candle.LowPrice <= take;

			if (stopTriggered || takeTriggered || previousClose < previousDonchLower || previousClose > previousDonchUpper)
			{
				BuyMarket(exitVolume);
				ClearProtection();
			}
		}

		CachePreviousValues(candle, lower, upper, donchLower, donchUpper, atrValue, lowerTrendLength, upperTrendLength);
	}

	private int CalculateLowerTrendLength(decimal currentLower)
	{
		if (_prevDonchLower is decimal prevLower)
		{
			return currentLower >= prevLower ? _lowerTrendLength + 1 : 1;
		}

		return 1;
	}

	private int CalculateUpperTrendLength(decimal currentUpper)
	{
		if (_prevDonchUpper is decimal prevUpper)
		{
			return currentUpper <= prevUpper ? _upperTrendLength + 1 : 1;
		}

		return 1;
	}

	private void CachePreviousValues(ICandleMessage candle, decimal lower, decimal upper, decimal donchLower, decimal donchUpper, decimal atrValue, int lowerTrendLength, int upperTrendLength)
	{
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_prevLowerBand = lower;
		_prevUpperBand = upper;
		_prevDonchLower = donchLower;
		_prevDonchUpper = donchUpper;
		_prevAtr = atrValue;

		_lowerTrendLength = lowerTrendLength;
		_upperTrendLength = upperTrendLength;
	}

	private void OpenLong(decimal entryPrice, decimal atrValue)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		AssignProtection(entryPrice, atrValue, true);
	}

	private void OpenShort(decimal entryPrice, decimal atrValue)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		SellMarket(volume);
		AssignProtection(entryPrice, atrValue, false);
	}

	private void AssignProtection(decimal entryPrice, decimal atrValue, bool isLong)
	{
		if (atrValue <= 0m)
		{
			ClearProtection();
			return;
		}

		var stopDistance = atrValue * StopAtrMultiplier;
		var takeDistance = atrValue * TakeAtrMultiplier;

		if (isLong)
		{
			_stopLossPrice = entryPrice - stopDistance;
			_takeProfitPrice = entryPrice + takeDistance;
		}
		else
		{
			_stopLossPrice = entryPrice + stopDistance;
			_takeProfitPrice = entryPrice - takeDistance;
		}
	}

	private void ClearProtection()
	{
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		UpdateEquityStatistics(portfolio.CurrentValue);
	}

	private void UpdateEquityStatistics(decimal equity)
	{
		var index = (decimal)_equitySamples;
		_sumIndices += index;
		_sumEquity += equity;
		_sumIndexEquity += index * equity;
		_sumIndexSquared += index * index;
		_sumEquitySquared += equity * equity;
		_equitySamples++;

		if (_equitySamples % 100 != 0)
			return;

		var n = (decimal)_equitySamples;
		if (n <= 1m)
			return;

		var denominator = n * _sumIndexSquared - _sumIndices * _sumIndices;
		if (denominator == 0m)
			return;

		var slope = (n * _sumIndexEquity - _sumIndices * _sumEquity) / denominator;
		var mean = _sumEquity / n;
		var ssTotal = _sumEquitySquared - n * mean * mean;

		if (ssTotal == 0m)
		{
			LogInfo("Equity R-squared: 1.0000");
			return;
		}

		var regressionComponent = slope * (_sumIndexEquity - (_sumIndices / n) * _sumEquity);
		var rSquared = regressionComponent / ssTotal;

		if (slope < 0m)
			rSquared = -rSquared;

		LogInfo($"Equity R-squared: {rSquared:F4}");
	}
}
