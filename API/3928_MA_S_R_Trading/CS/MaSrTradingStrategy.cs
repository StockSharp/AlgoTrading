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
/// Strategy converted from the MA S.R Trading MetaTrader expert advisor.
/// It detects short-term turning points using a short simple moving average
/// and manages stop levels at nearby swing highs and lows.
/// </summary>
public class MaSrTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _highLookback;
	private readonly StrategyParam<int> _lowLookback;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma;
	private Highest _highestHigh;
	private Lowest _lowestLow;

	private decimal? _smaPrev1;
	private decimal? _smaPrev2;
	private decimal? _smaPrev3;
	private decimal? _lastSellStop;
	private decimal? _lastBuyStop;
	private decimal? _previousClose;

	/// <summary>
	/// Gets or sets the period for the simple moving average.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// Gets or sets the number of candles used to search for the stop level on shorts.
	/// </summary>
	public int HighLookback
	{
		get => _highLookback.Value;
		set => _highLookback.Value = value;
	}

	/// <summary>
	/// Gets or sets the number of candles used to search for the stop level on longs.
	/// </summary>
	public int LowLookback
	{
		get => _lowLookback.Value;
		set => _lowLookback.Value = value;
	}

	/// <summary>
	/// Gets or sets the trading volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets the candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MaSrTradingStrategy"/> class.
	/// </summary>
	public MaSrTradingStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 5)
			.SetDisplay("SMA Period", "Period of the moving average used for turning points", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_highLookback = Param(nameof(HighLookback), 5)
			.SetDisplay("High Lookback", "Number of candles to evaluate swing highs", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(3, 30, 1);

		_lowLookback = Param(nameof(LowLookback), 5)
			.SetDisplay("Low Lookback", "Number of candles to evaluate swing lows", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(3, 30, 1);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Volume", "Trading volume in lots", "Trading")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to analyse", "General");

		Volume = TradeVolume;
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

		_sma = null;
		_highestHigh = null;
		_lowestLow = null;
		_smaPrev1 = null;
		_smaPrev2 = null;
		_smaPrev3 = null;
		_lastSellStop = null;
		_lastBuyStop = null;
		_previousClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_sma = new SimpleMovingAverage { Length = SmaPeriod };
		_highestHigh = new Highest { Length = HighLookback };
		_lowestLow = new Lowest { Length = LowLookback };

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenCandlesFinished(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
		}

		_previousClose = null;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (_sma is null || _highestHigh is null || _lowestLow is null)
			return;

		var smaValue = _sma.Process(new CandleIndicatorValue(candle, candle.ClosePrice));
		var highestValue = _highestHigh.Process(new CandleIndicatorValue(candle, candle.HighPrice));
		var lowestValue = _lowestLow.Process(new CandleIndicatorValue(candle, candle.LowPrice));

		if (!smaValue.IsFinal)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var currentSma = smaValue.GetValue<decimal>();

		if (_smaPrev1 is not decimal prev1 || _smaPrev2 is not decimal prev2 || _smaPrev3 is not decimal prev3)
		{
			ShiftSmaHistory(currentSma);
			_previousClose = candle.ClosePrice;
			return;
		}

		var hasHigh = highestValue.IsFinal;
		var hasLow = lowestValue.IsFinal;

		var isBearishTurn = prev1 < prev2 && prev2 > prev3;
		var isBullishTurn = prev1 > prev2 && prev2 < prev3;

		if (isBearishTurn && hasHigh && _previousClose is decimal prevCloseForShort)
		{
			var candidate = highestValue.GetValue<decimal>();
			if (candidate > prevCloseForShort)
			{
				_lastSellStop = candidate;

				if (IsFormedAndOnlineAndAllowTrading() && Position >= 0)
				{
					var volume = Volume + Math.Max(0m, Position);
					SellMarket(volume);
				}
			}
		}

		if (isBullishTurn && hasLow && _previousClose is decimal prevCloseForLong)
		{
			var candidate = lowestValue.GetValue<decimal>();
			if (candidate < prevCloseForLong)
			{
				_lastBuyStop = candidate;

				if (IsFormedAndOnlineAndAllowTrading() && Position <= 0)
				{
					var volume = Volume + Math.Max(0m, -Position);
					BuyMarket(volume);
				}
			}
		}

		if (Position < 0 && _lastSellStop is decimal shortStop && candle.HighPrice >= shortStop)
		{
			BuyMarket(Math.Abs(Position));
			_lastSellStop = null;
		}
		else if (Position > 0 && _lastBuyStop is decimal longStop && candle.LowPrice <= longStop)
		{
			SellMarket(Position);
			_lastBuyStop = null;
		}

		ShiftSmaHistory(currentSma);
		_previousClose = candle.ClosePrice;
	}

	private void ShiftSmaHistory(decimal current)
	{
		_smaPrev3 = _smaPrev2;
		_smaPrev2 = _smaPrev1;
		_smaPrev1 = current;
	}
}

