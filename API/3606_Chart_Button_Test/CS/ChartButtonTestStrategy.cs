using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Chartbuttontest translation (44020).
/// Emulates a draggable chart button by drawing a dynamic rectangle and text label.
/// </summary>
public class ChartButtonTestStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookbackCandles;
	private readonly StrategyParam<decimal> _priceHeight;
	private readonly StrategyParam<string> _buttonText;
	private readonly StrategyParam<bool> _lockHorizontal;

	private readonly Queue<DateTimeOffset> _timeWindow = new();

	private IChartArea? _area;
	private DateTimeOffset? _leftTime;
	private DateTimeOffset? _rightTime;
	private decimal _topPrice;
	private decimal _bottomPrice;
	private bool _initialized;
	private decimal _lastLoggedPrice;
	private DateTimeOffset? _lastLoggedTime;

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of candles used to position the left edge of the button.
	/// </summary>
	public int LookbackCandles
	{
		get => _lookbackCandles.Value;
		set => _lookbackCandles.Value = value;
	}

	/// <summary>
	/// Height of the emulated chart button in price units.
	/// </summary>
	public decimal PriceHeight
	{
		get => _priceHeight.Value;
		set => _priceHeight.Value = value;
	}

	/// <summary>
	/// Text displayed near the button.
	/// </summary>
	public string ButtonText
	{
		get => _buttonText.Value;
		set => _buttonText.Value = value;
	}

	/// <summary>
	/// Keeps the button anchored to its initial time when enabled.
	/// </summary>
	public bool LockHorizontalMovement
	{
		get => _lockHorizontal.Value;
		set => _lockHorizontal.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ChartButtonTestStrategy"/>.
	/// </summary>
	public ChartButtonTestStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to observe", "General");

		_lookbackCandles = Param(nameof(LookbackCandles), 100)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Candles", "Number of candles defining the left anchor", "Visualization");

		_priceHeight = Param(nameof(PriceHeight), 0.001m)
			.SetGreaterThanZero()
			.SetDisplay("Button Height", "Vertical size of the emulated button", "Visualization");

		_buttonText = Param(nameof(ButtonText), "Button price:")
			.SetDisplay("Button Text", "Label shown near the emulated button", "Visualization")
			.SetCanOptimize(false);

		_lockHorizontal = Param(nameof(LockHorizontalMovement), false)
			.SetDisplay("Lock Horizontal Movement", "Keep the button anchored to the initial time", "Visualization");
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

		_timeWindow.Clear();
		_area = null;
		_leftTime = null;
		_rightTime = null;
		_topPrice = 0m;
		_bottomPrice = 0m;
		_initialized = false;
		_lastLoggedPrice = 0m;
		_lastLoggedTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		_area = CreateChartArea();
		if (_area != null)
		{
			DrawCandles(_area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_timeWindow.Enqueue(candle.OpenTime);

		while (_timeWindow.Count > LookbackCandles)
			_timeWindow.Dequeue();

		if (!_initialized)
		{
			_bottomPrice = GetInitialPrice(candle);
			_topPrice = _bottomPrice + Math.Abs(PriceHeight);
			_rightTime = candle.OpenTime;

			if (_timeWindow.Count > 0)
				_leftTime = _timeWindow.Peek();

			_initialized = true;

			AddInfoLog($"Button created at {FormatPrice(_bottomPrice)} (time {_rightTime:O}).");
		}
		else
		{
			if (!LockHorizontalMovement && _timeWindow.Count > 0)
				_leftTime = _timeWindow.Peek();

			_rightTime = candle.OpenTime;
			_bottomPrice = candle.ClosePrice;
			_topPrice = _bottomPrice + Math.Abs(PriceHeight);
		}

		UpdateChart();

		if (_initialized && (_lastLoggedTime != _rightTime || _lastLoggedPrice != _bottomPrice))
		{
			_lastLoggedPrice = _bottomPrice;
			_lastLoggedTime = _rightTime;
			AddInfoLog($"Button moved to {FormatPrice(_bottomPrice)} at {_rightTime:O}.");
		}
	}

	private decimal GetInitialPrice(ICandleMessage candle)
	{
		if (Security?.BestAskPrice is decimal ask && ask > 0m)
			return ask;

		if (Security?.LastPrice is decimal last && last > 0m)
			return last;

		return candle.ClosePrice > 0m ? candle.ClosePrice : Math.Abs(PriceHeight);
	}

	private void UpdateChart()
	{
		if (_area == null || _leftTime == null || _rightTime == null)
			return;

		DrawLine(_leftTime.Value, _topPrice, _rightTime.Value, _topPrice);
		DrawLine(_leftTime.Value, _bottomPrice, _rightTime.Value, _bottomPrice);
		DrawLine(_leftTime.Value, _topPrice, _leftTime.Value, _bottomPrice);
		DrawLine(_rightTime.Value, _topPrice, _rightTime.Value, _bottomPrice);

		var decimals = Security?.Decimals ?? 2;
		var format = $"F{decimals}";
		var text = $"{ButtonText} {_bottomPrice.ToString(format)}";

		DrawText(_area, _rightTime.Value, _bottomPrice, text);
	}

	private string FormatPrice(decimal price)
	{
		var decimals = Security?.Decimals ?? 2;
		return price.ToString($"F{decimals}");
	}
}
