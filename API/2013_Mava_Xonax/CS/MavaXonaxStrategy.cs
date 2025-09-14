using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on EMA cross of open and close prices with stop and take levels.
/// </summary>
public class MavaXonaxStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private EMA? _emaClose;
	private EMA? _emaOpen;
	private EMA? _emaHigh;
	private EMA? _emaLow;

	private decimal _prevOpen1;
	private decimal _prevOpen2;
	private decimal _prevClose1;
	private decimal _prevClose2;
	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;
	private int _history;

	/// <summary>
	/// EMA period for all calculations.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MavaXonaxStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(240).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevOpen1 = _prevOpen2 = 0m;
		_prevClose1 = _prevClose2 = 0m;
		_prevHigh = _prevLow = 0m;
		_longStop = _longTake = 0m;
		_shortStop = _shortTake = 0m;
		_history = 0;
		_emaClose = _emaOpen = _emaHigh = _emaLow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaClose = new EMA { Length = EmaPeriod };
		_emaOpen = new EMA { Length = EmaPeriod };
		_emaHigh = new EMA { Length = EmaPeriod };
		_emaLow = new EMA { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_emaClose, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal closeEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openValue = _emaOpen!.Process(candle.OpenPrice);
		var highValue = _emaHigh!.Process(candle.HighPrice);
		var lowValue = _emaLow!.Process(candle.LowPrice);

		if (!openValue.IsFinal || !openValue.TryGetValue(out var openEma) ||
			!highValue.IsFinal || !highValue.TryGetValue(out var highEma) ||
			!lowValue.IsFinal || !lowValue.TryGetValue(out var lowEma))
			return;

		// Check for stop loss or take profit hits.
		if (Position > 0)
		{
			if (candle.ClosePrice <= _longStop || candle.ClosePrice >= _longTake)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice >= _shortStop || candle.ClosePrice <= _shortTake)
				BuyMarket();
		}

		if (_history >= 2 && IsFormedAndOnlineAndAllowTrading())
		{
			var step = Security.PriceStep ?? 1m;
			var buySignal = _prevOpen2 > _prevClose2 && _prevOpen1 < _prevClose1;
			var sellSignal = _prevOpen2 < _prevClose2 && _prevOpen1 > _prevClose1;

			if (buySignal && Position <= 0)
			{
				var takePr = _prevHigh - _prevLow;
				if (takePr < 600m * step)
					takePr = 600m * step;

				var stopL = 2m * (_prevOpen1 - _prevLow);
				if (stopL > 400m * step)
					stopL = 400m * step;

				var entry = candle.ClosePrice;
				_longStop = entry - stopL;
				_longTake = entry + takePr;

				BuyMarket();
			}
			else if (sellSignal && Position >= 0)
			{
				var takePr = _prevHigh - _prevLow;
				if (takePr < 600m * step)
					takePr = 600m * step;

				var stopL = 2m * (_prevHigh - _prevClose1);
				if (stopL > 400m * step)
					stopL = 400m * step;

				var entry = candle.ClosePrice;
				_shortStop = entry + stopL;
				_shortTake = entry - takePr;

				SellMarket();
			}
		}

		// Shift stored EMA values.
		_prevOpen2 = _prevOpen1;
		_prevOpen1 = openEma;
		_prevClose2 = _prevClose1;
		_prevClose1 = closeEma;
		_prevHigh = highEma;
		_prevLow = lowEma;

		if (_history < 2)
			_history++;
	}
}
