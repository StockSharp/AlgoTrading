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
/// Adaptive trend-following strategy based on the AltrTrend Signal v2.2 logic.
/// </summary>
public class AltrTrendSignalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _kPercent;
	private readonly StrategyParam<decimal> _kStop;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _allowBuyEntries;
	private readonly StrategyParam<bool> _allowSellEntries;
	private readonly StrategyParam<bool> _allowBuyExits;
	private readonly StrategyParam<bool> _allowSellExits;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<CandleInfo> _candles = new();
	private readonly List<SignalInfo> _signals = new();

	private decimal _entryPrice;
	private decimal? _previousAdx;
	private int _previousTrend;

	/// <summary>
	/// Percentage used to build the internal channel (original parameter K).
	/// </summary>
	public decimal KPercent
	{
		get => _kPercent.Value;
		set => _kPercent.Value = value;
	}

	/// <summary>
	/// Multiplier for the arrow projection used in the original indicator (Kstop).
	/// </summary>
	public decimal KStop
	{
		get => _kStop.Value;
		set => _kStop.Value = value;
	}

	/// <summary>
	/// Base lookback for the adaptive channel.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Period for the Average Directional Index calculation.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Number of completed candles to delay signal execution.
	/// </summary>
	public int SignalBar
	{
		get => Math.Max(0, _signalBar.Value);
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Permission to open long positions.
	/// </summary>
	public bool AllowBuyEntries
	{
		get => _allowBuyEntries.Value;
		set => _allowBuyEntries.Value = value;
	}

	/// <summary>
	/// Permission to open short positions.
	/// </summary>
	public bool AllowSellEntries
	{
		get => _allowSellEntries.Value;
		set => _allowSellEntries.Value = value;
	}

	/// <summary>
	/// Permission to close long positions on opposite signals.
	/// </summary>
	public bool AllowBuyExits
	{
		get => _allowBuyExits.Value;
		set => _allowBuyExits.Value = value;
	}

	/// <summary>
	/// Permission to close short positions on opposite signals.
	/// </summary>
	public bool AllowSellExits
	{
		get => _allowSellExits.Value;
		set => _allowSellExits.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps. Set to zero to disable.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps. Set to zero to disable.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for the indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults matching the original expert advisor.
	/// </summary>
	public AltrTrendSignalStrategy()
	{
		_kPercent = Param(nameof(KPercent), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Channel Percent", "Percentage used for channel contraction", "Indicator");

		_kStop = Param(nameof(KStop), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Arrow Offset", "Multiplier for projected arrow price", "Indicator");

		_kPeriod = Param(nameof(KPeriod), 150)
			.SetGreaterThanZero()
			.SetDisplay("Base Period", "Base period for adaptive channel", "Indicator");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "ADX length driving channel width", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Delay in bars before executing signals", "Trading Rules");

		_allowBuyEntries = Param(nameof(AllowBuyEntries), true)
			.SetDisplay("Allow Long Entries", "Enable opening long positions", "Trading Rules");

		_allowSellEntries = Param(nameof(AllowSellEntries), true)
			.SetDisplay("Allow Short Entries", "Enable opening short positions", "Trading Rules");

		_allowBuyExits = Param(nameof(AllowBuyExits), true)
			.SetDisplay("Allow Long Exits", "Enable closing long positions", "Trading Rules");

		_allowSellExits = Param(nameof(AllowSellExits), true)
			.SetDisplay("Allow Short Exits", "Enable closing short positions", "Trading Rules");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetDisplay("Stop Loss Points", "Protective stop in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetDisplay("Take Profit Points", "Profit target in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");
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

		_candles.Clear();
		_signals.Clear();
		_entryPrice = 0m;
		_previousAdx = null;
		_previousTrend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal currentAdx)
			return;

		var candleInfo = new CandleInfo(candle.OpenTime, candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		_candles.Add(candleInfo);

		SignalInfo signal = new SignalInfo(SignalTypes.None, null);
		if (_previousAdx.HasValue)
		{
			signal = CalculateSignal(_candles.Count - 1, _previousAdx.Value);
		}
		else
		{
			_previousTrend = 0;
		}

		_previousAdx = currentAdx;
		_signals.Add(signal);

		ManageEntries(candle);
		ManageRisk(candle);
		TrimHistory();
	}

	private SignalInfo CalculateSignal(int lastIndex, decimal previousAdx)
	{
		if (lastIndex < 0)
			return new SignalInfo(SignalTypes.None, null);

		var available = lastIndex + 1;
		var adx = Math.Max(previousAdx, 1m);
		var period = Math.Max(1, KPeriod);
		var ratio = (decimal)period / adx;
		var ssp = (int)Math.Ceiling((double)ratio);
		if (ssp < 1)
			ssp = 1;

		var length = Math.Min(ssp, available);
		var startIndex = available - length;

		decimal rangeSum = 0m;
		decimal ssMax = decimal.MinValue;
		decimal ssMin = decimal.MaxValue;

		for (var i = startIndex; i < available; i++)
		{
			var candle = _candles[i];
			var candleRange = Math.Abs(candle.High - candle.Low);
			rangeSum += candleRange;

			if (candle.High > ssMax)
				ssMax = candle.High;

			if (candle.Low < ssMin)
				ssMin = candle.Low;
		}

		if (ssMax == decimal.MinValue || ssMin == decimal.MaxValue)
			return new SignalInfo(SignalTypes.None, null);

		var range = rangeSum / (length + 1);
		var bandWidth = ssMax - ssMin;
		var contraction = bandWidth * KPercent / 100m;
		var lowerBound = ssMin + contraction;
		var upperBound = ssMax - contraction;

		var current = _candles[lastIndex];
		var previousTrend = _previousTrend;
		var trend = previousTrend;

		if (current.Close < lowerBound)
			trend = -1;
		else if (current.Close > upperBound)
			trend = 1;

		if (previousTrend == 0)
			previousTrend = trend;

		SignalTypes signalType = SignalTypes.None;
		decimal? arrowPrice = null;

		if (trend != previousTrend)
		{
			if (current.Close > upperBound)
			{
				signalType = SignalTypes.Buy;
				arrowPrice = current.Low - range * KStop;
			}
			else if (current.Close < lowerBound)
			{
				signalType = SignalTypes.Sell;
				arrowPrice = current.High + range * KStop;
			}
		}

		_previousTrend = trend;
		return new SignalInfo(signalType, arrowPrice);
	}

	private void ManageEntries(ICandleMessage candle)
	{
		var offset = SignalBar;
		if (offset < 0)
			offset = 0;

		var signalIndex = _signals.Count - 1 - offset;
		var candleIndex = _candles.Count - 1 - offset;
		if (signalIndex < 0 || candleIndex < 0)
			return;

		var action = _signals[signalIndex];
		if (action.Type == SignalTypes.None)
			return;

		var signalCandle = _candles[candleIndex];

		switch (action.Type)
		{
			case SignalTypes.Buy:
				ExecuteBuy(signalCandle, candle.ClosePrice, action.ArrowPrice);
				break;
			case SignalTypes.Sell:
				ExecuteSell(signalCandle, candle.ClosePrice, action.ArrowPrice);
				break;
		}
	}

	private void ExecuteBuy(CandleInfo signalCandle, decimal currentPrice, decimal? arrowPrice)
	{
		var position = Position;
		decimal closeVolume = 0m;
		decimal openVolume = 0m;

		if (position < 0 && AllowSellExits)
			closeVolume = Math.Abs(position);

		if (position < 0 && !AllowSellExits)
			openVolume = 0m;
		else if (AllowBuyEntries && position <= 0)
			openVolume = Volume;

		var totalVolume = 0m;
		if (closeVolume > 0m && openVolume > 0m)
			totalVolume = closeVolume + openVolume;
		else if (closeVolume > 0m)
			totalVolume = closeVolume;
		else if (openVolume > 0m)
			totalVolume = openVolume;

		if (totalVolume <= 0m)
			return;

		BuyMarket(totalVolume);

		if (openVolume > 0m)
		{
			_entryPrice = currentPrice;
			LogInfo($"Buy signal at {signalCandle.OpenTime:O}. Volume={totalVolume}, Arrow={arrowPrice ?? "n/a"}");
		}
		else
		{
			_entryPrice = 0m;
			LogInfo($"Closing short due to buy signal at {signalCandle.OpenTime:O}. Volume={totalVolume}, Arrow={arrowPrice ?? "n/a"}");
		}
	}

	private void ExecuteSell(CandleInfo signalCandle, decimal currentPrice, decimal? arrowPrice)
	{
		var position = Position;
		decimal closeVolume = 0m;
		decimal openVolume = 0m;

		if (position > 0 && AllowBuyExits)
			closeVolume = Math.Abs(position);

		if (position > 0 && !AllowBuyExits)
			openVolume = 0m;
		else if (AllowSellEntries && position >= 0)
			openVolume = Volume;

		var totalVolume = 0m;
		if (closeVolume > 0m && openVolume > 0m)
			totalVolume = closeVolume + openVolume;
		else if (closeVolume > 0m)
			totalVolume = closeVolume;
		else if (openVolume > 0m)
			totalVolume = openVolume;

		if (totalVolume <= 0m)
			return;

		SellMarket(totalVolume);

		if (openVolume > 0m)
		{
			_entryPrice = currentPrice;
			LogInfo($"Sell signal at {signalCandle.OpenTime:O}. Volume={totalVolume}, Arrow={arrowPrice ?? "n/a"}");
		}
		else
		{
			_entryPrice = 0m;
			LogInfo($"Closing long due to sell signal at {signalCandle.OpenTime:O}. Volume={totalVolume}, Arrow={arrowPrice ?? "n/a"}");
		}
	}

	private void ManageRisk(ICandleMessage candle)
	{
		if (_entryPrice == 0m)
			return;

		var position = Position;
		if (position == 0m)
		{
			_entryPrice = 0m;
			return;
		}

		var priceStep = Security?.PriceStep;
		if (priceStep is not decimal step || step <= 0m)
			return;

		var stopLoss = StopLossPoints;
		var takeProfit = TakeProfitPoints;

		if (position > 0)
		{
			if (stopLoss > 0 && candle.ClosePrice <= _entryPrice - stopLoss * step)
			{
				SellMarket(Math.Abs(position));
				_entryPrice = 0m;
				LogInfo($"Long stop loss triggered at {candle.ClosePrice}.");
				return;
			}

			if (takeProfit > 0 && candle.ClosePrice >= _entryPrice + takeProfit * step)
			{
				SellMarket(Math.Abs(position));
				_entryPrice = 0m;
				LogInfo($"Long take profit triggered at {candle.ClosePrice}.");
			}
		}
		else if (position < 0)
		{
			if (stopLoss > 0 && candle.ClosePrice >= _entryPrice + stopLoss * step)
			{
				BuyMarket(Math.Abs(position));
				_entryPrice = 0m;
				LogInfo($"Short stop loss triggered at {candle.ClosePrice}.");
				return;
			}

			if (takeProfit > 0 && candle.ClosePrice <= _entryPrice - takeProfit * step)
			{
				BuyMarket(Math.Abs(position));
				_entryPrice = 0m;
				LogInfo($"Short take profit triggered at {candle.ClosePrice}.");
			}
		}
	}

	private void TrimHistory()
	{
		var maxHistory = Math.Max(KPeriod * 4, SignalBar + 500);
		while (_candles.Count > maxHistory)
		{
			_candles.RemoveAt(0);
			if (_signals.Count > 0)
				_signals.RemoveAt(0);
		}
	}

	private readonly record struct CandleInfo(DateTimeOffset OpenTime, decimal High, decimal Low, decimal Close);
	private readonly record struct SignalInfo(SignalTypes Type, decimal? ArrowPrice);

	private enum SignalTypes
	{
		None,
		Buy,
		Sell,
	}
}