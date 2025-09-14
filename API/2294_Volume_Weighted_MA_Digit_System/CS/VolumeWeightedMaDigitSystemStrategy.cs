using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Volume Weighted MA Digit System indicator.
/// Entry: price crosses above the upper VWMA band.
/// Exit/Short entry: price crosses below the lower VWMA band.
/// </summary>
public class VolumeWeightedMaDigitSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _vwmaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private readonly VolumeWeightedMovingAverage _vwmaHigh = new();
	private readonly VolumeWeightedMovingAverage _vwmaLow = new();

	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private bool _isFirst = true;

	/// <summary>
	/// VWMA calculation period.
	/// </summary>
	public int VwmaPeriod
	{
		get => _vwmaPeriod.Value;
		set => _vwmaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="VolumeWeightedMaDigitSystemStrategy"/>.
	/// </summary>
	public VolumeWeightedMaDigitSystemStrategy()
	{
		_vwmaPeriod = Param(nameof(VwmaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("VWMA Period", "Length of the VWMA indicator", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe for analysis", "Parameters");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss value in points", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100m, 3000m, 100m);

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit value in points", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100m, 5000m, 100m);
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
		_prevClose = 0;
		_prevUpper = 0;
		_prevLower = 0;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_vwmaHigh.Length = VwmaPeriod;
		_vwmaLow.Length = VwmaPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(StopLoss, UnitTypes.Point),
			takeProfit: new Unit(TakeProfit, UnitTypes.Point)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _vwmaHigh, "VWMA High");
			DrawIndicator(area, _vwmaLow, "VWMA Low");
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Ignore unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure strategy is ready
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Update indicators with high and low prices
		var highValue = _vwmaHigh.Process(candle.HighPrice, candle.OpenTime, true);
		var lowValue = _vwmaLow.Process(candle.LowPrice, candle.OpenTime, true);

		if (!highValue.IsFinal || !lowValue.IsFinal)
			return;

		var upper = highValue.ToDecimal();
		var lower = lowValue.ToDecimal();

		if (_isFirst)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upper;
			_prevLower = lower;
			_isFirst = false;
			return;
		}

		var crossUp = _prevClose <= _prevUpper && candle.ClosePrice > upper;
		var crossDown = _prevClose >= _prevLower && candle.ClosePrice < lower;

		if (crossUp)
		{
			// Close short positions and open long
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			if (Position == 0)
				BuyMarket();
		}
		else if (crossDown)
		{
			// Close long positions and open short
			if (Position > 0)
				SellMarket(Position);
			if (Position == 0)
				SellMarket();
		}

		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
	}
}
