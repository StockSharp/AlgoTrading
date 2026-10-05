using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy.
/// A short and a long moving average of the selected type are calculated on the selected candle price.
/// A long opens when the short average crosses above the long one and closes when it crosses back below. Long only, no stops.
/// </summary>
public class MovingAverageStrategy : Strategy
{
	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MaTypes
	{
		/// <summary>Simple moving average.</summary>
		SMA,
		/// <summary>Exponential moving average.</summary>
		EMA,
		/// <summary>Double exponential moving average.</summary>
		DEMA,
		/// <summary>Triple exponential moving average.</summary>
		TEMA,
		/// <summary>Weighted moving average.</summary>
		WMA,
		/// <summary>Volume weighted moving average.</summary>
		VWMA,
	}

	/// <summary>
	/// Candle price used by the averages.
	/// </summary>
	public enum PriceTypes
	{
		/// <summary>Close price.</summary>
		Close,
		/// <summary>High price.</summary>
		High,
		/// <summary>Open price.</summary>
		Open,
		/// <summary>Low price.</summary>
		Low,
		/// <summary>(High + Low + Close) / 3.</summary>
		Typical,
		/// <summary>(High + Low) / 2.</summary>
		Center,
	}

	private readonly StrategyParam<MaTypes> _maType;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<PriceTypes> _priceType;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageLine _shortMa;
	private MovingAverageLine _longMa;
	private decimal? _prevShort;
	private decimal? _prevLong;

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MaTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Short moving average length.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Long moving average length.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Candle price used by the averages.
	/// </summary>
	public PriceTypes PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
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
	/// Constructor.
	/// </summary>
	public MovingAverageStrategy()
	{
		_maType = Param(nameof(MaType), MaTypes.EMA)
			.SetDisplay("MA Type", "Moving average type", "Indicators");

		_shortLength = Param(nameof(ShortLength), 1)
			.SetGreaterThanZero()
			.SetDisplay("Short Length", "Short moving average length", "Indicators");

		_longLength = Param(nameof(LongLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Long Length", "Long moving average length", "Indicators");

		_priceType = Param(nameof(PriceType), PriceTypes.Typical)
			.SetDisplay("Price Type", "Candle price used by the averages", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_shortMa = null;
		_longMa = null;
		_prevShort = null;
		_prevLong = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevShort = null;
		_prevLong = null;
		_shortMa = new MovingAverageLine(MaType, ShortLength);
		_longMa = new MovingAverageLine(MaType, LongLength);

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

	private decimal GetPrice(ICandleMessage candle)
	{
		return PriceType switch
		{
			PriceTypes.High => candle.HighPrice,
			PriceTypes.Open => candle.OpenPrice,
			PriceTypes.Low => candle.LowPrice,
			PriceTypes.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			PriceTypes.Center => (candle.HighPrice + candle.LowPrice) / 2m,
			_ => candle.ClosePrice,
		};
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = GetPrice(candle);
		var shortValue = _shortMa.Process(price, candle.TotalVolume, candle.OpenTime);
		var longValue = _longMa.Process(price, candle.TotalVolume, candle.OpenTime);

		if (shortValue is not decimal shortMa || longValue is not decimal longMa)
			return;

		var prevShort = _prevShort;
		var prevLong = _prevLong;
		_prevShort = shortMa;
		_prevLong = longMa;

		if (prevShort is not decimal ps || prevLong is not decimal pl)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (ps <= pl && shortMa > longMa && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (ps >= pl && shortMa < longMa && Position > 0)
			SellMarket(Position);
	}

	/// <summary>
	/// A moving average fed with an arbitrary price; VWMA is calculated from the price and candle volume.
	/// </summary>
	private sealed class MovingAverageLine
	{
		private readonly IIndicator _indicator;
		private readonly int _length;
		private readonly Queue<(decimal price, decimal volume)> _window = new();

		public MovingAverageLine(MaTypes type, int length)
		{
			_length = length;
			_indicator = type switch
			{
				MaTypes.SMA => new SimpleMovingAverage { Length = length },
				MaTypes.DEMA => new DoubleExponentialMovingAverage { Length = length },
				MaTypes.TEMA => new TripleExponentialMovingAverage { Length = length },
				MaTypes.WMA => new WeightedMovingAverage { Length = length },
				MaTypes.VWMA => null,
				_ => new ExponentialMovingAverage { Length = length },
			};
		}

		public decimal? Process(decimal price, decimal volume, DateTime time)
		{
			if (_indicator == null)
			{
				_window.Enqueue((price, volume));
				if (_window.Count > _length)
					_window.Dequeue();
				if (_window.Count < _length)
					return null;

				decimal sumPv = 0m, sumV = 0m;
				foreach (var (p, v) in _window)
				{
					sumPv += p * v;
					sumV += v;
				}

				return sumV == 0m ? price : sumPv / sumV;
			}

			var value = _indicator.Process(new DecimalIndicatorValue(_indicator, price, time) { IsFinal = true });
			if (!_indicator.IsFormed || value.IsEmpty)
				return null;

			return value.ToDecimal();
		}
	}
}
