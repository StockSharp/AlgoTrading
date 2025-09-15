using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class TurtleTraderSarStrategy : Strategy
{
	// Strategy parameters
	private readonly StrategyParam<int> _exitPeriod;
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<decimal> _riskFraction;
	private readonly StrategyParam<decimal> _maxUnits;
	private readonly StrategyParam<decimal> _addInterval;
	private readonly StrategyParam<decimal> _stopAtr;
	private readonly StrategyParam<decimal> _takeAtr;
	private readonly StrategyParam<bool> _useSar;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<DataType> _candleType;

	// Indicators
	private AverageTrueRange _atr;
	private Highest _exitHigh;
	private Lowest _exitLow;
	private Highest _shortHigh;
	private Lowest _shortLow;
	private Highest _longHigh;
	private Lowest _longLow;
	private ParabolicSar _sar;

	// Internal state
	private decimal _lastEntryPrice;
	private decimal _stopPrice;
	private decimal? _takePrice;

	public int ExitPeriod { get => _exitPeriod.Value; set => _exitPeriod.Value = value; }
	public int ShortPeriod { get => _shortPeriod.Value; set => _shortPeriod.Value = value; }
	public int LongPeriod { get => _longPeriod.Value; set => _longPeriod.Value = value; }
	public decimal RiskFraction { get => _riskFraction.Value; set => _riskFraction.Value = value; }
	public decimal MaxUnits { get => _maxUnits.Value; set => _maxUnits.Value = value; }
	public decimal AddInterval { get => _addInterval.Value; set => _addInterval.Value = value; }
	public decimal StopAtr { get => _stopAtr.Value; set => _stopAtr.Value = value; }
	public decimal TakeAtr { get => _takeAtr.Value; set => _takeAtr.Value = value; }
	public bool UseSar { get => _useSar.Value; set => _useSar.Value = value; }
	public decimal SarStep { get => _sarStep.Value; set => _sarStep.Value = value; }
	public decimal SarMax { get => _sarMax.Value; set => _sarMax.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TurtleTraderSarStrategy()
	{
		// Initialize parameters
		_exitPeriod = Param(nameof(ExitPeriod), 10)
			.SetDisplay("Exit Period", "Donchian exit period", "General")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_shortPeriod = Param(nameof(ShortPeriod), 20)
			.SetDisplay("Short Period", "Short breakout period", "General")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_longPeriod = Param(nameof(LongPeriod), 55)
			.SetDisplay("Long Period", "Long breakout period", "General")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_riskFraction = Param(nameof(RiskFraction), 0.01m)
			.SetDisplay("Risk Fraction", "Account fraction risked per trade", "Money")
			.SetCanOptimize(true);

		_maxUnits = Param(nameof(MaxUnits), 4m)
			.SetDisplay("Max Units", "Maximum number of units", "Money")
			.SetCanOptimize(true);

		_addInterval = Param(nameof(AddInterval), 1m)
			.SetDisplay("Add Interval", "ATR move to add units", "Money")
			.SetCanOptimize(true);

		_stopAtr = Param(nameof(StopAtr), 1m)
			.SetDisplay("Stop ATR", "ATR multiplier for stop", "Money")
			.SetCanOptimize(true);

		_takeAtr = Param(nameof(TakeAtr), 1m)
			.SetDisplay("Take ATR", "ATR multiplier for take profit", "Money")
			.SetCanOptimize(true);

		_useSar = Param(nameof(UseSar), false)
			.SetDisplay("Use SAR", "Enable Parabolic SAR trailing", "Money");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetDisplay("SAR Step", "Parabolic SAR step", "Money")
			.SetCanOptimize(true);

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetDisplay("SAR Max", "Parabolic SAR maximum", "Money")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Enable built-in position protection
		StartProtection();

		// Create indicators
		_atr = new AverageTrueRange { Length = 20 };
		_exitHigh = new Highest { Length = ExitPeriod };
		_exitLow = new Lowest { Length = ExitPeriod };
		_shortHigh = new Highest { Length = ShortPeriod };
		_shortLow = new Lowest { Length = ShortPeriod };
		_longHigh = new Highest { Length = LongPeriod };
		_longLow = new Lowest { Length = LongPeriod };
		_sar = new ParabolicSar { Step = SarStep, Max = SarMax };

		// Subscribe to candles and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, _exitHigh, _exitLow, _shortHigh, _shortLow, _longHigh, _longLow, _sar, ProcessCandle)
			.Start();

		// Create chart for visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _exitHigh);
			DrawIndicator(area, _exitLow);
			DrawIndicator(area, _shortHigh);
			DrawIndicator(area, _shortLow);
			DrawIndicator(area, _longHigh);
			DrawIndicator(area, _longLow);
			if (UseSar)
				DrawIndicator(area, _sar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal exitHigh, decimal exitLow,
		decimal shortHigh, decimal shortLow, decimal longHigh, decimal longLow, decimal sar)
	{
		// Ignore unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Skip if trading not allowed or data incomplete
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (atr <= 0)
			return;

		// Calculate account risk and unit size
		var account = Portfolio.CurrentValue ?? 0m;
		if (account <= 0)
			return;

		var unit = Math.Min(MaxUnits, RiskFraction * account / atr);
		if (unit <= 0)
			return;

		var price = candle.ClosePrice;

		// Manage existing position
		if (Position != 0)
		{
			// Add to position on favorable move
			if ((price - _lastEntryPrice) * Math.Sign(Position) >= AddInterval * atr &&
				Math.Abs(Position) + unit <= MaxUnits)
			{
				if (Position > 0)
					BuyMarket(unit);
				else
					SellMarket(unit);

				_lastEntryPrice = price;
			}

			if (Position > 0)
			{
				// Check exit conditions for long position
				var exit = Math.Min(exitLow, _stopPrice);
				if (price <= exit || (UseSar && price <= sar))
				{
					SellMarket(Position);
					return;
				}

				if (_takePrice != null && price >= _takePrice)
				{
					SellMarket(Position);
					return;
				}
			}
			else
			{
				// Check exit conditions for short position
				var exit = Math.Max(exitHigh, _stopPrice);
				if (price >= exit || (UseSar && price >= sar))
				{
					BuyMarket(-Position);
					return;
				}

				if (_takePrice != null && price <= _takePrice)
				{
					BuyMarket(-Position);
					return;
				}
			}

			return;
		}

		// No position, check for breakout entry
		var breakout = price > shortHigh ? 1 : price < shortLow ? -1 : 0;
		if (breakout == 0)
			return;

		_lastEntryPrice = price;

		// Enter long or short with ATR based stops
		if (breakout > 0)
		{
			_stopPrice = price - StopAtr * atr;
			_takePrice = TakeAtr > 0 ? price + TakeAtr * atr : null;
			BuyMarket(unit);
		}
		else
		{
			_stopPrice = price + StopAtr * atr;
			_takePrice = TakeAtr > 0 ? price - TakeAtr * atr : null;
			SellMarket(unit);
		}
	}
}
