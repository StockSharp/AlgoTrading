using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy inspired by the Doji Trader Expert Advisor.
/// Looks for a recent doji candle and trades when the next candle closes beyond the doji range.
/// </summary>
public class DojiTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _maximumDojiHeight;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _previousCandle;
	private ICandleMessage _twoAgoCandle;
	private ICandleMessage _threeAgoCandle;
	private decimal _pipSize;

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// First trading hour (inclusive) using exchange time.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Last trading hour (exclusive) using exchange time.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Maximum body height for a candle to be considered a doji (in pips).
	/// </summary>
	public decimal MaximumDojiHeight
	{
		get => _maximumDojiHeight.Value;
		set => _maximumDojiHeight.Value = value;
	}

	/// <summary>
	/// Candle type used for pattern detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DojiTraderStrategy"/>.
	/// </summary>
	public DojiTraderStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Protection")
			.SetRange(0m, 500m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit", "Take-profit distance in pips", "Protection")
			.SetRange(0m, 500m);

		_startHour = Param(nameof(StartHour), 8)
			.SetDisplay("Start Hour", "Hour when trading becomes active", "Session")
			.SetRange(0, 23);

		_endHour = Param(nameof(EndHour), 17)
			.SetDisplay("End Hour", "Hour when trading stops (exclusive)", "Session")
			.SetRange(1, 24);

		_maximumDojiHeight = Param(nameof(MaximumDojiHeight), 1m)
			.SetDisplay("Doji Height", "Maximum doji body height in pips", "Pattern")
			.SetRange(0.1m, 20m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for doji detection", "General");
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

		_previousCandle = null;
		_twoAgoCandle = null;
		_threeAgoCandle = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		// Configure stop-loss and take-profit protection once at start.
		var takeProfitUnit = TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : default;
		var stopLossUnit = StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : default;

		if (takeProfitUnit != default || stopLossUnit != default)
		{
			StartProtection(takeProfitUnit, stopLossUnit, useMarketOrders: true);
		}
		else
		{
			StartProtection();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Process only finished candles.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the strategy is ready to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			ShiftHistory(candle);
			return;
		}

		// Skip trading outside the configured session window.
		var nextHour = candle.CloseTime.Hour;
		if (nextHour < StartHour || nextHour >= EndHour)
		{
			ShiftHistory(candle);
			return;
		}

		// We need at least three completed candles for pattern detection.
		if (_twoAgoCandle is null)
		{
			ShiftHistory(candle);
			return;
		}

		var pipSize = _pipSize > 0m ? _pipSize : (_pipSize = CalculatePipSize());
		var dojiHeight = MaximumDojiHeight * pipSize;

		var dojiHigh = 0m;
		var dojiLow = 0m;

		// Check the two candles before the current close for the most recent doji.
		if (IsDoji(_twoAgoCandle, dojiHeight))
		{
			dojiHigh = _twoAgoCandle.HighPrice;
			dojiLow = _twoAgoCandle.LowPrice;
		}
		else if (_threeAgoCandle is not null && IsDoji(_threeAgoCandle, dojiHeight))
		{
			dojiHigh = _threeAgoCandle.HighPrice;
			dojiLow = _threeAgoCandle.LowPrice;
		}
		else
		{
			ShiftHistory(candle);
			return;
		}

		var direction = 0;

		// Long signal when the latest candle closes above the doji range.
		if (candle.ClosePrice > dojiHigh)
		{
			direction = 1;
		}
		// Short signal when the latest candle closes below the doji range.
		else if (candle.ClosePrice < dojiLow)
		{
			direction = -1;
		}

		if (direction != 0 && Volume > 0m)
		{
			if (direction > 0)
			{
				// Buy enough volume to cover a short position and establish the target long size.
				var volume = Volume + Math.Max(0m, -Position);
				if (volume > 0m)
					BuyMarket(volume);
			}
			else
			{
				// Sell enough volume to cover a long position and establish the target short size.
				var volume = Volume + Math.Max(0m, Position);
				if (volume > 0m)
					SellMarket(volume);
			}
		}

		ShiftHistory(candle);
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;

		var digits = 0;
		var value = step;

		while (value < 1m && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		var multiplier = (digits == 3 || digits == 5) ? 10m : 1m;
		return step * multiplier;
	}

	private static bool IsDoji(ICandleMessage candle, decimal threshold)
	{
		var body = Math.Abs(candle.OpenPrice - candle.ClosePrice);
		return body <= threshold;
	}

	private void ShiftHistory(ICandleMessage candle)
	{
		// Maintain the three most recent completed candles for doji detection.
		_threeAgoCandle = _twoAgoCandle;
		_twoAgoCandle = _previousCandle;
		_previousCandle = candle;
	}
}
