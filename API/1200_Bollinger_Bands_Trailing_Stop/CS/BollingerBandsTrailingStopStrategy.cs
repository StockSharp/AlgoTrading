using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy entering on Bollinger Bands breakout with ATR-based trailing stop.
/// </summary>
public class BollingerBandsTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _highestPrice;
	private decimal _trailOffset;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BbDeviation { get => _bbDeviation.Value; set => _bbDeviation.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier for trailing stop.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Moving average type for Bollinger basis.
	/// </summary>
	public MovingAverageTypeEnum MaType { get => _maType.Value; set => _maType.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="BollingerBandsTrailingStopStrategy"/>.
	/// </summary>
	public BollingerBandsTrailingStopStrategy()
	{
		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands length", "Indicators")
			.SetCanOptimize(true);

		_bbDeviation = Param(nameof(BbDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Deviation", "Standard deviation multiplier", "Indicators")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation period", "Risk")
			.SetCanOptimize(true);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR trailing stop multiplier", "Risk")
			.SetCanOptimize(true);

		_maType = Param(nameof(MaType), MovingAverageTypeEnum.Simple)
			.SetDisplay("MA Type", "Moving average for Bollinger basis", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for strategy", "General");
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
		_highestPrice = 0;
		_trailOffset = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ma = CreateMa(MaType, BbLength);
		var bb = new BollingerBands { Length = BbLength, Width = BbDeviation, MovingAverage = ma };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bb = (BollingerBandsValue)bbValue;

		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var atr = atrValue.ToDecimal();

		if (Position > 0)
		{
			if (candle.HighPrice > _highestPrice)
				_highestPrice = candle.HighPrice;

			var stop = _highestPrice - _trailOffset;
			if (candle.LowPrice <= stop || candle.ClosePrice < lower)
			{
				SellMarket(Position);
				_highestPrice = 0;
				_trailOffset = 0;
			}
			return;
		}

		if (candle.ClosePrice > upper && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_trailOffset = atr * AtrMultiplier;
			_highestPrice = candle.ClosePrice;
		}
	}

	private static MovingAverage CreateMa(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
			MovingAverageTypeEnum.VolumeWeighted => new VolumeWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MovingAverageTypeEnum
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Weighted,

		/// <summary>
		/// Volume weighted moving average.
		/// </summary>
		VolumeWeighted
	}
}
