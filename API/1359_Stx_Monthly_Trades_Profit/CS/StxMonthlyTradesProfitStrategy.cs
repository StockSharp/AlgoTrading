using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy demonstrating monthly profit tracking with scheduled trades.
/// </summary>
public class StxMonthlyTradesProfitStrategy : Strategy
{
	private readonly StrategyParam<string> _testCase;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<ProfitMode> _profitMode;
	private readonly StrategyParam<int> _leverage;
	private readonly StrategyParam<bool> _showYearly;

	private decimal _entryPrice;
	private bool _firstExitDone;
	private decimal _firstTarget;
	private decimal _secondTarget;
	private decimal _stopPrice;

	private decimal _monthlyProfit;
	private decimal _startMonthPnL;
	private decimal _globalProfit;
	private int _prevMonth = -1;
	private int _startYear;

	private readonly Dictionary<int, decimal[]> _monthlyPnL = new();

	/// <summary>
	/// Profit calculation modes.
	/// </summary>
	public enum ProfitMode
	{
		/// <summary>Disable profit calculation.</summary>
		Disabled,
		/// <summary>Calculate profit based on realized PnL.</summary>
		MonthlyProfit,
		/// <summary>Calculate profit based on equity changes.</summary>
		MonthlyEquity
	}

	/// <summary>Selected test case.</summary>
	public string TestCase
	{
		get => _testCase.Value;
		set => _testCase.Value = value;
	}

	/// <summary>Candle type used by the strategy.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Profit calculation mode.</summary>
	public ProfitMode Mode
	{
		get => _profitMode.Value;
		set => _profitMode.Value = value;
	}

	/// <summary>Leverage multiplier for monthly profit.</summary>
	public int Leverage
	{
		get => _leverage.Value;
		set => _leverage.Value = value;
	}

	/// <summary>Show yearly profit in logs.</summary>
	public bool ShowYearlyProfit
	{
		get => _showYearly.Value;
		set => _showYearly.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="StxMonthlyTradesProfitStrategy"/>.
	/// </summary>
	public StxMonthlyTradesProfitStrategy()
	{
		_testCase = Param(nameof(TestCase), "single_entry_exit")
			.SetDisplay("Test Case", "Trading scenario", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_profitMode = Param(nameof(Mode), ProfitMode.MonthlyProfit)
			.SetDisplay("Profit Mode", "Monthly calculation mode", "Trades Profit Table");

		_leverage = Param(nameof(Leverage), 1)
			.SetGreaterThanZero()
			.SetDisplay("Leverage", "Fixed leverage value", "Trades Profit Table");

		_showYearly = Param(nameof(ShowYearlyProfit), true)
			.SetDisplay("Show Yearly Profit", "Display yearly profit in logs", "Trades Profit Table");
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

		StartProtection();

		_startYear = time.Year;
		_prevMonth = time.Month;
		_monthlyPnL[_startYear] = new decimal[12];

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		var longCondition = time.Day == 1 && time.Hour == 10 && time.Minute == 0;
		var shortCondition = time.Day == 10 && time.Hour == 10 && time.Minute == 0;

		if (TestCase == "single_entry_exit")
		{
			if (longCondition && Position == 0 && IsFormedAndOnlineAndAllowTrading())
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice * (1m - 0.005m);
				_firstTarget = _entryPrice * (1m + 0.01m);
			}
			else if (shortCondition && Position == 0 && IsFormedAndOnlineAndAllowTrading())
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice * (1m + 0.005m);
				_firstTarget = _entryPrice * (1m - 0.01m);
			}

			if (Position > 0)
			{
				if (candle.ClosePrice >= _firstTarget || candle.ClosePrice <= _stopPrice)
					SellMarket(Position);
			}
			else if (Position < 0)
			{
				if (candle.ClosePrice <= _firstTarget || candle.ClosePrice >= _stopPrice)
					BuyMarket(-Position);
			}
		}
		else if (TestCase == "single_entry_multiple_exit")
		{
			if (longCondition && Position == 0 && IsFormedAndOnlineAndAllowTrading())
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice * (1m - 0.005m);
				_firstTarget = _entryPrice * (1m + 0.01m);
				_secondTarget = _entryPrice * (1m + 0.02m);
				_firstExitDone = false;
			}
			else if (shortCondition && Position == 0 && IsFormedAndOnlineAndAllowTrading())
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice * (1m + 0.005m);
				_firstTarget = _entryPrice * (1m - 0.01m);
				_secondTarget = _entryPrice * (1m - 0.02m);
				_firstExitDone = false;
			}

			if (Position > 0)
			{
				if (!_firstExitDone && candle.ClosePrice >= _firstTarget)
				{
					SellMarket(Position / 2m);
					_firstExitDone = true;
				}

				if (candle.ClosePrice >= _secondTarget || candle.ClosePrice <= _stopPrice)
					SellMarket(Position);
			}
			else if (Position < 0)
			{
				var absPos = -Position;

				if (!_firstExitDone && candle.ClosePrice <= _firstTarget)
				{
					BuyMarket(absPos / 2m);
					_firstExitDone = true;
				}

				if (candle.ClosePrice <= _secondTarget || candle.ClosePrice >= _stopPrice)
					BuyMarket(-Position);
			}
		}
		else // single_entry_switch_position
		{
			if (longCondition)
			{
				if (Position != 0)
					ClosePosition();
				if (IsFormedAndOnlineAndAllowTrading())
					BuyMarket();
			}
			else if (shortCondition)
			{
				if (Position != 0)
					ClosePosition();
				if (IsFormedAndOnlineAndAllowTrading())
					SellMarket();
			}
		}

		UpdateProfit(candle);
	}

	private void UpdateProfit(ICandleMessage candle)
	{
		if (Mode == ProfitMode.Disabled)
			return;

		var month = candle.OpenTime.Month;
		var year = candle.OpenTime.Year;

		if (!_monthlyPnL.TryGetValue(year, out var arr))
		{
			arr = new decimal[12];
			_monthlyPnL[year] = arr;
		}

		if (Mode == ProfitMode.MonthlyEquity)
		{
			var equity = 1m + PnL;
			_monthlyProfit = equity / (1m + _startMonthPnL) - 1m;
			_globalProfit = equity - 1m;
		}
		else
		{
			var realized = PnL;
			_monthlyProfit = (realized - _startMonthPnL) * Leverage;
			_globalProfit = realized;
		}

		arr[month - 1] = _monthlyProfit;

		if (month != _prevMonth)
		{
			_startMonthPnL = PnL;
			_prevMonth = month;

			if (ShowYearlyProfit && month == 1 && year > _startYear)
			{
				var prevYearArr = _monthlyPnL[year - 1];
				var yearly = 0m;
				foreach (var m in prevYearArr)
					yearly += m;
				LogInfo($"Yearly profit for {year - 1}: {yearly:P2}");
			}
		}
	}
}
