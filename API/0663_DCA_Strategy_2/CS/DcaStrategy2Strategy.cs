using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dollar cost averaging strategy using Heikin Ashi and RSI.
/// </summary>
public class DcaStrategy2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _startYear;
	private readonly StrategyParam<int> _endYear;
	private readonly StrategyParam<int> _exitPercent;
	private readonly StrategyParam<int> _exitRsi;

	private RelativeStrengthIndex _rsi;
	private DateTimeOffset _start;
	private DateTimeOffset _finish;
	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private decimal _prevRsi;
	private int _rsiExit;

	/// <summary>
	/// Initialize DCA Strategy 2.
	/// </summary>
	public DcaStrategy2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_startYear = Param(nameof(StartYear), 2004)
			.SetDisplay("Start Year", "The year at which the strategy to start backtesting", "Period");

		_endYear = Param(nameof(EndYear), 2030)
			.SetDisplay("End Year", "The year at which the strategy to stop backtesting", "Period");

		_exitPercent = Param(nameof(ExitPercent), 100)
			.SetDisplay("Exit Percent", "How much capital should be sold at exit", "Exit");

		_exitRsi = Param(nameof(ExitRsi), 85)
			.SetDisplay("Exit RSI", "The RSI value to exit at. Set to 100 for auto detection", "Exit");
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
	/// Start year.
	/// </summary>
	public int StartYear
	{
		get => _startYear.Value;
		set => _startYear.Value = value;
	}

	/// <summary>
	/// End year.
	/// </summary>
	public int EndYear
	{
		get => _endYear.Value;
		set => _endYear.Value = value;
	}

	/// <summary>
	/// Exit percent.
	/// </summary>
	public int ExitPercent
	{
		get => _exitPercent.Value;
		set => _exitPercent.Value = value;
	}

	/// <summary>
	/// Exit RSI level.
	/// </summary>
	public int ExitRsi
	{
		get => _exitRsi.Value;
		set => _exitRsi.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHaOpen = _prevHaClose = 0m;
		_prevRsi = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_start = new DateTimeOffset(StartYear, 1, 1, 0, 0, 0, time.Offset);
		_finish = new DateTimeOffset(EndYear, 1, 1, 0, 0, 0, time.Offset);

		var isBitcoin = Security.Code.IndexOf("BTC", StringComparison.OrdinalIgnoreCase) >= 0;
		_rsiExit = ExitRsi < 100 ? ExitRsi : isBitcoin ? 92 : 84;

		_rsi = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = _prevHaOpen == 0m && _prevHaClose == 0m
			? (candle.OpenPrice + candle.ClosePrice) / 2m
			: (_prevHaOpen + _prevHaClose) / 2m;

		var greenCandle = haClose > haOpen;
		var redPrev = _prevHaClose < _prevHaOpen;
		var inWindow = candle.OpenTime >= _start && candle.OpenTime <= _finish;

		if (_rsi.IsFormed && IsFormedAndOnlineAndAllowTrading())
		{
			if (greenCandle && redPrev && inWindow)
				BuyMarket(Volume);

			var crossUnder = _prevRsi >= _rsiExit && rsiValue < _rsiExit;
			var timeExit = candle.OpenTime >= _finish;

			if (Position > 0 && (crossUnder || timeExit))
			{
				var qty = Position * ExitPercent / 100m;
				SellMarket(qty);
			}
		}

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
		_prevRsi = rsiValue;
	}
}
