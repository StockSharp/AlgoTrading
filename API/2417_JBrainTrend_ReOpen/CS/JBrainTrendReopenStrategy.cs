using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Stochastic oscillator with position re-opening capability.
/// Derived from the MQL5 example "JBrainTrend1Stop_ReOpen".
/// Opens a position when the market enters oversold/overbought zones and
/// re-enters in the same direction after price moves by a defined step.
/// </summary>
public class JBrainTrendReopenStrategy : Strategy
{
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _priceStep;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<bool> _buyEnabled;
	private readonly StrategyParam<bool> _sellEnabled;

	private decimal _lastEntryPrice;
	private int _entriesCount;
	private bool _isLong;

	/// <summary>
	/// Main period for the Stochastic oscillator.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for the %K line.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for the %D line.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Candle type and timeframe used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop loss in absolute price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in absolute price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Price movement required to add to the position.
	/// </summary>
	public decimal PriceStep
	{
		get => _priceStep.Value;
		set => _priceStep.Value = value;
	}

	/// <summary>
	/// Maximum number of entries in one direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool BuyEnabled
	{
		get => _buyEnabled.Value;
		set => _buyEnabled.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool SellEnabled
	{
		get => _sellEnabled.Value;
		set => _sellEnabled.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public JBrainTrendReopenStrategy()
	{
		_stochPeriod = Param(nameof(StochPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Main period for Stochastic oscillator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_kPeriod = Param(nameof(KPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "Smoothing for %K line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Smoothing for %D line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Timeframe", "Timeframe for calculations", "General");

		_stopLoss = Param(nameof(StopLoss), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50m, 500m, 50m);

		_takeProfit = Param(nameof(TakeProfit), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price units", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 1000m, 100m);

		_priceStep = Param(nameof(PriceStep), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Re-entry Step", "Price move to add position", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 1000m, 100m);

		_maxPositions = Param(nameof(MaxPositions), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum entries in one direction", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_buyEnabled = Param(nameof(BuyEnabled), true)
			.SetDisplay("Allow Long", "Enable long trades", "General");

		_sellEnabled = Param(nameof(SellEnabled), true)
			.SetDisplay("Allow Short", "Enable short trades", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var stochastic = new StochasticOscillator
		{
			Length = StochPeriod,
			K = { Length = KPeriod },
			D = { Length = DPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute),
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		var k = stoch.K;
		var price = candle.ClosePrice;

		if (Position == 0)
		{
			_entriesCount = 0;
		}

		if (k < 20 && Position <= 0 && BuyEnabled)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_isLong = true;
			_lastEntryPrice = price;
			_entriesCount = 1;
			return;
		}

		if (k > 80 && Position >= 0 && SellEnabled)
		{
			SellMarket(Volume + Math.Abs(Position));
			_isLong = false;
			_lastEntryPrice = price;
			_entriesCount = 1;
			return;
		}

		if (Position > 0 && k > 80)
		{
			SellMarket(Position);
			return;
		}

		if (Position < 0 && k < 20)
		{
			BuyMarket(Math.Abs(Position));
			return;
		}

		if (_entriesCount > 0 && _entriesCount < MaxPositions)
		{
			if (_isLong && Position > 0 && price - _lastEntryPrice >= PriceStep)
			{
				BuyMarket(Volume);
				_lastEntryPrice = price;
				_entriesCount++;
			}
			else if (!_isLong && Position < 0 && _lastEntryPrice - price >= PriceStep)
			{
				SellMarket(Volume);
				_lastEntryPrice = price;
				_entriesCount++;
			}
		}
	}
}
