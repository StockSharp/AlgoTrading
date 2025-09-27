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
/// Hedged multi-currency strategy converted from the original CashMachine expert advisor.
/// Opens synchronized positions on a base and hedge symbol when EMA, RSI, and correlation filters align.
/// </summary>
public class CashMachineStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<int> _emaShortPeriod;
	private readonly StrategyParam<int> _emaLongPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _correlationLookback;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _dailyCandleType;
	private readonly StrategyParam<Security> _hedgeSecurityParam;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _baseDailySma;
	private SimpleMovingAverage _hedgeDailySma;

	private decimal[] _baseDiffBuffer;
	private decimal[] _hedgeDiffBuffer;
	private int _bufferCount;

	private decimal _currentCorrelation;

	private decimal _basePosition;
	private decimal _baseAveragePrice;
	private decimal _hedgePosition;
	private decimal _hedgeAveragePrice;

	private decimal _lastBaseClose;
	private decimal _lastHedgeClose;

	private DateTimeOffset? _pendingBaseTime;
	private DateTimeOffset? _pendingHedgeTime;
	private decimal _pendingBaseDiff;
	private decimal _pendingHedgeDiff;

	/// <summary>
	/// Initializes strategy parameters and defaults.
	/// </summary>
	public CashMachineStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 10m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Total floating profit that triggers closing both positions.", "Risk Management");

		_emaShortPeriod = Param(nameof(EmaShortPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Period", "Length of the short-term EMA on the base instrument.", "Trend Filter")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_emaLongPeriod = Param(nameof(EmaLongPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Period", "Length of the long-term EMA on the base instrument.", "Trend Filter")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 1);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of the RSI oscillator filter.", "Oscillators")
			.SetCanOptimize(true)
			.SetOptimize(10, 21, 1);

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "Upper bound to detect oversold conditions (<= opens long).", "Oscillators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Lower bound to detect overbought conditions (>= opens short).", "Oscillators");

		_correlationLookback = Param(nameof(CorrelationLookback), 60)
			.SetGreaterThanZero()
			.SetDisplay("Correlation Lookback", "Number of daily deviations used for Pearson correlation.", "Correlation")
			.SetCanOptimize(true)
			.SetOptimize(30, 90, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Signal Candle Type", "Intraday candle type feeding EMA and RSI.", "General");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Daily Candle Type", "Higher timeframe used to build daily deviations for correlation.", "General");

		_hedgeSecurityParam = Param<Security>(nameof(HedgeSecurity))
			.SetDisplay("Hedge Security", "Second symbol traded together with the base instrument.", "Instruments")
			.SetRequired();

		Volume = 0.1m;
	}

	/// <summary>
	/// Total floating profit threshold that liquidates both legs.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Fast EMA length applied to the base security.
	/// </summary>
	public int EmaShortPeriod
	{
		get => _emaShortPeriod.Value;
		set => _emaShortPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length applied to the base security.
	/// </summary>
	public int EmaLongPeriod
	{
		get => _emaLongPeriod.Value;
		set => _emaLongPeriod.Value = value;
	}

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI threshold that marks oversold territory.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// RSI threshold that marks overbought territory.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// Number of daily deviations used to compute Pearson correlation.
	/// </summary>
	public int CorrelationLookback
	{
		get => _correlationLookback.Value;
		set
		{
			_correlationLookback.Value = value;
			InitializeCorrelationBuffers();
		}
	}

	/// <summary>
	/// Intraday candle type for EMA and RSI evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type feeding the correlation calculations.
	/// </summary>
	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	/// <summary>
	/// Second security that is traded together with the base instrument.
	/// </summary>
	public Security HedgeSecurity
	{
		get => _hedgeSecurityParam.Value;
		set => _hedgeSecurityParam.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null && CandleType != null)
			yield return (Security, CandleType);

		if (Security != null && DailyCandleType != null && !Equals(DailyCandleType, CandleType))
			yield return (Security, DailyCandleType);

		if (HedgeSecurity != null && CandleType != null)
			yield return (HedgeSecurity, CandleType);

		if (HedgeSecurity != null && DailyCandleType != null && !Equals(DailyCandleType, CandleType))
			yield return (HedgeSecurity, DailyCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastEma = null;
		_slowEma = null;
		_rsi = null;
		_baseDailySma = null;
		_hedgeDailySma = null;

		InitializeCorrelationBuffers();

		_basePosition = 0m;
		_baseAveragePrice = 0m;
		_hedgePosition = 0m;
		_hedgeAveragePrice = 0m;

		_lastBaseClose = 0m;
		_lastHedgeClose = 0m;

		_pendingBaseTime = null;
		_pendingHedgeTime = null;
		_pendingBaseDiff = 0m;
		_pendingHedgeDiff = 0m;

		_currentCorrelation = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (HedgeSecurity == null)
			throw new InvalidOperationException("Hedge security is not specified.");

		_fastEma = new ExponentialMovingAverage { Length = EmaShortPeriod };
		_slowEma = new ExponentialMovingAverage { Length = EmaLongPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		_baseDailySma = new SimpleMovingAverage { Length = CorrelationLookback };
		_hedgeDailySma = new SimpleMovingAverage { Length = CorrelationLookback };

		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription
			.Bind(_fastEma, _slowEma, _rsi, ProcessBaseCandle)
			.Start();

		var hedgeSubscription = SubscribeCandles(CandleType, security: HedgeSecurity);
		hedgeSubscription
			.Bind(ProcessHedgeCandle)
			.Start();

		var baseDailySubscription = SubscribeCandles(DailyCandleType);
		baseDailySubscription
			.Bind(_baseDailySma, ProcessBaseDailyCandle)
			.Start();

		var hedgeDailySubscription = SubscribeCandles(DailyCandleType, security: HedgeSecurity);
		hedgeDailySubscription
			.Bind(_hedgeDailySma, ProcessHedgeDailyCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSubscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessBaseCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastBaseClose = candle.ClosePrice;
		CheckTakeProfit();

		if (!_fastEma.IsFormed || !_slowEma.IsFormed || !_rsi.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsWithinTradingWindow(candle.OpenTime))
			return;

		if (!HasCorrelationData() || _currentCorrelation >= 0m)
			return;

		if (_basePosition != 0m || _hedgePosition != 0m)
			return;

		var emaDiff = fast - slow;

		if (emaDiff > 0m && rsiValue <= RsiOversold)
		{
			OpenLongPair();
		}
		else if (emaDiff < 0m && rsiValue >= RsiOverbought)
		{
			OpenShortPair();
		}
	}

	private void ProcessHedgeCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastHedgeClose = candle.ClosePrice;
		CheckTakeProfit();
	}

	private void ProcessBaseDailyCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_baseDailySma.IsFormed)
			return;

		_pendingBaseDiff = candle.ClosePrice - smaValue;
		_pendingBaseTime = candle.OpenTime;
		TryUpdateCorrelation();
	}

	private void ProcessHedgeDailyCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hedgeDailySma.IsFormed)
			return;

		_pendingHedgeDiff = candle.ClosePrice - smaValue;
		_pendingHedgeTime = candle.OpenTime;
		TryUpdateCorrelation();
	}

	private void TryUpdateCorrelation()
	{
		if (!_pendingBaseTime.HasValue || !_pendingHedgeTime.HasValue)
			return;

		var baseTime = _pendingBaseTime.Value;
		var hedgeTime = _pendingHedgeTime.Value;

		if (baseTime == hedgeTime)
		{
			AddCorrelationSample(_pendingBaseDiff, _pendingHedgeDiff);
			_pendingBaseTime = null;
			_pendingHedgeTime = null;
		}
		else if (baseTime > hedgeTime)
		{
			_pendingHedgeTime = null;
		}
		else
		{
			_pendingBaseTime = null;
		}
	}

	private void AddCorrelationSample(decimal baseDiff, decimal hedgeDiff)
	{
		var requiredLength = Math.Max(2, CorrelationLookback);

		if (_baseDiffBuffer == null || _baseDiffBuffer.Length != requiredLength)
			InitializeCorrelationBuffers();

		if (_bufferCount < _baseDiffBuffer.Length)
		{
			_baseDiffBuffer[_bufferCount] = baseDiff;
			_hedgeDiffBuffer[_bufferCount] = hedgeDiff;
			_bufferCount++;
		}
		else
		{
			Array.Copy(_baseDiffBuffer, 1, _baseDiffBuffer, 0, _baseDiffBuffer.Length - 1);
			Array.Copy(_hedgeDiffBuffer, 1, _hedgeDiffBuffer, 0, _hedgeDiffBuffer.Length - 1);
			_baseDiffBuffer[^1] = baseDiff;
			_hedgeDiffBuffer[^1] = hedgeDiff;
		}

		_currentCorrelation = CalculateCorrelation(_baseDiffBuffer, _hedgeDiffBuffer, _bufferCount);
	}

	private static decimal CalculateCorrelation(decimal[] baseValues, decimal[] hedgeValues, int count)
	{
		if (baseValues == null || hedgeValues == null)
			return 0m;

		var length = Math.Min(Math.Min(baseValues.Length, hedgeValues.Length), Math.Max(1, count));
		if (length <= 1)
			return 0m;

		decimal sumBase = 0m;
		decimal sumHedge = 0m;

		for (var i = 0; i < length; i++)
		{
			sumBase += baseValues[i];
			sumHedge += hedgeValues[i];
		}

		var meanBase = sumBase / length;
		var meanHedge = sumHedge / length;

		decimal numerator = 0m;
		decimal sumSqBase = 0m;
		decimal sumSqHedge = 0m;

		for (var i = 0; i < length; i++)
		{
			var baseDelta = baseValues[i] - meanBase;
			var hedgeDelta = hedgeValues[i] - meanHedge;

			numerator += baseDelta * hedgeDelta;
			sumSqBase += baseDelta * baseDelta;
			sumSqHedge += hedgeDelta * hedgeDelta;
		}

		if (sumSqBase <= 0m || sumSqHedge <= 0m)
			return 0m;

		var denominator = (decimal)Math.Sqrt((double)(sumSqBase * sumSqHedge));
		if (denominator == 0m)
			return 0m;

		return numerator / denominator;
	}

	private bool HasCorrelationData()
	{
		if (_baseDiffBuffer == null)
			return false;

		var required = Math.Max(2, CorrelationLookback);
		return _bufferCount >= Math.Min(required, _baseDiffBuffer.Length);
	}

	private bool IsWithinTradingWindow(DateTimeOffset candleTime)
	{
		var time = candleTime.DateTime;
		if (time.Day > 5)
			return false;

		if (time.Day == 5 && time.Hour > 18)
			return false;

		return true;
	}

	private void CheckTakeProfit()
	{
		if (TakeProfit <= 0m)
			return;

		var totalProfit = CalculateOpenProfit();
		if (totalProfit >= TakeProfit && (_basePosition != 0m || _hedgePosition != 0m))
		{
			ClosePairPositions();
		}
	}

	private decimal CalculateOpenProfit()
	{
		decimal profit = 0m;

		if (_basePosition != 0m && _baseAveragePrice != 0m && _lastBaseClose != 0m)
			profit += (_lastBaseClose - _baseAveragePrice) * _basePosition;

		if (_hedgePosition != 0m && _hedgeAveragePrice != 0m && _lastHedgeClose != 0m)
			profit += (_lastHedgeClose - _hedgeAveragePrice) * _hedgePosition;

		return profit;
	}

	private void ClosePairPositions()
	{
		if (_basePosition > 0m)
			SellMarket(_basePosition);
		else if (_basePosition < 0m)
			BuyMarket(Math.Abs(_basePosition));

		if (_hedgePosition > 0m)
			SellMarket(_hedgePosition, security: HedgeSecurity);
		else if (_hedgePosition < 0m)
			BuyMarket(Math.Abs(_hedgePosition), security: HedgeSecurity);
	}

	private void OpenLongPair()
	{
		var baseVolume = GetBaseTradeVolume();
		var hedgeVolume = GetHedgeTradeVolume(baseVolume);

		if (baseVolume <= 0m || hedgeVolume <= 0m)
			return;

		BuyMarket(baseVolume);
		BuyMarket(hedgeVolume, security: HedgeSecurity);
	}

	private void OpenShortPair()
	{
		var baseVolume = GetBaseTradeVolume();
		var hedgeVolume = GetHedgeTradeVolume(baseVolume);

		if (baseVolume <= 0m || hedgeVolume <= 0m)
			return;

		SellMarket(baseVolume);
		SellMarket(hedgeVolume, security: HedgeSecurity);
	}

	private decimal GetBaseTradeVolume()
	{
		var volume = Volume;
		if (volume <= 0m)
			volume = 0.1m;

		return NormalizeVolume(Security, volume);
	}

	private decimal GetHedgeTradeVolume(decimal baseVolume)
	{
		return NormalizeVolume(HedgeSecurity, baseVolume);
	}

	private static decimal NormalizeVolume(Security security, decimal volume)
	{
		if (security == null)
			return volume;

		if (security.VolumeStep is decimal step && step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		if (security.VolumeMin is decimal min && min > 0m && volume < min)
			volume = min;

		if (security.VolumeMax is decimal max && max > 0m && volume > max)
			volume = max;

		return volume;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null)
			return;

		var security = trade.Trade.Security;

		if (security == Security)
		{
			UpdatePosition(ref _basePosition, ref _baseAveragePrice, trade);
		}
		else if (HedgeSecurity != null && security == HedgeSecurity)
		{
			UpdatePosition(ref _hedgePosition, ref _hedgeAveragePrice, trade);
		}
	}

	private static void UpdatePosition(ref decimal position, ref decimal averagePrice, MyTrade trade)
	{
		var volume = trade.Trade.Volume;
		if (volume <= 0m)
			return;

		var price = trade.Trade.Price;
		var signedVolume = trade.Order.Side == Sides.Buy ? volume : -volume;
		var current = position;

		if (current == 0m)
		{
			position = signedVolume;
			averagePrice = price;
			return;
		}

		var newPosition = current + signedVolume;

		if (Math.Sign(current) == Math.Sign(newPosition))
		{
			if (Math.Sign(current) == Math.Sign(signedVolume))
			{
				var totalVolume = Math.Abs(current) + volume;
				if (totalVolume > 0m)
					averagePrice = ((averagePrice * Math.Abs(current)) + (price * volume)) / totalVolume;
			}

			position = newPosition;

			if (position == 0m)
				averagePrice = 0m;

			return;
		}

		position = newPosition;
		if (position == 0m)
		{
			averagePrice = 0m;
		}
		else
		{
			averagePrice = price;
		}
	}

	private void InitializeCorrelationBuffers()
	{
		var length = Math.Max(2, CorrelationLookback);
		_baseDiffBuffer = new decimal[length];
		_hedgeDiffBuffer = new decimal[length];
		_bufferCount = 0;
		_currentCorrelation = 0m;
	}
}
