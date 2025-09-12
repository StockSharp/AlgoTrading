using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 15-minute MACD and volume oscillator strategy for XAUUSD.
/// </summary>
public class MacdVolumeXauusdStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _shortVolumeEma;
	private ExponentialMovingAverage _longVolumeEma;
	private MovingAverageConvergenceDivergence _macd;

	private decimal _prevMacd;
	private decimal _prevVolume;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Short volume EMA length.
	/// </summary>
	public int ShortLength { get => _shortLength.Value; set => _shortLength.Value = value; }

	/// <summary>
	/// Long volume EMA length.
	/// </summary>
	public int LongLength { get => _longLength.Value; set => _longLength.Value = value; }

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

	/// <summary>
	/// Position leverage.
	/// </summary>
	public decimal Leverage { get => _leverage.Value; set => _leverage.Value = value; }

	/// <summary>
	/// Stop-loss distance.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit multiplier.
	/// </summary>
	public decimal TakeProfitMultiplier { get => _takeProfitMultiplier.Value; set => _takeProfitMultiplier.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public MacdVolumeXauusdStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 5)
			.SetDisplay("Short Length", "Volume EMA short length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_longLength = Param(nameof(LongLength), 8)
			.SetDisplay("Long Length", "Volume EMA long length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_fastLength = Param(nameof(FastLength), 16)
			.SetDisplay("Fast Length", "MACD fast period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 1);

		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("Slow Length", "MACD slow period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 1);

		_signalLength = Param(nameof(SignalLength), 9)
			.SetDisplay("Signal Length", "MACD signal period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_leverage = Param(nameof(Leverage), 1m)
			.SetDisplay("Leverage", "Position leverage", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_stopLoss = Param(nameof(StopLoss), 10100m)
			.SetDisplay("Stop Loss", "Price distance for stop-loss", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1000m, 20000m, 1000m);

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 1.1m)
			.SetDisplay("Take Profit Multiplier", "Take profit = StopLoss * multiplier", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_shortVolumeEma?.Reset();
		_longVolumeEma?.Reset();
		_macd?.Reset();
		_prevMacd = 0;
		_prevVolume = 0;
		_entryPrice = 0;
		_stopPrice = 0;
		_takePrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_shortVolumeEma = new ExponentialMovingAverage { Length = ShortLength };
		_longVolumeEma = new ExponentialMovingAverage { Length = LongLength };
		_macd = new MovingAverageConvergenceDivergence
		{
			Fast = FastLength,
			Slow = SlowLength,
			Signal = SignalLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_macd, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macd, decimal signal, decimal hist)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var shortVol = _shortVolumeEma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
		var longVol = _longVolumeEma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

		if (longVol == 0)
			return;

		var osc = 100m * (shortVol - longVol) / longVol;
		var vol = candle.TotalVolume;

		var longSignal = _prevMacd <= 0 && macd > 0 && osc > 0m && vol > _prevVolume / 2m;
		var shortSignal = _prevMacd >= 0 && macd < 0 && osc > 0m && vol < _prevVolume / 2m;

		var qty = (Portfolio?.CurrentValue * Leverage / candle.ClosePrice) ?? 0m;
		if (qty <= 0)
			qty = Volume;

		if (longSignal && Position <= 0)
		{
			BuyMarket(qty);
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice - StopLoss;
			_takePrice = _entryPrice + StopLoss * TakeProfitMultiplier;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(qty);
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice + StopLoss;
			_takePrice = _entryPrice - StopLoss * TakeProfitMultiplier;
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				BuyMarket(Math.Abs(Position));
		}

		_prevMacd = macd;
		_prevVolume = vol;
	}
}
