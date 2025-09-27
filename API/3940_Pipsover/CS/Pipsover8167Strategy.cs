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
/// Port of the MetaTrader 4 expert "Pipsover" (build 8167) that combines a Chaikin oscillator spike
/// with a pullback to the 20-period simple moving average on the previous candle.
/// </summary>
public class Pipsover8167Strategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _openLevel;
	private readonly StrategyParam<decimal> _closeLevel;
	private readonly StrategyParam<int> _chaikinFastLength;
	private readonly StrategyParam<int> _chaikinSlowLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma = null!;
	private AccumulationDistributionLine _adl = null!;
	private ExponentialMovingAverage _chaikinFast = null!;
	private ExponentialMovingAverage _chaikinSlow = null!;

	private bool _hasPreviousCandle;
	private decimal _previousOpen;
	private decimal _previousHigh;
	private decimal _previousLow;
	private decimal _previousClose;
	private decimal _previousSma;
	private decimal _previousChaikin;

	private bool _targetsActive;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	/// <summary>
	/// Trading volume used for each market order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Length of the simple moving average used as the pullback filter.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in instrument price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Absolute Chaikin oscillator threshold that allows opening new positions.
	/// </summary>
	public decimal OpenLevel
	{
		get => _openLevel.Value;
		set => _openLevel.Value = value;
	}

	/// <summary>
	/// Absolute Chaikin oscillator threshold that enforces exits on existing positions.
	/// </summary>
	public decimal CloseLevel
	{
		get => _closeLevel.Value;
		set => _closeLevel.Value = value;
	}

	/// <summary>
	/// Fast exponential moving average length for the Chaikin oscillator reconstruction.
	/// </summary>
	public int ChaikinFastLength
	{
		get => _chaikinFastLength.Value;
		set => _chaikinFastLength.Value = value;
	}

	/// <summary>
	/// Slow exponential moving average length for the Chaikin oscillator reconstruction.
	/// </summary>
	public int ChaikinSlowLength
	{
		get => _chaikinSlowLength.Value;
		set => _chaikinSlowLength.Value = value;
	}

	/// <summary>
	/// Candle type (time-frame) used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Pipsover8167Strategy"/> class.
	/// </summary>
	public Pipsover8167Strategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Order size transmitted to market orders", "Trading");

		_maLength = Param(nameof(MaLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("SMA Length", "Simple moving average period applied to closes", "Indicators");

		_stopLossPoints = Param(nameof(StopLossPoints), 70m)
		.SetGreaterThanZero()
		.SetDisplay("Stop-Loss Points", "Protective stop distance measured in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 140m)
		.SetGreaterThanZero()
		.SetDisplay("Take-Profit Points", "Profit target distance measured in price steps", "Risk");

		_openLevel = Param(nameof(OpenLevel), 55m)
		.SetGreaterThanZero()
		.SetDisplay("Open Level", "Chaikin oscillator magnitude required for entries", "Chaikin");

		_closeLevel = Param(nameof(CloseLevel), 90m)
		.SetGreaterThanZero()
		.SetDisplay("Close Level", "Chaikin oscillator magnitude that closes trades", "Chaikin");

		_chaikinFastLength = Param(nameof(ChaikinFastLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Chaikin Fast Length", "Fast EMA applied to the accumulation/distribution line", "Chaikin");

		_chaikinSlowLength = Param(nameof(ChaikinSlowLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Chaikin Slow Length", "Slow EMA applied to the accumulation/distribution line", "Chaikin");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for candle subscriptions", "Data");
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

	_hasPreviousCandle = false;
	_previousOpen = 0m;
	_previousHigh = 0m;
	_previousLow = 0m;
	_previousClose = 0m;
	_previousSma = 0m;
	_previousChaikin = 0m;

	_targetsActive = false;
	_stopPrice = 0m;
	_takeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	Volume = TradeVolume;

	_sma = new SimpleMovingAverage { Length = MaLength };
	_adl = new AccumulationDistributionLine();
	_chaikinFast = new ExponentialMovingAverage { Length = ChaikinFastLength };
	_chaikinSlow = new ExponentialMovingAverage { Length = ChaikinSlowLength };

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(_adl, _sma, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _sma);
	DrawIndicator(area, _adl);
	DrawOwnTrades(area);
	}
	}

	private void ProcessCandle(ICandleMessage candle, decimal adlValue, decimal smaValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var fastResult = _chaikinFast.Process(new DecimalIndicatorValue(_chaikinFast, adlValue));
	var slowResult = _chaikinSlow.Process(new DecimalIndicatorValue(_chaikinSlow, adlValue));
	var chaikinValue = fastResult.ToDecimal() - slowResult.ToDecimal();

	if (!_chaikinFast.IsFormed || !_chaikinSlow.IsFormed || !_sma.IsFormed)
	{
	StorePreviousState(candle, smaValue, chaikinValue);
	return;
	}

	if (!_hasPreviousCandle)
	{
	StorePreviousState(candle, smaValue, chaikinValue);
	return;
	}

	var step = Security?.PriceStep ?? 1m;
	var stopLossDistance = StopLossPoints * step;
	var takeProfitDistance = TakeProfitPoints * step;

	if (_targetsActive)
	{
	if (Position > 0m)
	{
	if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
	{
	SellMarket(Position);
	DisableTargets();
	}
	}
	else if (Position < 0m)
	{
	if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
	{
	BuyMarket(Math.Abs(Position));
	DisableTargets();
	}
	}
	else
	{
	DisableTargets();
	}
	}

	if (!IsFormedAndOnlineAndAllowTrading())
	{
	StorePreviousState(candle, smaValue, chaikinValue);
	return;
	}

	var previousBullish = _previousClose > _previousOpen;
	var previousBearish = _previousClose < _previousOpen;

	if (Position > 0m)
	{
	var shouldExitLong = previousBearish && _previousHigh > _previousSma && _previousChaikin > CloseLevel;
	if (shouldExitLong)
	{
	SellMarket(Position);
	DisableTargets();
	}
	}
	else if (Position < 0m)
	{
	var shouldExitShort = previousBullish && _previousLow < _previousSma && _previousChaikin < -CloseLevel;
	if (shouldExitShort)
	{
	BuyMarket(Math.Abs(Position));
	DisableTargets();
	}
	}
	else
	{
	CancelActiveOrders();

	var allowLong = previousBullish && _previousLow < _previousSma && _previousChaikin < -OpenLevel;
	var allowShort = previousBearish && _previousHigh > _previousSma && _previousChaikin > OpenLevel;

	if (allowLong)
	{
	BuyMarket();

	var entryPrice = candle.ClosePrice;
	_stopPrice = entryPrice - stopLossDistance;
	_takeProfitPrice = entryPrice + takeProfitDistance;
	_targetsActive = true;
	}
	else if (allowShort)
	{
	SellMarket();

	var entryPrice = candle.ClosePrice;
	_stopPrice = entryPrice + stopLossDistance;
	_takeProfitPrice = entryPrice - takeProfitDistance;
	_targetsActive = true;
	}
	}

	StorePreviousState(candle, smaValue, chaikinValue);
	}

	private void StorePreviousState(ICandleMessage candle, decimal smaValue, decimal chaikinValue)
	{
	_previousOpen = candle.OpenPrice;
	_previousHigh = candle.HighPrice;
	_previousLow = candle.LowPrice;
	_previousClose = candle.ClosePrice;
	_previousSma = smaValue;
	_previousChaikin = chaikinValue;
	_hasPreviousCandle = true;
	}

	private void DisableTargets()
	{
	_targetsActive = false;
	_stopPrice = 0m;
	_takeProfitPrice = 0m;
	}
}

