using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manages trailing profit, take profit, stop loss and partial closing.
/// Opens a long position on the first finished candle and then manages it.
/// </summary>
public class ManagerTrailingStrategy : Strategy
{
	private readonly StrategyParam<bool> _trailProfitOn;
	private readonly StrategyParam<decimal> _trailStartPercent;
	private readonly StrategyParam<decimal> _trailStepPercent;
	private readonly StrategyParam<bool> _takeProfitOn;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _stopLossOn;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _partCloseOn;
	private readonly StrategyParam<string> _partCloseLevels;
	private readonly StrategyParam<string> _partClosePercents;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _maxProfit;
	private decimal _trailTrigger;
	private decimal[] _levels = Array.Empty<decimal>();
	private decimal[] _percents = Array.Empty<decimal>();
	private int _partIndex;

	public bool TrailProfitOn { get => _trailProfitOn.Value; set => _trailProfitOn.Value = value; }
	public decimal TrailStartPercent { get => _trailStartPercent.Value; set => _trailStartPercent.Value = value; }
	public decimal TrailStepPercent { get => _trailStepPercent.Value; set => _trailStepPercent.Value = value; }
	public bool TakeProfitOn { get => _takeProfitOn.Value; set => _takeProfitOn.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public bool StopLossOn { get => _stopLossOn.Value; set => _stopLossOn.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public bool PartCloseOn { get => _partCloseOn.Value; set => _partCloseOn.Value = value; }
	public string PartCloseLevels { get => _partCloseLevels.Value; set => _partCloseLevels.Value = value; }
	public string PartClosePercents { get => _partClosePercents.Value; set => _partClosePercents.Value = value; }
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ManagerTrailingStrategy()
	{
		_trailProfitOn = Param(nameof(TrailProfitOn), false)
			.SetDisplay("Trail Profit", "Enable profit trailing", "General");

		_trailStartPercent = Param(nameof(TrailStartPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Start %", "Start trailing after this profit", "Trailing");

		_trailStepPercent = Param(nameof(TrailStepPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Step %", "Distance between trail updates", "Trailing");

		_takeProfitOn = Param(nameof(TakeProfitOn), false)
			.SetDisplay("Take Profit", "Enable take profit", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Profit % to close position", "Risk");

		_stopLossOn = Param(nameof(StopLossOn), false)
			.SetDisplay("Stop Loss", "Enable stop loss", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Loss % to close position", "Risk");

		_partCloseOn = Param(nameof(PartCloseOn), true)
			.SetDisplay("Partial Close", "Enable partial closing", "Risk");

		_partCloseLevels = Param(nameof(PartCloseLevels), "20/50/200")
			.SetDisplay("Part Levels", "Profit % levels for partial close", "Risk");

		_partClosePercents = Param(nameof(PartClosePercents), "50/25/25")
			.SetDisplay("Part Percents", "Portions of position to close", "Risk");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to analyze", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		ParsePartClose();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ParsePartClose()
	{
		var levelParts = PartCloseLevels.Split('/');
		_levels = new decimal[levelParts.Length];
		for (var i = 0; i < levelParts.Length; i++)
			_levels[i] = decimal.Parse(levelParts[i], CultureInfo.InvariantCulture);

		var percParts = PartClosePercents.Split('/');
		_percents = new decimal[percParts.Length];
		for (var i = 0; i < percParts.Length; i++)
			_percents[i] = decimal.Parse(percParts[i], CultureInfo.InvariantCulture);

		_partIndex = 0;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position == 0)
		{
			BuyMarket(Volume);
			// enter initial long position
			_entryPrice = candle.ClosePrice;
			_maxProfit = 0m;
			_trailTrigger = decimal.MinValue;
			return;
		}

		var profit = Position > 0
			// calculate current profit in percent based on position direction
			? (candle.ClosePrice - _entryPrice) / _entryPrice * 100m
			: (_entryPrice - candle.ClosePrice) / _entryPrice * 100m;

		if (profit > _maxProfit)
			// update maximum observed profit
			_maxProfit = profit;

		if (TakeProfitOn && profit >= TakeProfitPercent)
			// close on take profit
		{
			ClosePosition();
			return;
		}

		if (StopLossOn && profit <= -StopLossPercent)
			// close on stop loss
		{
			ClosePosition();
			return;
		}

		if (PartCloseOn && _partIndex < _levels.Length && profit >= _levels[_partIndex])
		{
			var part = _percents[_partIndex]
			// volume to close at this level
			/ 100m * Math.Abs(Position);
			if (Position > 0)
				SellMarket(part);
			else
				BuyMarket(part);
			_partIndex++;
		}

		if (TrailProfitOn && profit >= TrailStartPercent)
		{
			var trigger = _maxProfit - TrailStepPercent;
			// trailing trigger follows max profit
			if (trigger > _trailTrigger)
				_trailTrigger = trigger;

			if (profit <= _trailTrigger)
				ClosePosition();
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}
}
