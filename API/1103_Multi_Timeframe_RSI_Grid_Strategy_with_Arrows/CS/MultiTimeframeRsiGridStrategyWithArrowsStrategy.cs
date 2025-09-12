using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe RSI grid strategy with ATR based spacing and daily profit target.
/// </summary>
public class MultiTimeframeRsiGridStrategyWithArrowsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _oversold;
	private readonly StrategyParam<int> _overbought;
	private readonly StrategyParam<DataType> _higherTimeframe1;
	private readonly StrategyParam<DataType> _higherTimeframe2;
	private readonly StrategyParam<decimal> _gridFactor;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<int> _maxGridLevels;
	private readonly StrategyParam<decimal> _dailyTargetPercent;
	private readonly StrategyParam<int> _atrLength;

	private decimal _higherTf1Rsi;
	private decimal _higherTf2Rsi;
	private int _gridLevel;
	private decimal? _lastEntryPrice;
	private decimal _dailyProfitTarget;
	private bool _targetReached;
	private DateTime _currentDay;
	private decimal _gridSpace;

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Oversold level.
	/// </summary>
	public int Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public int Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// First higher timeframe.
	/// </summary>
	public DataType HigherTimeframe1
	{
		get => _higherTimeframe1.Value;
		set => _higherTimeframe1.Value = value;
	}

	/// <summary>
	/// Second higher timeframe.
	/// </summary>
	public DataType HigherTimeframe2
	{
		get => _higherTimeframe2.Value;
		set => _higherTimeframe2.Value = value;
	}

	/// <summary>
	/// ATR multiplication factor for grid spacing.
	/// </summary>
	public decimal GridFactor
	{
		get => _gridFactor.Value;
		set => _gridFactor.Value = value;
	}

	/// <summary>
	/// Lot multiplication factor for grid orders.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum number of grid levels.
	/// </summary>
	public int MaxGridLevels
	{
		get => _maxGridLevels.Value;
		set => _maxGridLevels.Value = value;
	}

	/// <summary>
	/// Daily profit target percent.
	/// </summary>
	public decimal DailyTargetPercent
	{
		get => _dailyTargetPercent.Value;
		set => _dailyTargetPercent.Value = value;
	}

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MultiTimeframeRsiGridStrategyWithArrowsStrategy"/>.
	/// </summary>
	public MultiTimeframeRsiGridStrategyWithArrowsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Number of periods", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_oversold = Param(nameof(Oversold), 30)
			.SetDisplay("Oversold", "Oversold level", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_overbought = Param(nameof(Overbought), 70)
			.SetDisplay("Overbought", "Overbought level", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50, 90, 5);

		_higherTimeframe1 = Param(nameof(HigherTimeframe1), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Higher TF1", "First higher timeframe", "General");

		_higherTimeframe2 = Param(nameof(HigherTimeframe2), TimeSpan.FromMinutes(240).TimeFrame())
			.SetDisplay("Higher TF2", "Second higher timeframe", "General");

		_gridFactor = Param(nameof(GridFactor), 1.2m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Factor", "ATR multiplier", "Grid")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.1m);

		_lotMultiplier = Param(nameof(LotMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Multiplier", "Order size multiplier", "Grid")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.1m);

		_maxGridLevels = Param(nameof(MaxGridLevels), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Grid Levels", "Maximum grid additions", "Grid");

		_dailyTargetPercent = Param(nameof(DailyTargetPercent), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Daily Target %", "Daily profit target", "Risk");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HigherTimeframe1), (Security, HigherTimeframe2)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_higherTf1Rsi = 0m;
		_higherTf2Rsi = 0m;
		_gridLevel = 0;
		_lastEntryPrice = null;
		_dailyProfitTarget = 0m;
		_targetReached = false;
		_currentDay = default;
		_gridSpace = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var htf1Rsi = new RelativeStrengthIndex { Length = RsiLength };
		var htf2Rsi = new RelativeStrengthIndex { Length = RsiLength };

		var mainSub = SubscribeCandles(CandleType);
		mainSub
			.Bind(rsi, atr, ProcessCandle)
			.Start();

		SubscribeCandles(HigherTimeframe1)
			.Bind(htf1Rsi, ProcessHigherTf1)
			.Start();

		SubscribeCandles(HigherTimeframe2)
			.Bind(htf2Rsi, ProcessHigherTf2)
			.Start();

		StartProtection();
	}

	private void ProcessHigherTf1(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_higherTf1Rsi = rsi;
	}

	private void ProcessHigherTf2(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_higherTf2Rsi = rsi;
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		_gridSpace = atr * GridFactor;

		var day = candle.Time.Date;
		if (day != _currentDay)
		{
			var equity = Portfolio?.CurrentValue ?? 0m;
			_dailyProfitTarget = equity * (DailyTargetPercent / 100m);
			_targetReached = false;
			_gridLevel = 0;
			_lastEntryPrice = null;
			_currentDay = day;
		}

		var buyCondition = rsi < Oversold && _higherTf1Rsi < Oversold && _higherTf2Rsi < Oversold;
		var sellCondition = rsi > Overbought && _higherTf1Rsi > Overbought && _higherTf2Rsi > Overbought;

		var reverseLongToShort = sellCondition && Position > 0;
		var reverseShortToLong = buyCondition && Position < 0;

		if (reverseLongToShort || reverseShortToLong)
		{
			CloseAll();
			_gridLevel = 0;
			_lastEntryPrice = null;
		}

		if (Position == 0)
		{
			_gridLevel = 0;
			_lastEntryPrice = null;
		}

		var equityNow = Portfolio?.CurrentValue ?? 0m;
		var baseSize = close != 0m ? equityNow * 0.01m / close : 0m;

		if (Position > 0 && !reverseLongToShort)
		{
			if (_lastEntryPrice is decimal le && close < le - _gridSpace && _gridLevel < MaxGridLevels && !_targetReached)
			{
				var qty = baseSize * (decimal)Math.Pow((double)LotMultiplier, _gridLevel);
				BuyMarket(qty);
				_gridLevel++;
				_lastEntryPrice = close;
			}
		}
		else if (Position < 0 && !reverseShortToLong)
		{
			if (_lastEntryPrice is decimal le && close > le + _gridSpace && _gridLevel < MaxGridLevels && !_targetReached)
			{
				var qty = baseSize * (decimal)Math.Pow((double)LotMultiplier, _gridLevel);
				SellMarket(qty);
				_gridLevel++;
				_lastEntryPrice = close;
			}
		}

		if (buyCondition && Position == 0 && !_targetReached)
		{
			BuyMarket(baseSize);
			_gridLevel = 1;
			_lastEntryPrice = close;
		}
		else if (sellCondition && Position == 0 && !_targetReached)
		{
			SellMarket(baseSize);
			_gridLevel = 1;
			_lastEntryPrice = close;
		}

		var unrealized = Position * (close - PositionPrice);
		var currentProfit = PnL + unrealized;

		if (currentProfit >= _dailyProfitTarget && !_targetReached)
		{
			CloseAll();
			_targetReached = true;
		}

		if (unrealized < -(0.02m * equityNow))
		{
			CloseAll();
			_gridLevel = 0;
			_lastEntryPrice = null;
		}
	}
}
