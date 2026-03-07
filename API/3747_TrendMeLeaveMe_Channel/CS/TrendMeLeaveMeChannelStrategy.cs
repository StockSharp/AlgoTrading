using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pending stop-order breakout strategy inspired by the TrendMeLeaveMe expert advisor.
/// Uses a regression trend line as the dynamic channel center and offsets upper/lower boundaries.
/// When price trades near the trend line it places stop orders that include static stop-loss and take-profit distances.
/// </summary>
public class TrendMeLeaveMeChannelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _buyStepUpper;
	private readonly StrategyParam<int> _buyStepLower;
	private readonly StrategyParam<int> _sellStepUpper;
	private readonly StrategyParam<int> _sellStepLower;
	private readonly StrategyParam<int> _buyTakeProfitSteps;
	private readonly StrategyParam<int> _buyStopLossSteps;
	private readonly StrategyParam<int> _sellTakeProfitSteps;
	private readonly StrategyParam<int> _sellStopLossSteps;
	private readonly StrategyParam<decimal> _buyVolume;
	private readonly StrategyParam<decimal> _sellVolume;

	private decimal _entryPrice;
	private decimal? _activeStop;
	private decimal? _activeTake;
	private int _activeDirection; // 1=long, -1=short, 0=flat

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public TrendMeLeaveMeChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General");

		_trendLength = Param(nameof(TrendLength), 100)
		.SetGreaterThanZero()
		.SetDisplay("Trend Length", "Number of candles used in the regression trend line", "Trend")
		
		.SetOptimize(50, 200, 25);

		_buyStepUpper = Param(nameof(BuyStepUpper), 10)
		.SetGreaterThanZero()
		.SetDisplay("Buy Upper Offset", "Number of price steps added above the trend line for buy stop", "Buy Orders")
		
		.SetOptimize(5, 30, 5);

		_buyStepLower = Param(nameof(BuyStepLower), 50)
		.SetGreaterThanZero()
		.SetDisplay("Buy Lower Offset", "Number of price steps below the trend line that activates buy orders", "Buy Orders")
		
		.SetOptimize(20, 80, 10);

		_sellStepUpper = Param(nameof(SellStepUpper), 50)
		.SetGreaterThanZero()
		.SetDisplay("Sell Upper Offset", "Number of price steps above the trend line that activates sell orders", "Sell Orders")
		
		.SetOptimize(20, 80, 10);

		_sellStepLower = Param(nameof(SellStepLower), 10)
		.SetGreaterThanZero()
		.SetDisplay("Sell Lower Offset", "Number of price steps below the trend line for sell stop", "Sell Orders")
		
		.SetOptimize(5, 30, 5);

		_buyTakeProfitSteps = Param(nameof(BuyTakeProfitSteps), 50)
		.SetGreaterThanZero()
		.SetDisplay("Buy Take Profit", "Take-profit distance in price steps for long trades", "Risk")
		
		.SetOptimize(20, 100, 10);

		_buyStopLossSteps = Param(nameof(BuyStopLossSteps), 30)
		.SetGreaterThanZero()
		.SetDisplay("Buy Stop Loss", "Stop-loss distance in price steps for long trades", "Risk")
		
		.SetOptimize(10, 60, 10);

		_sellTakeProfitSteps = Param(nameof(SellTakeProfitSteps), 50)
		.SetGreaterThanZero()
		.SetDisplay("Sell Take Profit", "Take-profit distance in price steps for short trades", "Risk")
		
		.SetOptimize(20, 100, 10);

		_sellStopLossSteps = Param(nameof(SellStopLossSteps), 30)
		.SetGreaterThanZero()
		.SetDisplay("Sell Stop Loss", "Stop-loss distance in price steps for short trades", "Risk")
		
		.SetOptimize(10, 60, 10);

		_buyVolume = Param(nameof(BuyVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Buy Volume", "Order volume for buy stop entries", "Buy Orders");

		_sellVolume = Param(nameof(SellVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Sell Volume", "Order volume for sell stop entries", "Sell Orders");
	}

	/// <summary>
	/// Candle aggregation used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the regression trend line indicator.
	/// </summary>
	public int TrendLength
	{
		get => _trendLength.Value;
		set => _trendLength.Value = value;
	}

	/// <summary>
	/// Price steps added above the trend line for buy stop orders.
	/// </summary>
	public int BuyStepUpper
	{
		get => _buyStepUpper.Value;
		set => _buyStepUpper.Value = value;
	}

	/// <summary>
	/// Price steps subtracted below the trend line that activates buy orders.
	/// </summary>
	public int BuyStepLower
	{
		get => _buyStepLower.Value;
		set => _buyStepLower.Value = value;
	}

	/// <summary>
	/// Price steps added above the trend line that activates sell orders.
	/// </summary>
	public int SellStepUpper
	{
		get => _sellStepUpper.Value;
		set => _sellStepUpper.Value = value;
	}

	/// <summary>
	/// Price steps subtracted below the trend line for sell stop orders.
	/// </summary>
	public int SellStepLower
	{
		get => _sellStepLower.Value;
		set => _sellStepLower.Value = value;
	}

	/// <summary>
	/// Take-profit distance (price steps) for long trades.
	/// </summary>
	public int BuyTakeProfitSteps
	{
		get => _buyTakeProfitSteps.Value;
		set => _buyTakeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop-loss distance (price steps) for long trades.
	/// </summary>
	public int BuyStopLossSteps
	{
		get => _buyStopLossSteps.Value;
		set => _buyStopLossSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance (price steps) for short trades.
	/// </summary>
	public int SellTakeProfitSteps
	{
		get => _sellTakeProfitSteps.Value;
		set => _sellTakeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop-loss distance (price steps) for short trades.
	/// </summary>
	public int SellStopLossSteps
	{
		get => _sellStopLossSteps.Value;
		set => _sellStopLossSteps.Value = value;
	}

	/// <summary>
	/// Order volume for buy stop entries.
	/// </summary>
	public decimal BuyVolume
	{
		get => _buyVolume.Value;
		set => _buyVolume.Value = value;
	}

	/// <summary>
	/// Order volume for sell stop entries.
	/// </summary>
	public decimal SellVolume
	{
		get => _sellVolume.Value;
		set => _sellVolume.Value = value;
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

		_entryPrice = 0m;
		_activeStop = null;
		_activeTake = null;
		_activeDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var regression = new LinearRegression { Length = TrendLength };
		var subscription = SubscribeCandles(CandleType);

		subscription
		.BindEx(regression, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!indVal.IsFinal || !indVal.IsFormed)
			return;

		if (indVal is not ILinearRegressionValue lrVal || lrVal.LinearReg is not decimal trendValue)
			return;

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
			priceStep = 1m;

		// Check virtual SL/TP first
		CheckProtection(candle);

		var close = candle.ClosePrice;
		var middle = trendValue;

		var buyLower = middle - BuyStepLower * priceStep;
		var sellUpper = middle + SellStepUpper * priceStep;

		// Buy signal: price is below trend line in the buy zone
		if (close <= middle && close >= buyLower && Position <= 0)
		{
			if (Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				ClearProtection();
			}

			BuyMarket(BuyVolume);
			_entryPrice = close;
			_activeStop = close - BuyStopLossSteps * priceStep;
			_activeTake = close + BuyTakeProfitSteps * priceStep;
			_activeDirection = 1;
		}
		// Sell signal: price is above trend line in the sell zone
		else if (close >= middle && close <= sellUpper && Position >= 0)
		{
			if (Position > 0)
			{
				SellMarket(Position);
				ClearProtection();
			}

			SellMarket(SellVolume);
			_entryPrice = close;
			_activeStop = close + SellStopLossSteps * priceStep;
			_activeTake = close - SellTakeProfitSteps * priceStep;
			_activeDirection = -1;
		}
	}

	private void CheckProtection(ICandleMessage candle)
	{
		if (_activeDirection == 1 && Position > 0 && _activeStop.HasValue && _activeTake.HasValue)
		{
			if (candle.LowPrice <= _activeStop.Value || candle.HighPrice >= _activeTake.Value)
			{
				SellMarket(Position);
				ClearProtection();
			}
		}
		else if (_activeDirection == -1 && Position < 0 && _activeStop.HasValue && _activeTake.HasValue)
		{
			if (candle.HighPrice >= _activeStop.Value || candle.LowPrice <= _activeTake.Value)
			{
				BuyMarket(Math.Abs(Position));
				ClearProtection();
			}
		}
	}

	private void ClearProtection()
	{
		_activeStop = null;
		_activeTake = null;
		_activeDirection = 0;
		_entryPrice = 0m;
	}
}

