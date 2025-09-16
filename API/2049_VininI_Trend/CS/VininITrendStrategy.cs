using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the VininI Trend concept using the CCI indicator.
/// </summary>
public class VininITrendStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _upLevel;
	private readonly StrategyParam<int> _downLevel;
	private readonly StrategyParam<Mode> _entryMode;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prev1;
	private decimal? _prev2;
	private CommodityChannelIndex _cci;

	/// <summary>
	/// Period for the CCI indicator.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Upper threshold for generating buy signals.
	/// </summary>
	public int UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold for generating sell signals.
	/// </summary>
	public int DownLevel
	{
		get => _downLevel.Value;
		set => _downLevel.Value = value;
	}

	/// <summary>
	/// Mode for entry signal calculation.
	/// </summary>
	public Mode EntryMode
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the strategy parameters.
	/// </summary>
	public VininITrendStrategy()
	{
		_period = Param(nameof(Period), 20)
			.SetDisplay("CCI Period", "Period for the CCI indicator", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_upLevel = Param(nameof(UpLevel), 10)
			.SetDisplay("Upper Level", "Upper threshold to trigger buy", "Parameters");

		_downLevel = Param(nameof(DownLevel), -10)
			.SetDisplay("Lower Level", "Lower threshold to trigger sell", "Parameters");

		_entryMode = Param(nameof(EntryMode), Mode.Breakdown)
			.SetDisplay("Entry Mode", "Signal generation mode", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_prev1 = _prev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cci = new CommodityChannelIndex { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}
	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var prev = _prev1;
		var prev2 = _prev2;

		var buySignal = false;
		var sellSignal = false;

		switch (EntryMode)
		{
			case Mode.Breakdown:
				if (prev is not null)
				{
					if (prev <= UpLevel && cciValue > UpLevel)
						buySignal = true;

					if (prev >= DownLevel && cciValue < DownLevel)
						sellSignal = true;
				}
				break;

			case Mode.Twist:
				if (prev is not null && prev2 is not null)
				{
					if (prev < prev2 && cciValue > prev)
						buySignal = true;

					if (prev > prev2 && cciValue < prev)
						sellSignal = true;
				}
				break;
		}

		var volume = Volume + Math.Abs(Position);

		if (buySignal && Position <= 0)
			BuyMarket(volume);
		else if (sellSignal && Position >= 0)
			SellMarket(volume);

		_prev2 = prev;
		_prev1 = cciValue;
	}

	/// <summary>
	/// Available entry modes.
	/// </summary>
	public enum Mode
	{
		/// <summary>
		/// Trigger when the oscillator breaks predefined levels.
		/// </summary>
		Breakdown,

		/// <summary>
		/// Trigger when the oscillator changes direction.
		/// </summary>
		Twist
	}
}
