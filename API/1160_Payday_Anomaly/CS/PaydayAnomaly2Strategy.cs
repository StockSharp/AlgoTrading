using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Payday anomaly strategy.
/// Opens long positions on selected days of the month and closes on other days.
/// </summary>
public class PaydayAnomaly2Strategy : Strategy
{
	private readonly StrategyParam<bool> _trade1st;
	private readonly StrategyParam<bool> _trade2nd;
	private readonly StrategyParam<bool> _trade16th;
	private readonly StrategyParam<bool> _trade31st;
	private readonly StrategyParam<DataType> _candleType;

	private bool _tradeOpened;

	/// <summary>
	/// Trade on the 1st day of the month.
	/// </summary>
	public bool Trade1st
	{
		get => _trade1st.Value;
		set => _trade1st.Value = value;
	}

	/// <summary>
	/// Trade on the 2nd day of the month.
	/// </summary>
	public bool Trade2nd
	{
		get => _trade2nd.Value;
		set => _trade2nd.Value = value;
	}

	/// <summary>
	/// Trade on the 16th day of the month.
	/// </summary>
	public bool Trade16th
	{
		get => _trade16th.Value;
		set => _trade16th.Value = value;
	}

	/// <summary>
	/// Trade on the 31st day of the month.
	/// </summary>
	public bool Trade31st
	{
		get => _trade31st.Value;
		set => _trade31st.Value = value;
	}

	/// <summary>
	/// Candle type used for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PaydayAnomalyStrategy"/> class.
	/// </summary>
	public PaydayAnomaly2Strategy()
	{
		_trade1st = Param(nameof(Trade1st), true)
			.SetDisplay("Trade 1st", "Trade on the 1st day", "General")
			.SetCanOptimize(true);

		_trade2nd = Param(nameof(Trade2nd), true)
			.SetDisplay("Trade 2nd", "Trade on the 2nd day", "General")
			.SetCanOptimize(true);

		_trade16th = Param(nameof(Trade16th), true)
			.SetDisplay("Trade 16th", "Trade on the 16th day", "General")
			.SetCanOptimize(true);

		_trade31st = Param(nameof(Trade31st), true)
			.SetDisplay("Trade 31st", "Trade on the 31st day", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_tradeOpened = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var day = candle.OpenTime.Day;
		var isTargetDay = (day == 1 && Trade1st)
			|| (day == 2 && Trade2nd)
			|| (day == 16 && Trade16th)
			|| (day == 31 && Trade31st);

		if (isTargetDay && !_tradeOpened)
		{
			BuyMarket();
			_tradeOpened = true;
		}
		else if (!isTargetDay && _tradeOpened && Position > 0)
		{
			ClosePosition();
			_tradeOpened = false;
		}
	}
}
