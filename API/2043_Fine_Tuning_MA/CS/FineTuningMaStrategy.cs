using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using System;
using System.Collections.Generic;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fine Tuning MA strategy converted from the MQL version.
/// Opens positions when the moving average changes direction.
/// </summary>
public class FineTuningMaStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prev1;
	private decimal? _prev2;
	private SimpleMovingAverage _ma;

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool AllowBuyOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool AllowSellOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool AllowBuyClose
	{
		get => _allowBuyClose.Value;
		set => _allowBuyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool AllowSellClose
	{
		get => _allowSellClose.Value;
		set => _allowSellClose.Value = value;
	}

	/// <summary>
	/// Take profit level in percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss level in percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="FineTuningMaStrategy"/>.
	/// </summary>
	public FineTuningMaStrategy()
	{
		_maLength = Param(nameof(MaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Length of the moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
			.SetDisplay("Allow Buy Open", "Enable long entries", "Behavior");

		_allowSellOpen = Param(nameof(AllowSellOpen), true)
			.SetDisplay("Allow Sell Open", "Enable short entries", "Behavior");

		_allowBuyClose = Param(nameof(AllowBuyClose), true)
			.SetDisplay("Allow Buy Close", "Allow closing of long positions", "Behavior");

		_allowSellClose = Param(nameof(AllowSellClose), true)
			.SetDisplay("Allow Sell Close", "Allow closing of short positions", "Behavior");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1m)
			.SetDisplay("Take Profit, %", "Take profit level in percent", "Protection");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetDisplay("Stop Loss, %", "Stop loss level in percent", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "Parameters");
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

		_ma = null;
		_prev1 = null;
		_prev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ma, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prev1 is null || _prev2 is null)
		{
			_prev2 = _prev1;
			_prev1 = ma;
			return;
		}

		var wasRising = _prev1 < _prev2;
		var wasFalling = _prev1 > _prev2;

		if (AllowSellClose && wasRising && Position < 0)
			BuyMarket(Math.Abs(Position));

		if (AllowBuyClose && wasFalling && Position > 0)
			SellMarket(Math.Abs(Position));

		if (AllowBuyOpen && wasRising && ma > _prev1 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (AllowSellOpen && wasFalling && ma < _prev1 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prev2 = _prev1;
		_prev1 = ma;
	}
}
