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
/// Strategy based on Coppock histogram turns to capture trend reversals.
/// </summary>
public class CoppockHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _roc1Period;
	private readonly StrategyParam<int> _roc2Period;
	private readonly StrategyParam<int> _smoothPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalCooldownBars;
	private static readonly object _sync = new();

	private SimpleMovingAverage _sma = null!;
	private decimal? _prev;
	private decimal? _prev2;
	private int _cooldownRemaining;

	/// <summary>
	/// First rate of change period.
	/// </summary>
	public int Roc1Period
	{
		get => _roc1Period.Value;
		set => _roc1Period.Value = value;
	}

	/// <summary>
	/// Second rate of change period.
	/// </summary>
	public int Roc2Period
	{
		get => _roc2Period.Value;
		set => _roc2Period.Value = value;
	}

	/// <summary>
	/// Moving average smoothing length.
	/// </summary>
	public int SmoothPeriod
	{
		get => _smoothPeriod.Value;
		set => _smoothPeriod.Value = value;
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
	/// Closed candles to wait before acting on the next turn.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CoppockHistogramStrategy"/>.
	/// </summary>
	public CoppockHistogramStrategy()
	{
		_roc1Period = Param(nameof(Roc1Period), 14)
			.SetRange(1, 200)
			.SetDisplay("ROC1 Period", "First ROC length", "Parameters")
			;

		_roc2Period = Param(nameof(Roc2Period), 11)
			.SetRange(1, 200)
			.SetDisplay("ROC2 Period", "Second ROC length", "Parameters")
			;

		_smoothPeriod = Param(nameof(SmoothPeriod), 3)
			.SetRange(1, 50)
			.SetDisplay("Smoothing", "Moving average length", "Parameters")
			;


		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 2)
			.SetNotNegative()
			.SetDisplay("Signal Cooldown", "Closed candles to wait before the next trade", "Parameters");
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

		_sma?.Reset();
		_prev = null;
		_prev2 = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SMA { Length = SmoothPeriod };

		var roc1 = new RateOfChange { Length = Roc1Period };
		var roc2 = new RateOfChange { Length = Roc2Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(roc1, roc2, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}

		StartProtection(null, null);
	}

	private void ProcessCandle(ICandleMessage candle, decimal roc1Value, decimal roc2Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		lock (_sync)
		{
			if (_cooldownRemaining > 0)
				_cooldownRemaining--;

			var smoothValue = _sma.Process(roc1Value + roc2Value, candle.OpenTime, true);
			if (!smoothValue.IsFinal || smoothValue.IsEmpty || !_sma.IsFormed)
				return;

			var coppock = smoothValue.ToDecimal();

			if (_prev is decimal prev && _prev2 is decimal prev2)
			{
				if (_cooldownRemaining == 0 && prev < prev2 && coppock > prev && Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
					_cooldownRemaining = SignalCooldownBars;
				}
				else if (_cooldownRemaining == 0 && prev > prev2 && coppock < prev && Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					_cooldownRemaining = SignalCooldownBars;
				}
			}

			_prev2 = _prev;
			_prev = coppock;
		}
	}
}
