namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// LANZ Strategy 6.0: trades the 09:00 New York hour candle with risk management.
/// </summary>
public class LanzStrategy60BacktestStrategy : Strategy
{
	private readonly StrategyParam<decimal> _slPercent;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<decimal> _accountSizeUsd;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<bool> _useManualPipValue;
	private readonly StrategyParam<decimal> _manualPipValue;
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<DataType> _candleType;

	private readonly TimeZoneInfo _nyZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

	/// <summary>
	/// Stop-loss as percent of candle range.
	/// </summary>
	public decimal SlPercent
	{
		get => _slPercent.Value;
		set => _slPercent.Value = value;
	}

	/// <summary>
	/// Risk-reward ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Account capital in USD.
	/// </summary>
	public decimal AccountSizeUsd
	{
		get => _accountSizeUsd.Value;
		set => _accountSizeUsd.Value = value;
	}

	/// <summary>
	/// Risk percent per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Use manual pip value.
	/// </summary>
	public bool UseManualPipValue
	{
		get => _useManualPipValue.Value;
		set => _useManualPipValue.Value = value;
	}

	/// <summary>
	/// Manual pip value.
	/// </summary>
	public decimal ManualPipValue
	{
		get => _manualPipValue.Value;
		set => _manualPipValue.Value = value;
	}

	/// <summary>
	/// Enable buy entries.
	/// </summary>
	public bool EnableBuy
	{
		get => _enableBuy.Value;
		set => _enableBuy.Value = value;
	}

	/// <summary>
	/// Enable sell entries.
	/// </summary>
	public bool EnableSell
	{
		get => _enableSell.Value;
		set => _enableSell.Value = value;
	}

	/// <summary>
	/// Candle type used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public LanzStrategy60BacktestStrategy()
	{
		_slPercent = Param(nameof(SlPercent), 0.05m)
			.SetDisplay("SL %", "Stop loss percent of candle range", "Risk")
			.SetCanOptimize(true);

		_riskReward = Param(nameof(RiskReward), 5m)
			.SetDisplay("Risk Reward", "Risk reward ratio", "Risk")
			.SetCanOptimize(true);

		_accountSizeUsd = Param(nameof(AccountSizeUsd), 10000m)
			.SetDisplay("Account Size USD", "Account capital", "Money");

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetDisplay("Risk %", "Risk percent per trade", "Money")
			.SetCanOptimize(true);

		_useManualPipValue = Param(nameof(UseManualPipValue), true)
			.SetDisplay("Manual Pip Value", "Use manual pip value", "Money");

		_manualPipValue = Param(nameof(ManualPipValue), 1m)
			.SetDisplay("Pip Value", "Manual pip value", "Money");

		_enableBuy = Param(nameof(EnableBuy), true)
			.SetDisplay("Enable Buy", "Allow long entries", "General");

		_enableSell = Param(nameof(EnableSell), false)
			.SetDisplay("Enable Sell", "Allow short entries", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var nyTime = TimeZoneInfo.ConvertTime(candle.OpenTime, _nyZone);

		if (nyTime.Hour == 15 && nyTime.Minute == 0)
		{
		if (Position > 0)
		SellMarket(Position);
		else if (Position < 0)
		BuyMarket(-Position);

		return;
		}

		var isBull = nyTime.Hour == 9 && candle.ClosePrice > candle.OpenPrice;
		var isBear = nyTime.Hour == 9 && candle.ClosePrice < candle.OpenPrice;

		if ((!isBull || !EnableBuy) && (!isBear || !EnableSell))
		return;

		var entryPrice = candle.ClosePrice;
		var hi = candle.HighPrice;
		var lo = candle.LowPrice;
		var candleRange = hi - lo;

		var slPrice = SlPercent == 0m
		? (isBull ? lo : hi)
		: (isBull ? lo - candleRange * SlPercent : hi + candleRange * SlPercent);

		var risk = Math.Abs(entryPrice - slPrice);
		var tpPrice = isBull ? entryPrice + risk * RiskReward : entryPrice - risk * RiskReward;

		var step = Security.PriceStep ?? 1m;
		var pipSize = step * 10m;
		var slPips = pipSize > 0m ? risk / pipSize : 0m;
		var pipValue = UseManualPipValue || Security.Board?.Code != "FX"
		? ManualPipValue
		: (Security.Code.EndsWith("USD", StringComparison.OrdinalIgnoreCase)
		? 10m
		: Security.Code.StartsWith("USD", StringComparison.OrdinalIgnoreCase)
		? 100000m * pipSize / candle.ClosePrice
		: ManualPipValue);

		var riskUsd = AccountSizeUsd * (RiskPercent / 100m);
		var qty = slPips > 0m && pipValue > 0m ? riskUsd / (slPips * pipValue) : 0m;
		var volume = qty + Math.Abs(Position);

		if (isBull && EnableBuy && Position <= 0)
		{
		BuyLimit(volume, entryPrice);
		SellStop(volume, slPrice);
		SellLimit(volume, tpPrice);
		}
		else if (isBear && EnableSell && Position >= 0)
		{
		SellLimit(volume, entryPrice);
		BuyStop(volume, slPrice);
		BuyLimit(volume, tpPrice);
		}
	}
}
