using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VininI Trend LRMA strategy.
/// Uses linear regression moving average to detect breakouts or twists.
/// </summary>
public class VininITrendLrmaStrategy : Strategy
{
	/// <summary>
	/// Entry algorithm.
	/// </summary>
	public enum EntryMode
	{
		/// <summary>Enter on level breakout.</summary>
		Breakdown,
		/// <summary>Enter when direction changes.</summary>
		Twist
	}

	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _upLevel;
	private readonly StrategyParam<int> _dnLevel;
	private readonly StrategyParam<EntryMode> _mode;
	private readonly StrategyParam<DataType> _candleType;

	private LinearRegression _lrma = null!;

	private decimal? _prev;
	private decimal? _prevPrev;

	/// <summary>
	/// Initializes a new instance of <see cref="VininITrendLrmaStrategy"/>.
	/// </summary>
	public VininITrendLrmaStrategy()
	{
		_period = Param(nameof(Period), 13).SetDisplay("LRMA period").SetCanOptimize(true);
		_upLevel = Param(nameof(UpLevel), 10).SetDisplay("Upper level").SetCanOptimize(true);
		_dnLevel = Param(nameof(DnLevel), -10).SetDisplay("Lower level").SetCanOptimize(true);
		_mode = Param(nameof(Mode), EntryMode.Breakdown).SetDisplay("Entry mode");
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type");
	}

	/// <summary>LRMA period.</summary>
	public int Period { get => _period.Value; set => _period.Value = value; }

	/// <summary>Upper trigger level.</summary>
	public int UpLevel { get => _upLevel.Value; set => _upLevel.Value = value; }

	/// <summary>Lower trigger level.</summary>
	public int DnLevel { get => _dnLevel.Value; set => _dnLevel.Value = value; }

	/// <summary>Entry algorithm.</summary>
	public EntryMode Mode { get => _mode.Value; set => _mode.Value = value; }

	/// <summary>Candle series type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_lrma = new LinearRegression { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_lrma, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal lrma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Mode == EntryMode.Breakdown)
		{
			if (_prev is not null)
			{
				if (lrma > UpLevel && _prev <= UpLevel && Position <= 0)
					BuyMarket();
				else if (lrma < DnLevel && _prev >= DnLevel && Position >= 0)
					SellMarket();
			}
		}
		else
		{
			if (_prev is not null && _prevPrev is not null)
			{
				if (_prevPrev < _prev && lrma > _prev && Position <= 0)
					BuyMarket();
				else if (_prevPrev > _prev && lrma < _prev && Position >= 0)
					SellMarket();
			}
		}

		_prevPrev = _prev;
		_prev = lrma;
	}
}
