using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum MaType
{
	SMA,
	EMA,
	HMA,
}

public class SeparatedMovingAverageStrategy : Strategy
{
	private readonly StrategyParam<MaType> _maType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<bool> _useHeikinAshi;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage _maUp = null!;
	private MovingAverage _maDown = null!;
	private decimal _upValue;
	private decimal _downValue;
	private decimal _prevHaOpen;
	private decimal _prevHaClose;

	public SeparatedMovingAverageStrategy()
	{
		_maType = Param(nameof(MaType), MaType.SMA)
			.SetDisplay("Type", "Moving average type", "General");
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Average period", "General");
		_useHeikinAshi = Param(nameof(UseHeikinAshi), true)
			.SetDisplay("Heikin Ashi", "Use Heikin Ashi prices", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public MaType MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public bool UseHeikinAshi
	{
		get => _useHeikinAshi.Value;
		set => _useHeikinAshi.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		_maUp = CreateMa();
		_maDown = CreateMa();
		_upValue = 0m;
		_downValue = 0m;
		_prevHaOpen = 0m;
		_prevHaClose = 0m;

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private MovingAverage CreateMa()
		=> MaType switch
		{
			MaType.EMA => new ExponentialMovingAverage { Length = Length },
			MaType.HMA => new HullMovingAverage { Length = Length },
			_ => new SMA { Length = Length },
		};

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		if (UseHeikinAshi)
		{
			var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
			var haOpen = _prevHaOpen == 0m
				? (candle.OpenPrice + candle.ClosePrice) / 2m
				: (_prevHaOpen + _prevHaClose) / 2m;
			open = haOpen;
			close = haClose;
			_prevHaOpen = haOpen;
			_prevHaClose = haClose;
		}

		if (close > open)
			_upValue = close;
		if (close < open)
			_downValue = close;

		var maUp = _maUp.Process(_upValue, candle.OpenTime, true).ToDecimal();
		var maDown = _maDown.Process(_downValue, candle.OpenTime, true).ToDecimal();

		if (!_maUp.IsFormed || !_maDown.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (maUp > maDown && Position <= 0)
			BuyMarket();
		else if (maUp < maDown && Position >= 0)
			SellMarket();
	}
}

