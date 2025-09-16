using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MADX-07 strategy converted from MQL4.
/// Combines moving averages and ADX for trend following entries.
/// </summary>
public class Madx07AdxMaStrategy : Strategy
{
	private readonly StrategyParam<int> _bigMaPeriod;
	private readonly StrategyParam<MovingAverageTypes> _bigMaType;
	private readonly StrategyParam<int> _smallMaPeriod;
	private readonly StrategyParam<MovingAverageTypes> _smallMaType;
	private readonly StrategyParam<int> _maDifference;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxMainLevel;
	private readonly StrategyParam<decimal> _adxPlusLevel;
	private readonly StrategyParam<decimal> _adxMinusLevel;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _closeProfit;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevAdx;
	private decimal _prevPlus;
	private decimal _prevMinus;
	private decimal _prevSmallMa;
	private decimal _prevLow;
	private decimal _prevHigh;
	private decimal _entryPrice;
	private Order _takeProfitOrder;

	/// <summary>
	/// Period of the slower moving average.
	/// </summary>
	public int BigMaPeriod { get => _bigMaPeriod.Value; set => _bigMaPeriod.Value = value; }

	/// <summary>
	/// Type of the slower moving average.
	/// </summary>
	public MovingAverageTypes BigMaType { get => _bigMaType.Value; set => _bigMaType.Value = value; }

	/// <summary>
	/// Period of the faster moving average.
	/// </summary>
	public int SmallMaPeriod { get => _smallMaPeriod.Value; set => _smallMaPeriod.Value = value; }

	/// <summary>
	/// Type of the faster moving average.
	/// </summary>
	public MovingAverageTypes SmallMaType { get => _smallMaType.Value; set => _smallMaType.Value = value; }

