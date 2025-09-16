using System;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum TrendMode
{
	Direct,
	NotDirect
}

/// <summary>
/// Strategy based on the Waddah Attar Trend indicator.
/// Opens a long position when the trend color changes from down to up and
/// opens a short position when the color switches from up to down.
/// </summary>
public class WaddahAttarTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<TrendMode> _trendMode;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevTrend;
	private decimal[] _colors = Array.Empty<decimal>();
	private int _bufferIndex;

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Smoothing moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Number of bars back used to detect signals.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Determines how indicator colors are interpreted.
	/// </summary>
	public TrendMode TrendMode
	{
		get => _trendMode.Value;
		set => _trendMode.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public WaddahAttarTrendStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Fast EMA period for MACD", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Slow EMA period for MACD", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 1);

		_maLength = Param(nameof(MaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Smoothing moving average period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Signal Bar", "Bar offset for signal detection", "General");

		_trendMode = Param(nameof(TrendMode), Strategies.TrendMode.Direct)
			.SetDisplay("Trend Mode", "Interpretation of indicator colors", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevTrend = 0m;
		_colors = Array.Empty<decimal>();
		_bufferIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_colors = new decimal[SignalBar + 2];
		_bufferIndex = 0;

		var fastEma = new EMA { Length = FastLength };
		var slowEma = new EMA { Length = SlowLength };
		var ma = new SMA { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastEma, slowEma, ma, ProcessCandle)
			.Start();

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal maValue)
	{
		// Work only with finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macd = fast - slow;
		var trend = macd * maValue;
		var color = trend >= _prevTrend ? 0m : 1m;

		_colors[_bufferIndex % _colors.Length] = color;
		_bufferIndex++;

		if (_bufferIndex <= SignalBar + 1)
		{
			_prevTrend = trend;
			return;
		}

		var signalIndex = (_bufferIndex - SignalBar - 1) % _colors.Length;
		var prevSignalIndex = (_bufferIndex - SignalBar - 2) % _colors.Length;

		var signalColor = _colors[signalIndex];
		var prevSignalColor = _colors[prevSignalIndex];

		var volume = Volume + Math.Abs(Position);

		if (TrendMode == Strategies.TrendMode.Direct)
		{
			if (prevSignalColor == 0m && signalColor > 0m)
				BuyMarket(volume);
			else if (prevSignalColor == 1m && signalColor < 1m)
				SellMarket(volume);
		}
		else
		{
			if (prevSignalColor == 1m && signalColor < 1m)
				BuyMarket(volume);
			else if (prevSignalColor == 0m && signalColor > 0m)
				SellMarket(volume);
		}

		_prevTrend = trend;
	}
}
