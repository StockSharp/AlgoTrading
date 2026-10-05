using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Autonomous 5-minute robot strategy.
/// Buy volume is the volume of up candles and sell volume the volume of down candles over the last VolumeLength candles. A close
/// above the SMA and above the close 6 candles ago with buy volume above sell volume goes long; a close below the SMA and below
/// the close 6 candles ago with sell volume above buy volume goes short, reversing an opposite position. Percent stop-loss and
/// take-profit protect every position.
/// </summary>
public class Autonomous5MinuteRobotStrategy : Strategy
{
	private const int _momentumBars = 6;

	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _volumeLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = [];
	private readonly List<(decimal buy, decimal sell)> _volumes = [];

	/// <summary>
	/// SMA period.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Candles over which buy and sell volume are summed.
	/// </summary>
	public int VolumeLength
	{
		get => _volumeLength.Value;
		set => _volumeLength.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take-profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
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
	public Autonomous5MinuteRobotStrategy()
	{
		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "SMA period", "Trend");

		_volumeLength = Param(nameof(VolumeLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Volume Length", "Candles over which buy and sell volume are summed", "Volume");

		_stopLossPercent = Param(nameof(StopLossPercent), 3m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 29m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take-profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_closes.Clear();
		_volumes.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_closes.Clear();
		_volumes.Clear();

		var sma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		StartProtection(
			TakeProfitPercent > 0m ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit(),
			StopLossPercent > 0m ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var volume = candle.TotalVolume;

		_closes.Add(close);
		if (_closes.Count > _momentumBars + 1)
			_closes.RemoveAt(0);

		_volumes.Add((close > candle.OpenPrice ? volume : 0m, close < candle.OpenPrice ? volume : 0m));
		if (_volumes.Count > VolumeLength)
			_volumes.RemoveAt(0);

		if (_closes.Count <= _momentumBars || _volumes.Count < VolumeLength)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var buyVolume = 0m;
		var sellVolume = 0m;

		foreach (var (buy, sell) in _volumes)
		{
			buyVolume += buy;
			sellVolume += sell;
		}

		var pastClose = _closes[0];

		if (close > sma && close > pastClose && buyVolume > sellVolume && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < sma && close < pastClose && sellVolume > buyVolume && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
