using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades against sharp price moves using ROC indicator.
/// Short when ROC rises above threshold, long when ROC falls below negative threshold.
/// </summary>
public class AnomalyCounterTrendStrategy : Strategy
{
	private readonly StrategyParam<decimal> _percentageThreshold;
	private readonly StrategyParam<int> _rocLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private int _barIndex;
	private int _lastTradeBar;

	/// <summary>
	/// Minimum ROC percentage to detect anomaly.
	/// </summary>
	public decimal PercentageThreshold
	{
		get => _percentageThreshold.Value;
		set => _percentageThreshold.Value = value;
	}

	/// <summary>
	/// ROC lookback period.
	/// </summary>
	public int RocLength
	{
		get => _rocLength.Value;
		set => _rocLength.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AnomalyCounterTrendStrategy"/>.
	/// </summary>
	public AnomalyCounterTrendStrategy()
	{
		_percentageThreshold = Param(nameof(PercentageThreshold), 1m)
			.SetDisplay("Percentage Threshold", "Minimum ROC to trigger counter trade", "Anomaly Detection");

		_rocLength = Param(nameof(RocLength), 60)
			.SetDisplay("ROC Length", "Rate of change lookback period", "Anomaly Detection");

		_cooldownBars = Param(nameof(CooldownBars), 200)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_lastTradeBar = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var roc = new RateOfChange { Length = RocLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(roc, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rocValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var cooldownOk = _barIndex - _lastTradeBar > CooldownBars;

		// Counter-trend: sell when sharp rise, buy when sharp fall
		if (rocValue >= PercentageThreshold && Position >= 0 && cooldownOk)
		{
			SellMarket();
			_lastTradeBar = _barIndex;
		}
		else if (rocValue <= -PercentageThreshold && Position <= 0 && cooldownOk)
		{
			BuyMarket();
			_lastTradeBar = _barIndex;
		}
	}
}
