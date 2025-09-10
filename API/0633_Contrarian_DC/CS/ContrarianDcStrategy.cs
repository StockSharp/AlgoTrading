using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Contrarian Donchian Channel strategy with stop-loss pause and risk/reward exits.
/// </summary>
public class ContrarianDcStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _pauseCandles;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	private long _barIndex;
	private long _longSlBar;
	private long _shortSlBar;
	private int _lastDirection;

	/// <summary>
	/// Donchian Channel period.
	/// </summary>
	public int DonchianPeriod { get => _donchianPeriod.Value; set => _donchianPeriod.Value = value; }

	/// <summary>
	/// Risk to reward ratio.
	/// </summary>
	public decimal RiskRewardRatio { get => _riskRewardRatio.Value; set => _riskRewardRatio.Value = value; }

	/// <summary>
	/// Stop-loss percent.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Pause candles after stop-loss.
	/// </summary>
	public int PauseCandles { get => _pauseCandles.Value; set => _pauseCandles.Value = value; }

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ContrarianDcStrategy"/> class.
	/// </summary>
	public ContrarianDcStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Period for Donchian Channel", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_riskRewardRatio = Param(nameof(RiskRewardRatio), 1.7m)
			.SetGreaterThanZero()
			.SetDisplay("Risk/Reward", "Risk to reward ratio", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Stop-loss %", "Stop-loss percentage", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_pauseCandles = Param(nameof(PauseCandles), 3)
			.SetGreaterThanZero()
			.SetDisplay("Pause Candles", "Bars to wait after stop-loss", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_barIndex = 0;
		_longSlBar = 0;
		_shortSlBar = 0;
		_lastDirection = 0;
		_highest = null;
		_lowest = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = DonchianPeriod };
		_lowest = new Lowest { Length = DonchianPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_barIndex++;
			return;
		}

		var high = candle.HighPrice;
		var low = candle.LowPrice;

		if (Position == 0)
		{
			var longCondition = low <= lower && (_barIndex - _longSlBar > PauseCandles || _lastDirection != 1);
			var shortCondition = high >= upper && (_barIndex - _shortSlBar > PauseCandles || _lastDirection != -1);

			if (longCondition)
			{
				BuyMarket();
			}
			else if (shortCondition)
			{
				SellMarket();
			}
		}
		else if (Position > 0)
		{
			var entry = PositionPrice;
			var stop = entry * (1m - StopLossPercent / 100m);
			var take = entry * (1m + StopLossPercent * RiskRewardRatio / 100m);

			if (low <= stop)
			{
				SellMarket(Position);
				_longSlBar = _barIndex;
				_lastDirection = 1;
			}
			else if (high >= take)
			{
				SellMarket(Position);
			}
			else if (high >= upper)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			var entry = PositionPrice;
			var stop = entry * (1m + StopLossPercent / 100m);
			var take = entry * (1m - StopLossPercent * RiskRewardRatio / 100m);

			if (high >= stop)
			{
				BuyMarket(Math.Abs(Position));
				_shortSlBar = _barIndex;
				_lastDirection = -1;
			}
			else if (low <= take)
			{
				BuyMarket(Math.Abs(Position));
			}
			else if (low <= lower)
			{
				BuyMarket(Math.Abs(Position));
			}
		}

		_barIndex++;
	}
}
