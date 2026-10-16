using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Wilder RSI on finished candle OPEN prices; market entries on strict threshold crossings.
/// The selected order volume and PriceStep-based protection are applied to every entry.
/// </summary>
public class CmRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _buyLevel;
	private readonly StrategyParam<decimal> _sellLevel;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousOpen;
	private decimal? _previousRsi;
	private decimal _sumGain;
	private decimal _sumLoss;
	private decimal _averageGain;
	private decimal _averageLoss;
	private int _changes;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal BuyLevel { get => _buyLevel.Value; set => _buyLevel.Value = value; }
	public decimal SellLevel { get => _sellLevel.Value; set => _sellLevel.Value = value; }
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CmRsiStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators")
			.SetOptimize(7, 21, 1);
		_buyLevel = Param(nameof(BuyLevel), 30m)
			.SetDisplay("Buy Level", "RSI level to enter long", "Indicators")
			.SetOptimize(10m, 40m, 5m);
		_sellLevel = Param(nameof(SellLevel), 70m)
			.SetDisplay("Sell Level", "RSI level to enter short", "Indicators")
			.SetOptimize(60m, 90m, 5m);
		_takeProfit = Param(nameof(TakeProfit), 200).SetNotNegative()
			.SetDisplay("Take Profit", "Take profit distance in price steps", "Risk Management")
			.SetOptimize(100, 400, 50);
		_stopLoss = Param(nameof(StopLoss), 100).SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk Management")
			.SetOptimize(50, 200, 50);
		_orderVolume = Param(nameof(OrderVolume), 0.1m).SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume of each trade", "General")
			.SetOptimize(0.1m, 1m, 0.1m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetRsi();
	}

	private void ResetRsi()
	{
		_previousOpen = null;
		_previousRsi = null;
		_sumGain = _sumLoss = _averageGain = _averageLoss = 0m;
		_changes = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetRsi();
		if (Security.PriceStep <= 0m)
			throw new InvalidOperationException("Cm RSI requires a positive Security.PriceStep for point-based stops.");
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
		StartProtection(
			takeProfit: TakeProfit > 0 ? new Unit(TakeProfit * Security.PriceStep, UnitTypes.Absolute) : new Unit(),
			stopLoss: StopLoss > 0 ? new Unit(StopLoss * Security.PriceStep, UnitTypes.Absolute) : new Unit());
	}

	private static decimal ToRsi(decimal averageGain, decimal averageLoss)
	{
		if (averageLoss == 0m)
			return averageGain == 0m ? 50m : 100m;
		if (averageGain == 0m)
			return 0m;
		return 100m * averageGain / (averageGain + averageLoss);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var open = candle.OpenPrice;
		if (_previousOpen is not decimal lastOpen)
		{
			_previousOpen = open;
			return;
		}
		_previousOpen = open;
		var change = open - lastOpen;
		var gain = Math.Max(change, 0m);
		var loss = Math.Max(-change, 0m);
		if (_changes < RsiPeriod)
		{
			_sumGain += gain;
			_sumLoss += loss;
			_changes++;
			if (_changes < RsiPeriod)
				return;
			_averageGain = _sumGain / RsiPeriod;
			_averageLoss = _sumLoss / RsiPeriod;
			_previousRsi = ToRsi(_averageGain, _averageLoss);
			return; // First completed RSI is the signal baseline, never an entry.
		}
		_averageGain = (_averageGain * (RsiPeriod - 1) + gain) / RsiPeriod;
		_averageLoss = (_averageLoss * (RsiPeriod - 1) + loss) / RsiPeriod;
		var rsi = ToRsi(_averageGain, _averageLoss);
		if (_previousRsi is decimal previous && IsFormedAndOnlineAndAllowTrading())
		{
			var position = Position;
			if (previous < BuyLevel && rsi > BuyLevel && position <= 0m)
			{
				if (position < 0m)
					BuyMarket(-position);
				BuyMarket(OrderVolume);
			}
			else if (previous > SellLevel && rsi < SellLevel && position >= 0m)
			{
				if (position > 0m)
					SellMarket(position);
				SellMarket(OrderVolume);
			}
		}
		_previousRsi = rsi;
	}
}
