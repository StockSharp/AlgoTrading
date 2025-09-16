using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pipsover strategy rebuilt on the high level StockSharp API.
/// The strategy opens positions when a strong Chaikin oscillator spike aligns with a pullback to the 20-period SMA on the previous candle.
/// Protective stop-loss and take-profit levels are recreated using price step distances defined in the original Expert Advisor.
/// </summary>
public class PipsoverStrategy : Strategy
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

	private SimpleMovingAverage _sma;
	private AccumulationDistributionLine _accumulationDistribution;
	private ExponentialMovingAverage _chaikinFast;
	private ExponentialMovingAverage _chaikinSlow;

	private decimal _prevOpen;
	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevClose;
	private decimal _prevSma;
	private decimal _prevChaikin;
	private bool _hasPrevCandle;

	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private bool _hasTargets;

	/// <summary>
	/// Trading volume used for market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Period of the simple moving average that acts as a pullback filter.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Chaikin oscillator level required to allow new entries.
	/// </summary>
	public decimal OpenLevel
	{
		get => _openLevel.Value;
		set => _openLevel.Value = value;
	}

	/// <summary>
	/// Chaikin oscillator level that closes existing positions.
	/// </summary>
	public decimal CloseLevel
	{
		get => _closeLevel.Value;
		set => _closeLevel.Value = value;
	}

	/// <summary>
	/// Fast EMA period for Chaikin oscillator reconstruction.
	/// </summary>
	public int ChaikinFastLength
	{
		get => _chaikinFastLength.Value;
		set => _chaikinFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period for Chaikin oscillator reconstruction.
	/// </summary>
	public int ChaikinSlowLength
	{
		get => _chaikinSlowLength.Value;
		set => _chaikinSlowLength.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PipsoverStrategy"/> class.
	/// </summary>
	public PipsoverStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order size used for market entries", "Trading");

		_maLength = Param(nameof(MaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Simple moving average length", "Indicators");

		_stopLossPoints = Param(nameof(StopLossPoints), 65m)
			.SetGreaterThanZero()
			.SetDisplay("Stop-Loss Points", "Stop-loss distance expressed in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Take-Profit Points", "Take-profit distance expressed in price steps", "Risk");

		_openLevel = Param(nameof(OpenLevel), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Open Level", "Chaikin oscillator threshold for entries", "Chaikin");

		_closeLevel = Param(nameof(CloseLevel), 125m)
			.SetGreaterThanZero()
			.SetDisplay("Close Level", "Chaikin oscillator threshold for exits", "Chaikin");

		_chaikinFastLength = Param(nameof(ChaikinFastLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Chaikin Fast Length", "Fast EMA length for Chaikin oscillator", "Chaikin");

		_chaikinSlowLength = Param(nameof(ChaikinSlowLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Chaikin Slow Length", "Slow EMA length for Chaikin oscillator", "Chaikin");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "Data");
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

		// Reset cached candle and indicator state.
		_hasPrevCandle = false;
		_prevOpen = 0m;
		_prevHigh = 0m;
		_prevLow = 0m;
		_prevClose = 0m;
		_prevSma = 0m;
		_prevChaikin = 0m;

		// Reset protective price levels.
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
		_hasTargets = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Apply configured trading volume to base strategy.
		Volume = TradeVolume;

		// Prepare indicators that replicate the MQL Expert Advisor logic.
		_sma = new SimpleMovingAverage { Length = MaLength };
		_accumulationDistribution = new AccumulationDistributionLine();
		_chaikinFast = new ExponentialMovingAverage { Length = ChaikinFastLength };
		_chaikinSlow = new ExponentialMovingAverage { Length = ChaikinSlowLength };

		// Subscribe to candle data and bind indicators.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_accumulationDistribution, _sma, ProcessCandle)
			.Start();

		// Optional charting for visual validation.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawIndicator(area, _accumulationDistribution);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal adlValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Rebuild Chaikin oscillator values via EMA of the ADL indicator.
		var fastResult = _chaikinFast.Process(new DecimalIndicatorValue(_chaikinFast, adlValue));
		var slowResult = _chaikinSlow.Process(new DecimalIndicatorValue(_chaikinSlow, adlValue));
		var chaikinValue = fastResult.ToDecimal() - slowResult.ToDecimal();

		// Wait for all indicators to be fully formed before trading.
		if (!_chaikinFast.IsFormed || !_chaikinSlow.IsFormed || !_sma.IsFormed)
		{
			UpdateState(candle, chaikinValue, smaValue);
			return;
		}

		if (!_hasPrevCandle)
		{
			UpdateState(candle, chaikinValue, smaValue);
			return;
		}

		var step = Security?.PriceStep ?? 1m;
		var stopLossDistance = StopLossPoints * step;
		var takeProfitDistance = TakeProfitPoints * step;

		// Check protective stop-loss and take-profit targets before new decisions.
		if (_hasTargets)
		{
			if (Position > 0)
			{
				if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
				{
					SellMarket(Position);
					ResetTargets();
				}
			}
			else if (Position < 0)
			{
				if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
				{
					BuyMarket(Math.Abs(Position));
					ResetTargets();
				}
			}
			else
			{
				ResetTargets();
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateState(candle, chaikinValue, smaValue);
			return;
		}

		var prevBullish = _prevClose > _prevOpen;
		var prevBearish = _prevClose < _prevOpen;

		if (Position > 0)
		{
			var shouldExitLong = prevBearish && _prevHigh > _prevSma && _prevChaikin > CloseLevel;
			if (shouldExitLong)
			{
				SellMarket(Position);
				ResetTargets();
			}
		}
		else if (Position < 0)
		{
			var shouldExitShort = prevBullish && _prevLow < _prevSma && _prevChaikin < -CloseLevel;
			if (shouldExitShort)
			{
				BuyMarket(Math.Abs(Position));
				ResetTargets();
			}
		}
		else
		{
			// No position is open, evaluate entry signals.
			CancelActiveOrders();

			var allowLong = prevBullish && _prevLow < _prevSma && _prevChaikin < -OpenLevel;
			var allowShort = prevBearish && _prevHigh > _prevSma && _prevChaikin > OpenLevel;

			if (allowLong)
			{
				BuyMarket();

				var entryPrice = candle.ClosePrice;
				_stopPrice = entryPrice - stopLossDistance;
				_takeProfitPrice = entryPrice + takeProfitDistance;
				_hasTargets = true;
			}
			else if (allowShort)
			{
				SellMarket();

				var entryPrice = candle.ClosePrice;
				_stopPrice = entryPrice + stopLossDistance;
				_takeProfitPrice = entryPrice - takeProfitDistance;
				_hasTargets = true;
			}
		}

		UpdateState(candle, chaikinValue, smaValue);
	}

	private void UpdateState(ICandleMessage candle, decimal chaikinValue, decimal smaValue)
	{
		// Store previous candle data for next iteration checks.
		_prevOpen = candle.OpenPrice;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;
		_prevSma = smaValue;
		_prevChaikin = chaikinValue;
		_hasPrevCandle = true;
	}

	private void ResetTargets()
	{
		// Clear stop-loss and take-profit levels once position is closed.
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
		_hasTargets = false;
	}
}
