using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Smart Money Concepts with Bollinger Bands breakout strategy.
/// </summary>
public class SmcBbBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _orderBlockLength;
	private readonly StrategyParam<int> _swingLength;
	private readonly StrategyParam<bool> _momentumFilter;
	private readonly StrategyParam<decimal> _momentumBodyPercent;

	private BollingerBands _bollinger;
	private Highest _orderBlockHigh;
	private Lowest _orderBlockLow;
	private Highest _swingHigh;
	private Lowest _swingLow;

	private decimal _previousHigh;
	private decimal _previousLow;
	private decimal _lastSwingHigh;
	private decimal _lastSwingLow;
	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevBasis;
	private bool _hasPrevBands;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }

	/// <summary>
	/// Bollinger Bands width multiplier.
	/// </summary>
	public decimal BbMultiplier { get => _bbMultiplier.Value; set => _bbMultiplier.Value = value; }

	/// <summary>
	/// Order block lookback length.
	/// </summary>
	public int OrderBlockLength { get => _orderBlockLength.Value; set => _orderBlockLength.Value = value; }

	/// <summary>
	/// Swing lookback length.
	/// </summary>
	public int SwingLength { get => _swingLength.Value; set => _swingLength.Value = value; }

	/// <summary>
	/// Require momentum candle for entry.
	/// </summary>
	public bool MomentumFilter { get => _momentumFilter.Value; set => _momentumFilter.Value = value; }

	/// <summary>
	/// Minimum body percent to treat candle as momentum.
	/// </summary>
	public decimal MomentumBodyPercent { get => _momentumBodyPercent.Value; set => _momentumBodyPercent.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public SmcBbBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bbLength = Param(nameof(BbLength), 55)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 5);

		_bbMultiplier = Param(nameof(BbMultiplier), 2m)
			.SetRange(0.5m, 5m)
			.SetDisplay("BB Multiplier", "Bollinger width multiplier", "Bollinger");

		_orderBlockLength = Param(nameof(OrderBlockLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Order Block Length", "Lookback for order block", "SMC");

		_swingLength = Param(nameof(SwingLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Swing Length", "Lookback for swings", "SMC");

		_momentumFilter = Param(nameof(MomentumFilter), true)
			.SetDisplay("Momentum Filter", "Require momentum candle for entry", "Momentum");

		_momentumBodyPercent = Param(nameof(MomentumBodyPercent), 0.7m)
			.SetRange(0.01m, 1m)
			.SetDisplay("Momentum Body %", "Minimum body percentage", "Momentum");
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

		_previousHigh = default;
		_previousLow = default;
		_lastSwingHigh = default;
		_lastSwingLow = default;
		_prevClose = default;
		_prevUpper = default;
		_prevLower = default;
		_prevBasis = default;
		_hasPrevBands = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bollinger = new BollingerBands { Length = BbLength, Width = BbMultiplier };
		_orderBlockHigh = new Highest { Length = OrderBlockLength };
		_orderBlockLow = new Lowest { Length = OrderBlockLength };
		_swingHigh = new Highest { Length = SwingLength };
		_swingLow = new Lowest { Length = SwingLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx([_orderBlockHigh, _orderBlockLow, _swingHigh, _swingLow], ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bbValue = _bollinger.Process(candle);
		var bb = (BollingerBandsValue)bbValue;

		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower || bb.MovingAverage is not decimal basis)
			return;

		if (values[0].ToNullableDecimal() is not decimal obHigh)
			return;
		if (values[1].ToNullableDecimal() is not decimal obLow)
			return;
		if (values[2].ToNullableDecimal() is not decimal swingHigh)
			return;
		if (values[3].ToNullableDecimal() is not decimal swingLow)
			return;

		if (_lastSwingHigh == default)
		{
			_lastSwingHigh = swingHigh;
			_previousHigh = swingHigh;
		}
		else if (swingHigh > _lastSwingHigh)
		{
			_previousHigh = _lastSwingHigh;
			_lastSwingHigh = swingHigh;
		}

		if (_lastSwingLow == default)
		{
			_lastSwingLow = swingLow;
			_previousLow = swingLow;
		}
		else if (swingLow < _lastSwingLow || _lastSwingLow == 0)
		{
			_previousLow = _lastSwingLow;
			_lastSwingLow = swingLow;
		}

		var range = candle.HighPrice - candle.LowPrice;
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var bodyPercent = range > 0m ? body / range : 0m;
		var longMomentum = bodyPercent >= MomentumBodyPercent && candle.ClosePrice > candle.OpenPrice;
		var shortMomentum = bodyPercent >= MomentumBodyPercent && candle.ClosePrice < candle.OpenPrice;

		var shiftBullish = candle.ClosePrice > _previousHigh;
		var shiftBearish = candle.ClosePrice < _previousLow;

		var crossoverUp = _hasPrevBands && _prevClose <= _prevUpper && candle.ClosePrice > upper;
		var crossunderDown = _hasPrevBands && _prevClose >= _prevLower && candle.ClosePrice < lower;
		var crossunderBasis = _hasPrevBands && _prevClose >= _prevBasis && candle.ClosePrice < basis;
		var crossoverBasis = _hasPrevBands && _prevClose <= _prevBasis && candle.ClosePrice > basis;

		var longCondition = crossoverUp && shiftBullish && (!MomentumFilter || longMomentum);
		var shortCondition = crossunderDown && shiftBearish && (!MomentumFilter || shortMomentum);
		var exitLongCondition = crossunderBasis || candle.ClosePrice < obLow * 0.99m;
		var exitShortCondition = crossoverBasis || candle.ClosePrice > obHigh * 1.01m;

		if (longCondition && Position <= 0)
			BuyMarket();

		if (shortCondition && Position >= 0)
			SellMarket();

		if (exitLongCondition && Position > 0)
			SellMarket();

		if (exitShortCondition && Position < 0)
			BuyMarket();

		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
		_prevBasis = basis;
		_hasPrevBands = true;
	}
}
