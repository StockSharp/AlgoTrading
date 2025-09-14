namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on the Corrected Average breakout.
/// The strategy monitors price relative to a corrected moving average.
/// A signal occurs when price breaks above/below the moving average by a certain level
/// and then returns to the breakout level.
/// </summary>
public class CorrectedAverageBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<MaType> _maType;
	private readonly StrategyParam<int> _levelPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;

	private decimal _prevCorrected;
	private decimal _prevPrevCorrected;
	private decimal _prevClose;
	private decimal _prevPrevClose;
	private bool _isInitialized;
	private decimal _level;
	private decimal _stopLoss;
	private decimal _takeProfit;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Length of moving average and standard deviation.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MaType MaTypeOption { get => _maType.Value; set => _maType.Value = value; }

	/// <summary>
	/// Breakout level in price steps.
	/// </summary>
	public int LevelPoints { get => _levelPoints.Value; set => _levelPoints.Value = value; }

	/// <summary>
	/// Stop-loss distance in points.
	/// </summary>
	public int StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Take-profit distance in points.
	/// </summary>
	public int TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public CorrectedAverageBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator calculations", "General");

		_length = Param(nameof(Length), 12)
			.SetRange(2, 200)
			.SetDisplay("Length", "Period of moving average and standard deviation", "Indicator");

		_maType = Param(nameof(MaTypeOption), MaType.Sma)
			.SetDisplay("MA Type", "Type of moving average used", "Indicator");

		_levelPoints = Param(nameof(LevelPoints), 300)
			.SetRange(10, 5000)
			.SetDisplay("Level Points", "Breakout distance from corrected average in price steps", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetRange(0, 10000)
			.SetDisplay("Stop Loss Points", "Stop-loss distance from entry price in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetRange(0, 10000)
			.SetDisplay("Take Profit Points", "Take-profit distance from entry price in price steps", "Risk");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow opening long positions", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow opening short positions", "Trading");
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
		_prevCorrected = default;
		_prevPrevCorrected = default;
		_prevClose = default;
		_prevPrevClose = default;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_level = LevelPoints * Security.PriceStep;
		_stopLoss = StopLossPoints * Security.PriceStep;
		_takeProfit = TakeProfitPoints * Security.PriceStep;

		var ma = CreateMa(MaTypeOption, Length);
		var std = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ma, std, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(_takeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(_stopLoss, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		decimal corrected;

		if (!_isInitialized)
		{
			corrected = maValue;
			_isInitialized = true;
		}
		else
		{
			var v1 = stdValue * stdValue;
			var v2 = (_prevCorrected - maValue) * (_prevCorrected - maValue);
			var k = (v2 < v1 || v2 == 0m) ? 0m : 1m - (v1 / v2);
			corrected = _prevCorrected + k * (maValue - _prevCorrected);
		}

		var buySignal = _prevPrevClose > _prevPrevCorrected + _level && _prevClose <= _prevCorrected + _level;
		var sellSignal = _prevPrevClose < _prevPrevCorrected - _level && _prevClose >= _prevCorrected - _level;

		if (buySignal && EnableLong && Position <= 0)
		{
			if (Position < 0)
				ClosePosition();

			BuyMarket();
		}
		else if (sellSignal && EnableShort && Position >= 0)
		{
			if (Position > 0)
				ClosePosition();

			SellMarket();
		}

		_prevPrevCorrected = _prevCorrected;
		_prevPrevClose = _prevClose;
		_prevCorrected = corrected;
		_prevClose = candle.ClosePrice;
	}

	private static LengthIndicator<decimal> CreateMa(MaType type, int length)
	{
		return type switch
		{
			MaType.Sma => new SimpleMovingAverage { Length = length },
			MaType.Ema => new ExponentialMovingAverage { Length = length },
			MaType.Smma => new SmoothedMovingAverage { Length = length },
			MaType.Lwma => new WeightedMovingAverage { Length = length },
			_ => throw new ArgumentOutOfRangeException(nameof(type))
		};
	}

	/// <summary>
	/// Supported moving average types.
	/// </summary>
	public enum MaType
	{
		Sma,
		Ema,
		Smma,
		Lwma
	}
}