	/// <summary>
	/// Minimal distance in points between price and fast MA.
	/// </summary>
	public int MaDifference { get => _maDifference.Value; set => _maDifference.Value = value; }

	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }

	/// <summary>
	/// Minimal ADX main line value.
	/// </summary>
	public decimal AdxMainLevel { get => _adxMainLevel.Value; set => _adxMainLevel.Value = value; }

	/// <summary>
	/// Minimal +DI level.
	/// </summary>
	public decimal AdxPlusLevel { get => _adxPlusLevel.Value; set => _adxPlusLevel.Value = value; }

	/// <summary>
	/// Minimal -DI level.
	/// </summary>
	public decimal AdxMinusLevel { get => _adxMinusLevel.Value; set => _adxMinusLevel.Value = value; }

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Profit in points to close the position early.
	/// </summary>
	public decimal CloseProfit { get => _closeProfit.Value; set => _closeProfit.Value = value; }

	/// <summary>
	/// Trade volume in lots.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public Madx07AdxMaStrategy()
	{
		_bigMaPeriod = Param(nameof(BigMaPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Big MA Period", "Period of the slower moving average", "MA")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_bigMaType = Param(nameof(BigMaType), MovingAverageTypes.Smoothed)
			.SetDisplay("Big MA Type", "Type of the slower moving average", "MA");

		_smallMaPeriod = Param(nameof(SmallMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Small MA Period", "Period of the faster moving average", "MA")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 2);

		_smallMaType = Param(nameof(SmallMaType), MovingAverageTypes.Exponential)
			.SetDisplay("Small MA Type", "Type of the faster moving average", "MA");

		_maDifference = Param(nameof(MaDifference), 5)
			.SetGreaterThanZero()
			.SetDisplay("MA Difference", "Minimal distance between price and fast MA in points", "Filters");

		_adxPeriod = Param(nameof(AdxPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX indicator", "ADX")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_adxMainLevel = Param(nameof(AdxMainLevel), 13m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Level", "Minimal ADX main value", "ADX");

		_adxPlusLevel = Param(nameof(AdxPlusLevel), 13m)
			.SetGreaterThanZero()
			.SetDisplay("+DI Level", "Minimal +DI value", "ADX");

		_adxMinusLevel = Param(nameof(AdxMinusLevel), 14m)
			.SetGreaterThanZero()
			.SetDisplay("-DI Level", "Minimal -DI value", "ADX");

		_takeProfit = Param(nameof(TakeProfit), 299m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance in points", "Risk");

		_closeProfit = Param(nameof(CloseProfit), 13m)
			.SetGreaterThanZero()
			.SetDisplay("Close Profit", "Early exit profit in points", "Risk");

		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Trade volume in lots", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevAdx = 0;
		_prevPlus = 0;
		_prevMinus = 0;
		_prevSmallMa = 0;
		_prevLow = 0;
		_prevHigh = 0;
		_entryPrice = 0;
		_takeProfitOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		var bigMa = new MovingAverage { Length = BigMaPeriod, Type = BigMaType };
		var smallMa = new MovingAverage { Length = SmallMaPeriod, Type = SmallMaType };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		// Subscribe to candles and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(adx, bigMa, smallMa, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue bigMaValue, IIndicatorValue smallMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure indicator values are ready
		if (!adxValue.IsFinal || !bigMaValue.IsFinal || !smallMaValue.IsFinal)
			return;

		// Cast ADX value and extract components
		var adxVal = (AverageDirectionalIndexValue)adxValue;
		if (adxVal.Adx is not decimal adx || adxVal.PlusDi is not decimal plus || adxVal.MinusDi is not decimal minus)
			return;

		var bigMa = bigMaValue.GetValue<decimal>();
		var smallMa = smallMaValue.GetValue<decimal>();
		var step = Security.PriceStep ?? 1m;
		var diff = MaDifference * step;

		// Initialize previous values
		if (_prevSmallMa == 0)
		{
			_prevAdx = adx;
			_prevPlus = plus;
			_prevMinus = minus;
			_prevSmallMa = smallMa;
			_prevLow = candle.LowPrice;
			_prevHigh = candle.HighPrice;
			return;
		}

		// Close position when profit target reached
		if (Position != 0)
		{
			var profit = (candle.ClosePrice - _entryPrice) * (Position > 0 ? 1 : -1);
			if (profit >= CloseProfit * step)
			{
				ClosePosition();
				return;
			}
		}

		// Trading logic only when flat
		if (Position == 0)
		{
			// Buy conditions
			if (candle.ClosePrice > bigMa &&
				_prevLow - _prevSmallMa > diff &&
				candle.LowPrice - smallMa > diff &&
				smallMa > bigMa &&
				adx > _prevAdx && adx > AdxMainLevel &&
				plus > _prevPlus && plus > AdxPlusLevel &&
				minus < _prevMinus && minus < AdxMinusLevel)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				// Place take profit order
				if (TakeProfit > 0)
					_takeProfitOrder = SellLimit(_entryPrice + TakeProfit * step, Volume);
			}
			// Sell conditions
			else if (candle.ClosePrice < bigMa &&
				_prevSmallMa - _prevHigh > diff &&
				smallMa - candle.HighPrice > diff &&
				smallMa < bigMa &&
				adx > _prevAdx && adx > AdxMainLevel &&
				minus > _prevMinus && minus > AdxMinusLevel &&
				plus < _prevPlus && plus < AdxPlusLevel)
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				if (TakeProfit > 0)
					_takeProfitOrder = BuyLimit(_entryPrice - TakeProfit * step, Volume);
			}
		}

		_prevAdx = adx;
		_prevPlus = plus;
		_prevMinus = minus;
		_prevSmallMa = smallMa;
		_prevLow = candle.LowPrice;
		_prevHigh = candle.HighPrice;
	}

	private void ClosePosition()
	{
		// Cancel pending take profit order if exists
		if (_takeProfitOrder != null)
		{
			CancelOrder(_takeProfitOrder);
			_takeProfitOrder = null;
		}

		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}
}
