using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on smoothed ADX directional index cross.
/// </summary>
public class AdxSmoothedCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _alpha1;
	private readonly StrategyParam<decimal> _alpha2;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;
	private readonly StrategyParam<bool> _allowCloseBuy;
	private readonly StrategyParam<bool> _allowCloseSell;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _rawPlusPrev;
	private decimal? _rawMinusPrev;
	private decimal? _rawAdxPrev;

	private decimal? _firstPlusPrev;
	private decimal? _firstMinusPrev;
	private decimal? _firstAdxPrev;

	private decimal? _smPlusPrev;
	private decimal? _smMinusPrev;
	private decimal? _smAdxPrev;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// First smoothing coefficient.
	/// </summary>
	public decimal Alpha1
	{
		get => _alpha1.Value;
		set => _alpha1.Value = value;
	}

	/// <summary>
	/// Second smoothing coefficient.
	/// </summary>
	public decimal Alpha2
	{
		get => _alpha2.Value;
		set => _alpha2.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Enable long entries.
	/// </summary>
	public bool AllowBuy
	{
		get => _allowBuy.Value;
		set => _allowBuy.Value = value;
	}

	/// <summary>
	/// Enable short entries.
	/// </summary>
	public bool AllowSell
	{
		get => _allowSell.Value;
		set => _allowSell.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool AllowCloseBuy
	{
		get => _allowCloseBuy.Value;
		set => _allowCloseBuy.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool AllowCloseSell
	{
		get => _allowCloseSell.Value;
		set => _allowCloseSell.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AdxSmoothedCrossStrategy"/>.
	/// </summary>
	public AdxSmoothedCrossStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetRange(5, 50)
			.SetDisplay("ADX Period", "Period of ADX calculation", "Indicators")
			.SetCanOptimize(true);

		_alpha1 = Param(nameof(Alpha1), 0.25m)
			.SetRange(0.1m, 0.9m)
			.SetDisplay("Alpha1", "First smoothing factor", "Indicators")
			.SetCanOptimize(true);

		_alpha2 = Param(nameof(Alpha2), 0.33m)
			.SetRange(0.1m, 0.9m)
			.SetDisplay("Alpha2", "Second smoothing factor", "Indicators")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 1000)
			.SetRange(100, 5000)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk Management")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 2000)
			.SetRange(100, 5000)
			.SetDisplay("Take Profit", "Take profit in points", "Risk Management")
			.SetCanOptimize(true);

		_allowBuy = Param(nameof(AllowBuy), true)
			.SetDisplay("Allow Buy", "Enable long entries", "Trading");

		_allowSell = Param(nameof(AllowSell), true)
			.SetDisplay("Allow Sell", "Enable short entries", "Trading");

		_allowCloseBuy = Param(nameof(AllowCloseBuy), true)
			.SetDisplay("Allow Close Long", "Permission to close long positions", "Trading");

		_allowCloseSell = Param(nameof(AllowCloseSell), true)
			.SetDisplay("Allow Close Short", "Permission to close short positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create ADX indicator
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		// Subscribe to candles
		var subscription = SubscribeCandles(CandleType);

		// Bind indicator and start processing
		subscription
			.BindEx(adx, ProcessCandle)
			.Start();

		// Initialize risk management
		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Point),
			stopLoss: new Unit(StopLoss, UnitTypes.Point),
			isStopTrailing: false,
			useMarketOrders: true
		);

		// Visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adx = (AverageDirectionalIndexValue)adxValue;

		var rawPlus = adx.Dx.Plus;
		var rawMinus = adx.Dx.Minus;
		var rawAdx = adx.MovingAverage;

		if (_rawPlusPrev is null)
		{
			_rawPlusPrev = rawPlus;
			_rawMinusPrev = rawMinus;
			_rawAdxPrev = rawAdx;
			return;
		}

		var prevSmPlus = _smPlusPrev;
		var prevSmMinus = _smMinusPrev;

		// First smoothing stage
		var firstPlus = _firstPlusPrev is null
			? rawPlus
			: 2m * rawPlus + (Alpha1 - 2m) * _rawPlusPrev.Value + (1m - Alpha1) * _firstPlusPrev.Value;

		var firstMinus = _firstMinusPrev is null
			? rawMinus
			: 2m * rawMinus + (Alpha1 - 2m) * _rawMinusPrev.Value + (1m - Alpha1) * _firstMinusPrev.Value;

		var firstAdx = _firstAdxPrev is null
			? rawAdx
			: 2m * rawAdx + (Alpha1 - 2m) * _rawAdxPrev.Value + (1m - Alpha1) * _firstAdxPrev.Value;

		// Second smoothing stage
		var smPlus = _smPlusPrev is null
			? firstPlus
			: Alpha2 * firstPlus + (1m - Alpha2) * _smPlusPrev.Value;

		var smMinus = _smMinusPrev is null
			? firstMinus
			: Alpha2 * firstMinus + (1m - Alpha2) * _smMinusPrev.Value;

		var smAdx = _smAdxPrev is null
			? firstAdx
			: Alpha2 * firstAdx + (1m - Alpha2) * _smAdxPrev.Value;

		_rawPlusPrev = rawPlus;
		_rawMinusPrev = rawMinus;
		_rawAdxPrev = rawAdx;

		_firstPlusPrev = firstPlus;
		_firstMinusPrev = firstMinus;
		_firstAdxPrev = firstAdx;

		_smPlusPrev = smPlus;
		_smMinusPrev = smMinus;
		_smAdxPrev = smAdx;

		if (prevSmPlus is null || prevSmMinus is null)
			return;

		var buySignal = prevSmPlus <= prevSmMinus && smPlus > smMinus;
		var sellSignal = prevSmPlus >= prevSmMinus && smPlus < smMinus;

		if (buySignal)
		{
			if (Position < 0)
			{
				if (!AllowCloseSell)
					return;

				if (AllowBuy)
				{
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
				}
				else
				{
					BuyMarket(Math.Abs(Position));
				}
			}
			else if (AllowBuy && Position <= 0)
			{
				BuyMarket(Volume);
			}
		}
		else if (sellSignal)
		{
			if (Position > 0)
			{
				if (!AllowCloseBuy)
					return;

				if (AllowSell)
				{
					var volume = Volume + Position;
					SellMarket(volume);
				}
				else
				{
					SellMarket(Position);
				}
			}
			else if (AllowSell && Position >= 0)
			{
				SellMarket(Volume);
			}
		}
	}
}
