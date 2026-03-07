using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pin bar reversal strategy with ATR based stops and targets.
/// </summary>
public class PinBarReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<decimal> _maxBodyPct;
	private readonly StrategyParam<decimal> _minWickPct;
	private readonly StrategyParam<int> __atrLength;
	private readonly StrategyParam<decimal> __stopMultiplier;
	private readonly StrategyParam<decimal> __takeMultiplier;
	private readonly StrategyParam<decimal> __minAtr;
	private readonly StrategyParam<DataType> __candleType;

	/// <summary>
	/// Period for trend SMA.
	/// </summary>
	public int TrendLength
	{
		get => _trendLength.Value;
		set => _trendLength.Value = value;
	}

	/// <summary>
	/// Maximum body percent of candle range.
	/// </summary>
	public decimal MaxBodyPct
	{
		get => _maxBodyPct.Value;
		set => _maxBodyPct.Value = value;
	}

	/// <summary>
	/// Minimum wick percent of candle range.
	/// </summary>
	public decimal MinWickPct
	{
		get => _minWickPct.Value;
		set => _minWickPct.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => __atrLength.Value;
		set => __atrLength.Value = value;
	}

	/// <summary>
	/// Stop loss ATR multiplier.
	/// </summary>
	public decimal StopMultiplier
	{
		get => __stopMultiplier.Value;
		set => __stopMultiplier.Value = value;
	}

	/// <summary>
	/// Take profit ATR multiplier.
	/// </summary>
	public decimal TakeMultiplier
	{
		get => __takeMultiplier.Value;
		set => __takeMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum ATR value to allow entry.
	/// </summary>
	public decimal MinAtr
	{
		get => __minAtr.Value;
		set => __minAtr.Value = value;
	}

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType
	{
		get => __candleType.Value;
		set => __candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PinBarReversalStrategy"/>.
	/// </summary>
	public PinBarReversalStrategy()
	{
			_trendLength = Param(nameof(TrendLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trend SMA Length", "Period for trend SMA", "General")
			;

			_maxBodyPct = Param(nameof(MaxBodyPct), 0.30m)
			.SetRange(0.1m, 0.5m)
			.SetDisplay("Max Body %", "Maximum body as % of range", "Pattern")
			;

			_minWickPct = Param(nameof(MinWickPct), 0.66m)
			.SetRange(0.5m, 0.9m)
			.SetDisplay("Min Wick %", "Minimum wick as % of range", "Pattern")
			;

		__atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk")
			;

		__stopMultiplier = Param(nameof(StopMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Stop Mult", "Stop loss ATR multiplier", "Risk");

		__takeMultiplier = Param(nameof(TakeMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Take Mult", "Take profit ATR multiplier", "Risk");

		__minAtr = Param(nameof(MinAtr), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Min ATR", "Minimum ATR to allow entry", "Risk");

		__candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
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
		// no additional state to reset
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = 14 };
		var slow = new SimpleMovingAverage { Length = TrendLength };

		var prevF = 0m;
		var prevS = 0m;
		var init = false;
		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(600);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, (candle, f, s) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!fast.IsFormed || !slow.IsFormed)
					return;

				if (!init)
				{
					prevF = f;
					prevS = s;
					init = true;
					return;
				}

				if (candle.OpenTime - lastSignal >= cooldown)
				{
					if (prevF <= prevS && f > s && Position <= 0)
					{
						BuyMarket();
						lastSignal = candle.OpenTime;
					}
					else if (prevF >= prevS && f < s && Position >= 0)
					{
						SellMarket();
						lastSignal = candle.OpenTime;
					}
				}

				prevF = f;
				prevS = s;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}
}
