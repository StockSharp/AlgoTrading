using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses Laguerre-filtered DI+ and DI- to trade against sudden directional shifts.
/// </summary>
public class LaguerreAdxStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _gamma;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevUp;
	private decimal _prevDown;
	private decimal _l0Up;
	private decimal _l1Up;
	private decimal _l2Up;
	private decimal _l3Up;
	private decimal _l0Down;
	private decimal _l1Down;
	private decimal _l2Down;
	private decimal _l3Down;
	private bool _isInitialized;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Laguerre smoothing factor (0-1).
	/// </summary>
	public decimal Gamma
	{
		get => _gamma.Value;
		set => _gamma.Value = value;
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
	/// Initializes a new instance of the <see cref="LaguerreAdxStrategy"/> class.
	/// </summary>
	public LaguerreAdxStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 1);

		_gamma = Param(nameof(Gamma), 0.764m)
			.SetDisplay("Gamma", "Laguerre smoothing factor", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 0.9m, 0.02m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		_prevUp = default;
		_prevDown = default;
		_l0Up = _l1Up = _l2Up = _l3Up = default;
		_l0Down = _l1Down = _l2Down = _l3Down = default;
		_isInitialized = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, ProcessCandle)
			.Start();

		StartProtection();
	}

	// Calculates Laguerre RSI value for a single data point.
	private decimal LaguerreRsi(decimal value, ref decimal l0, ref decimal l1, ref decimal l2, ref decimal l3)
	{
		var l0Prev = l0;
		var l1Prev = l1;
		var l2Prev = l2;
		var l3Prev = l3;

		l0 = (1m - Gamma) * value + Gamma * l0Prev;
		l1 = -Gamma * l0 + l0Prev + Gamma * l1Prev;
		l2 = -Gamma * l1 + l1Prev + Gamma * l2Prev;
		l3 = -Gamma * l2 + l2Prev + Gamma * l3Prev;

		decimal cu = 0m;
		decimal cd = 0m;

		if (l0 >= l1)
			cu = l0 - l1;
		else
			cd = l1 - l0;

		if (l1 >= l2)
			cu += l1 - l2;
		else
			cd += l2 - l1;

		if (l2 >= l3)
			cu += l2 - l3;
		else
			cd += l3 - l2;

		return cu + cd == 0m ? 0m : cu / (cu + cd);
	}

	// Processes each finished candle and executes trading logic.
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adx = (AverageDirectionalIndexValue)adxValue;
		var plus = adx.Dx.Plus;
		var minus = adx.Dx.Minus;

		var up = LaguerreRsi(plus, ref _l0Up, ref _l1Up, ref _l2Up, ref _l3Up);
		var down = LaguerreRsi(minus, ref _l0Down, ref _l1Down, ref _l2Down, ref _l3Down);

		if (!_isInitialized)
		{
			_prevUp = up;
			_prevDown = down;
			_isInitialized = true;
			return;
		}

		// Close positions based on current dominance of DI lines.
		if (up > down && Position < 0)
			BuyMarket(Math.Abs(Position));
		else if (down > up && Position > 0)
			SellMarket(Math.Abs(Position));

		// Contrarian entry signals on crossovers.
		if (_prevUp > _prevDown && up < down && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (_prevUp < _prevDown && up > down && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prevUp = up;
		_prevDown = down;
	}
}
