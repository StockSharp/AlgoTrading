namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// ATR based channel breakout scalping strategy.
/// Builds dynamic upper and lower bands around candle midpoints and enters when the close crosses the bands.
/// </summary>
public class ChannelScalperStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _up;
	private decimal _down;
	private int _direction;
	private bool _isInitialized;

	/// <summary>
	/// ATR period length.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Multiplier applied to ATR to form channel width.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChannelScalperStrategy"/> class.
	/// </summary>
	public ChannelScalperStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 11).SetDisplay("ATR Period").SetCanOptimize(true);
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.28m).SetDisplay("ATR Multiplier").SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished || atr <= 0)
			return;

		var middle = (candle.HighPrice + candle.LowPrice) / 2;
		var currentUp = middle + AtrMultiplier * atr;
		var currentDown = middle - AtrMultiplier * atr;

		if (!_isInitialized)
		{
			_up = currentUp;
			_down = currentDown;
			_isInitialized = true;
			return;
		}

		// Check for breakout using previous channel values.
		if (_direction <= 0 && candle.ClosePrice > _up)
		{
			if (Position <= 0)
				BuyMarket();
			_direction = 1;
		}
		else if (_direction >= 0 && candle.ClosePrice < _down)
		{
			if (Position >= 0)
				SellMarket();
			_direction = -1;
		}

		// Update trailing channel according to current direction.
		if (_direction > 0)
			currentDown = Math.Max(currentDown, _down);
		else if (_direction < 0)
			currentUp = Math.Min(currentUp, _up);

		_up = currentUp;
		_down = currentDown;
	}
}
