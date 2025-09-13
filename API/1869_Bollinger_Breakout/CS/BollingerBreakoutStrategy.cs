namespace StockSharp.Samples.Strategies;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy trading Bollinger Band breakouts with additional RSI, EMA and MACD filters.
/// Executes one trade per breakout and trails stop at the middle band.
/// </summary>
public class BollingerBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _breakoutFactor;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private bool _breakoutFlag;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevClose;
	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _hasPrev;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Minimum band width to consider a breakout.
	/// </summary>
	public decimal BreakoutFactor
	{
		get => _breakoutFactor.Value;
		set => _breakoutFactor.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BollingerBreakoutStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 18)
			.SetDisplay("BB Length", "Bollinger Bands length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 2);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("BB Deviation", "Bollinger Bands deviation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_breakoutFactor = Param(nameof(BreakoutFactor), 0.0015m)
			.SetDisplay("Breakout Factor", "Minimum width of bands", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.0005m, 0.003m, 0.0005m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 100)
			.SetDisplay("Take Profit (pips)", "Distance for profit target", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of working candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_breakoutFlag = false;
		_prevUpper = 0m;
		_prevLower = 0m;
		_prevClose = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerDeviation
		};

		var ema = new ExponentialMovingAverage { Length = 3 };

		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = 12,
			LongPeriod = 26,
			SignalPeriod = 9
		};

		var rsi = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bollinger, ema, macd, rsi, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal emaValue, decimal macdValue, decimal signal, decimal histogram, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var diff = upper - lower;

		// Reset flag when band width contracts
		if (_breakoutFlag && diff < BreakoutFactor)
			_breakoutFlag = false;

		if (Position > 0)
		{
			// Check stop or take profit for long position
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				ClosePosition();

			// Update trailing stop to middle band
			_stopPrice = middle;
		}
		else if (Position < 0)
		{
			// Check stop or take profit for short position
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				ClosePosition();

			// Update trailing stop to middle band
			_stopPrice = middle;
		}
		else if (!_breakoutFlag && _hasPrev)
		{
			var breakout = diff >= BreakoutFactor;
			var buySignal = breakout && macdValue > 0m && rsiValue > 50m && emaValue > middle && _prevClose >= _prevUpper;
			var sellSignal = breakout && macdValue < 0m && rsiValue < 50m && emaValue < middle && _prevClose <= _prevLower;

			if (buySignal)
			{
				BuyMarket();
				_stopPrice = middle;
				_takePrice = candle.ClosePrice + TakeProfitPips * Security.Step;
				_breakoutFlag = true;
			}
			else if (sellSignal)
			{
				SellMarket();
				_stopPrice = middle;
				_takePrice = candle.ClosePrice - TakeProfitPips * Security.Step;
				_breakoutFlag = true;
			}
		}

		// Store current values for next candle analysis
		_prevUpper = upper;
		_prevLower = lower;
		_prevClose = candle.ClosePrice;
		_hasPrev = true;
	}
}
