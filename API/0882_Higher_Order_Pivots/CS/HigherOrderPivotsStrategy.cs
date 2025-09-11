namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Detects 1st, 2nd and 3rd order pivot highs and lows.
/// </summary>
public class HigherOrderPivotsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useThreeBar;
	private readonly StrategyParam<bool> _displayFirstOrder;
	private readonly StrategyParam<bool> _displaySecondOrder;
	private readonly StrategyParam<bool> _displayThirdOrder;

	private readonly decimal[] _high = new decimal[6];
	private readonly decimal[] _low = new decimal[6];
	private readonly DateTimeOffset[] _time = new DateTimeOffset[6];
	private int _bufferSize;

	private readonly (decimal price, DateTimeOffset time)?[] _pv1Highs = new (decimal, DateTimeOffset)?[3];
	private readonly (decimal price, DateTimeOffset time)?[] _pv1Lows = new (decimal, DateTimeOffset)?[3];
	private readonly (decimal price, DateTimeOffset time)?[] _pv2Highs = new (decimal, DateTimeOffset)?[3];
	private readonly (decimal price, DateTimeOffset time)?[] _pv2Lows = new (decimal, DateTimeOffset)?[3];
	private readonly (decimal price, DateTimeOffset time)?[] _pv3Highs = new (decimal, DateTimeOffset)?[3];
	private readonly (decimal price, DateTimeOffset time)?[] _pv3Lows = new (decimal, DateTimeOffset)?[3];

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Use 3-bar pivots instead of 5-bar.
	/// </summary>
	public bool UseThreeBar
	{
		get => _useThreeBar.Value;
		set => _useThreeBar.Value = value;
	}

	/// <summary>
	/// Display first order pivots.
	/// </summary>
	public bool DisplayFirstOrder
	{
		get => _displayFirstOrder.Value;
		set => _displayFirstOrder.Value = value;
	}

	/// <summary>
	/// Display second order pivots.
	/// </summary>
	public bool DisplaySecondOrder
	{
		get => _displaySecondOrder.Value;
		set => _displaySecondOrder.Value = value;
	}

	/// <summary>
	/// Display third order pivots.
	/// </summary>
	public bool DisplayThirdOrder
	{
		get => _displayThirdOrder.Value;
		set => _displayThirdOrder.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HigherOrderPivotsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_useThreeBar = Param(nameof(UseThreeBar), true)
			.SetDisplay("3 Bar Pivots", "Use 3-bar or 5-bar first order pivots", "General");

		_displayFirstOrder = Param(nameof(DisplayFirstOrder), true)
			.SetDisplay("Display 1st Order", "Show first order pivots", "Visual");

		_displaySecondOrder = Param(nameof(DisplaySecondOrder), true)
			.SetDisplay("Display 2nd Order", "Show second order pivots", "Visual");

		_displayThirdOrder = Param(nameof(DisplayThirdOrder), true)
			.SetDisplay("Display 3rd Order", "Show third order pivots", "Visual");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		Array.Clear(_high, 0, _high.Length);
		Array.Clear(_low, 0, _low.Length);
		Array.Clear(_time, 0, _time.Length);
		_bufferSize = 0;

		Clear(_pv1Highs);
		Clear(_pv1Lows);
		Clear(_pv2Highs);
		Clear(_pv2Lows);
		Clear(_pv3Highs);
		Clear(_pv3Lows);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		for (var i = _high.Length - 1; i > 0; i--)
		{
			_high[i] = _high[i - 1];
			_low[i] = _low[i - 1];
			_time[i] = _time[i - 1];
		}

		_high[0] = candle.HighPrice;
		_low[0] = candle.LowPrice;
		_time[0] = candle.OpenTime;
		_bufferSize = Math.Min(_bufferSize + 1, _high.Length);

		DetectFirstOrderPivots();
	}

	private void DetectFirstOrderPivots()
	{
		if (UseThreeBar)
		{
			if (_bufferSize < 4)
				return;

			var pvh = _high[1] < _high[2] && _high[2] > _high[3];
			var pvl = _low[1] > _low[2] && _low[2] < _low[3];

			if (pvh)
				AddFirstHigh(_high[2], _time[2]);

			if (pvl)
				AddFirstLow(_low[2], _time[2]);
		}
		else
		{
			if (_bufferSize < 6)
				return;

			var pvh = _high[1] < _high[2] && _high[2] < _high[3] && _high[3] > _high[4] && _high[4] > _high[5];
			var pvl = _low[1] > _low[2] && _low[2] > _low[3] && _low[3] < _low[4] && _low[4] < _low[5];

			if (pvh)
				AddFirstHigh(_high[3], _time[3]);

			if (pvl)
				AddFirstLow(_low[3], _time[3]);
		}
	}

	private void AddFirstHigh(decimal price, DateTimeOffset time)
	{
		Shift(_pv1Highs);
		_pv1Highs[0] = (price, time);

		if (DisplayFirstOrder)
			LogInfo($"1st order pivot high {price} at {time:O}");

		if (_pv1Highs[2] is { } c && _pv1Highs[1] is { } b && _pv1Highs[0] is { } a &&
			a.price < b.price && b.price > c.price)
		{
			AddSecondHigh(b.price, b.time);
		}
	}

	private void AddFirstLow(decimal price, DateTimeOffset time)
	{
		Shift(_pv1Lows);
		_pv1Lows[0] = (price, time);

		if (DisplayFirstOrder)
			LogInfo($"1st order pivot low {price} at {time:O}");

		if (_pv1Lows[2] is { } c && _pv1Lows[1] is { } b && _pv1Lows[0] is { } a &&
			a.price > b.price && b.price < c.price)
		{
			AddSecondLow(b.price, b.time);
		}
	}

	private void AddSecondHigh(decimal price, DateTimeOffset time)
	{
		Shift(_pv2Highs);
		_pv2Highs[0] = (price, time);

		if (DisplaySecondOrder)
			LogInfo($"2nd order pivot high {price} at {time:O}");

		if (_pv2Highs[2] is { } c && _pv2Highs[1] is { } b && _pv2Highs[0] is { } a &&
			a.price < b.price && b.price > c.price)
		{
			AddThirdHigh(b.price, b.time);
		}
	}

	private void AddSecondLow(decimal price, DateTimeOffset time)
	{
		Shift(_pv2Lows);
		_pv2Lows[0] = (price, time);

		if (DisplaySecondOrder)
			LogInfo($"2nd order pivot low {price} at {time:O}");

		if (_pv2Lows[2] is { } c && _pv2Lows[1] is { } b && _pv2Lows[0] is { } a &&
			a.price > b.price && b.price < c.price)
		{
			AddThirdLow(b.price, b.time);
		}
	}

	private void AddThirdHigh(decimal price, DateTimeOffset time)
	{
		Shift(_pv3Highs);
		_pv3Highs[0] = (price, time);

		if (DisplayThirdOrder)
			LogInfo($"3rd order pivot high {price} at {time:O}");
	}

	private void AddThirdLow(decimal price, DateTimeOffset time)
	{
		Shift(_pv3Lows);
		_pv3Lows[0] = (price, time);

		if (DisplayThirdOrder)
			LogInfo($"3rd order pivot low {price} at {time:O}");
	}

	private static void Shift((decimal price, DateTimeOffset time)?[] array)
	{
		for (var i = array.Length - 1; i > 0; i--)
			array[i] = array[i - 1];
	}

	private static void Clear((decimal price, DateTimeOffset time)?[] array)
	{
		for (var i = 0; i < array.Length; i++)
			array[i] = null;
	}
}

