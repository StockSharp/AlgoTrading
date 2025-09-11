using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Falcon Liquidity Grab Strategy - trades liquidity grabs during major sessions.
/// </summary>
public class FalconLiquidityGrabStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _swingPeriod;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _sma = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private static readonly TimeSpan _londonStart = new(7, 0, 0);
	private static readonly TimeSpan _londonEnd = new(16, 0, 0);
	private static readonly TimeSpan _newYorkStart = new(12, 0, 0);
	private static readonly TimeSpan _newYorkEnd = new(21, 0, 0);
	private static readonly TimeSpan _sydneyStart = new(22, 0, 0);
	private static readonly TimeSpan _sydneyEnd = new(6, 0, 0);
	private static readonly TimeSpan _tokyoStart = new(0, 0, 0);
	private static readonly TimeSpan _tokyoEnd = new(9, 0, 0);

	/// <summary>
	/// Initializes a new instance of the <see cref="FalconLiquidityGrabStrategy"/>.
	/// </summary>
	public FalconLiquidityGrabStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetDisplay("MA Period", "Moving average period", "General");

		_swingPeriod = Param(nameof(SwingPeriod), 5)
			.SetDisplay("Swing Period", "Lookback for swing high/low", "General");

		_stopLossPoints = Param(nameof(StopLossPoints), 20)
			.SetDisplay("Stop Loss Points", "Stop loss distance in ticks", "Risk Management");

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 2m)
			.SetDisplay("Take Profit Multiplier", "Multiplier for take profit distance", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	/// <summary>
	/// Lookback for swing calculation.
	/// </summary>
	public int SwingPeriod { get => _swingPeriod.Value; set => _swingPeriod.Value = value; }

	/// <summary>
	/// Stop loss distance in ticks.
	/// </summary>
	public int StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Take profit distance multiplier.
	/// </summary>
	public decimal TakeProfitMultiplier { get => _takeProfitMultiplier.Value; set => _takeProfitMultiplier.Value = value; }

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new SMA { Length = MaPeriod };
		_highest = new Highest { Length = SwingPeriod };
		_lowest = new Lowest { Length = SwingPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, _highest, _lowest, ProcessCandle)
			.Start();

		var step = Security.PriceStep ?? 1m;
		StartProtection(
		stopLoss: new Unit(StopLossPoints * step, UnitTypes.Absolute),
		takeProfit: new Unit(StopLossPoints * TakeProfitMultiplier * step, UnitTypes.Absolute),
		isStopTrailing: false,
		useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawIndicator(area, _sma);
		DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal swingHigh, decimal swingLow)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_sma.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
		return;

		var t = candle.OpenTime.TimeOfDay;
		if (!InSession(t))
		return;

		var isUptrend = candle.ClosePrice > maValue;
		var isDowntrend = candle.ClosePrice < maValue;

		if (candle.LowPrice < swingLow && isUptrend && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		}
		else if (candle.HighPrice > swingHigh && isDowntrend && Position >= 0)
		{
		SellMarket(Volume + Math.Abs(Position));
		}
	}

	private static bool InSession(TimeSpan time)
	{
		return InRange(_londonStart, _londonEnd, time)
		|| InRange(_newYorkStart, _newYorkEnd, time)
		|| InRange(_sydneyStart, _sydneyEnd, time)
		|| InRange(_tokyoStart, _tokyoEnd, time);
	}

	private static bool InRange(TimeSpan start, TimeSpan end, TimeSpan time)
		=> start <= end ? time >= start && time <= end : time >= start || time <= end;
}
