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
/// Strategy based on Jurik smoothed momentum.
/// Opens a long position when momentum turns up and a short position when momentum turns down.
/// Optional stop loss and take profit are applied through <see cref="StartProtection"/>.
/// </summary>
public class ColorJMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _enableStopLoss;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;

	private JurikMovingAverage _jma;
	private decimal? _prevValue;
	private decimal? _prevPrevValue;

	/// <summary>
	/// Length for momentum calculation.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Length for Jurik moving average smoothing.
	/// </summary>
	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Enable usage of stop loss.
	/// </summary>
	public bool EnableStopLoss
	{
		get => _enableStopLoss.Value;
		set => _enableStopLoss.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	public ColorJMomentumStrategy()
	{
		_momentumLength = Param(nameof(MomentumLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Period for momentum", "Parameters")
			
			.SetOptimize(5, 20, 1);

		_jmaLength = Param(nameof(JmaLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "Period for Jurik moving average", "Parameters")
			
			.SetOptimize(5, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "Parameters");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk management")
			
			.SetOptimize(0.5m, 3m, 0.5m);

		_enableStopLoss = Param(nameof(EnableStopLoss), true)
			.SetDisplay("Enable Stop Loss", "Use stop loss", "Risk management");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk management")
			
			.SetOptimize(1m, 5m, 1m);

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long entries", "General");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short entries", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var momentum = new Momentum { Length = MomentumLength };
		_jma = new JurikMovingAverage { Length = JmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(momentum, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: EnableStopLoss ? new Unit(StopLossPercent, UnitTypes.Percent) : null);
	}

	private void ProcessCandle(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var jmaResult = _jma.Process(momentumValue, candle.OpenTime, true);
		if (!_jma.IsFormed)
			return;

		var jMomentum = jmaResult.ToDecimal();

		if (_prevValue is decimal prev && _prevPrevValue is decimal prevPrev)
		{
			var wasDecreasing = prev < prevPrev;
			var nowIncreasing = jMomentum > prev;
			var wasIncreasing = prev > prevPrev;
			var nowDecreasing = jMomentum < prev;

			if (wasDecreasing && nowIncreasing && EnableLong && Position <= 0)
			{
				BuyMarket();
			}
			else if (wasIncreasing && nowDecreasing && EnableShort && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevPrevValue = _prevValue;
		_prevValue = jMomentum;
	}
}