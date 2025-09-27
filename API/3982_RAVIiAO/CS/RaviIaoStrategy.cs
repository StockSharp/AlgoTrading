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
/// Port of the MetaTrader 4 expert advisor "RAVIiAO" that combines the RAVI oscillator and the Acceleration/Deceleration oscillator.
/// </summary>
public class RaviIaoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _tradeVolume;

	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;
	private AwesomeOscillator _ao = null!;
	private SimpleMovingAverage _aoAverage = null!;

	private decimal? _prevRavi;
	private decimal? _prevPrevRavi;
	private decimal? _prevAc;
	private decimal? _prevPrevAc;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private bool _isLongPosition;
	private decimal _priceStep;

	/// <summary>
	/// Type of candles used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast moving average length for the RAVI oscillator.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow moving average length for the RAVI oscillator.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Threshold for bullish or bearish confirmation of the RAVI oscillator (percentage value).
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in instrument points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Market order volume sent on each entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RaviIaoStrategy"/>.
	/// </summary>
	public RaviIaoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Time-frame for analysis", "General");

		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast SMA period inside RAVI", "RAVI");

		_slowLength = Param(nameof(SlowLength), 72)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow SMA period inside RAVI", "RAVI");

		_threshold = Param(nameof(Threshold), 0.3m)
			.SetDisplay("RAVI Threshold", "Minimum absolute RAVI value to confirm the trend", "Signals");

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Stop-loss distance in price points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Take-profit distance in price points", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");
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

		_prevRavi = null;
		_prevPrevRavi = null;
		_prevAc = null;
		_prevPrevAc = null;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_isLongPosition = false;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
			throw new InvalidOperationException("Security must expose a positive price step.");

		_fastMa = new SimpleMovingAverage { Length = FastLength };
		_slowMa = new SimpleMovingAverage { Length = SlowLength };
		_ao = new AwesomeOscillator();
		_aoAverage = new SimpleMovingAverage { Length = 5 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Exit early if an active position hits its protective boundaries on this candle.
		CheckProtectiveLevels(candle);

		// Feed the candle into the Awesome Oscillator to obtain the raw momentum estimate.
		var aoValue = _ao.Process(candle.HighPrice, candle.LowPrice);
		if (!aoValue.IsFinal)
		return;

		// Smooth the oscillator with a 5-period SMA to match the MT4 Acceleration/Deceleration formula.
		var aoAverageValue = _aoAverage.Process(aoValue.GetValue<decimal>());
		if (!aoAverageValue.IsFinal)
		return;

		var ac = aoValue.GetValue<decimal>() - aoAverageValue.GetValue<decimal>();

		if (slowValue == 0m)
		{
		UpdateHistory(null, ac);
		return;
		}

		var ravi = 100m * (fastValue - slowValue) / slowValue;

		if (_prevRavi is decimal prevRavi &&
		_prevPrevRavi is decimal prevPrevRavi &&
		_prevAc is decimal prevAc &&
		_prevPrevAc is decimal prevPrevAc &&
		Position == 0 &&
		IsFormedAndOnlineAndAllowTrading())
		{
		var bullish = prevAc > prevPrevAc && prevPrevAc > 0m && prevRavi > prevPrevRavi && prevRavi > Threshold;
		var bearish = prevAc < prevPrevAc && prevPrevAc < 0m && prevRavi < prevPrevRavi && prevRavi < -Threshold;

		if (bullish)
		EnterLong(candle.ClosePrice);
		else if (bearish)
		EnterShort(candle.ClosePrice);
		}

		UpdateHistory(ravi, ac);
	}

	private void EnterLong(decimal price)
	{
		BuyMarket(TradeVolume);
		_isLongPosition = true;
		InitializeProtection(price);
	}

	private void EnterShort(decimal price)
	{
		SellMarket(TradeVolume);
		_isLongPosition = false;
		InitializeProtection(price);
	}

	private void InitializeProtection(decimal entryPrice)
	{
		_entryPrice = entryPrice;
		var offset = StopLossPoints * _priceStep;
		_stopPrice = StopLossPoints > 0m ? entryPrice + (_isLongPosition ? -offset : offset) : null;
		var takeOffset = TakeProfitPoints * _priceStep;
		_takePrice = TakeProfitPoints > 0m ? entryPrice + (_isLongPosition ? takeOffset : -takeOffset) : null;
	}

	private void CheckProtectiveLevels(ICandleMessage candle)
	{
		if (Position == 0 || _entryPrice is null)
		return;

		if (_isLongPosition)
		{
		if (_stopPrice is decimal stop && candle.LowPrice <= stop)
		{
		SellMarket(Math.Abs(Position));
		ResetProtection();
		return;
		}

		if (_takePrice is decimal take && candle.HighPrice >= take)
		{
		SellMarket(Math.Abs(Position));
		ResetProtection();
		}
		}
		else
		{
		if (_stopPrice is decimal stop && candle.HighPrice >= stop)
		{
		BuyMarket(Math.Abs(Position));
		ResetProtection();
		return;
		}

		if (_takePrice is decimal take && candle.LowPrice <= take)
		{
		BuyMarket(Math.Abs(Position));
		ResetProtection();
		}
		}
	}

	private void ResetProtection()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_isLongPosition = false;
	}

	private void UpdateHistory(decimal? ravi, decimal ac)
	{
		_prevPrevRavi = _prevRavi;
		_prevRavi = ravi;
		_prevPrevAc = _prevAc;
		_prevAc = ac;
	}
}

