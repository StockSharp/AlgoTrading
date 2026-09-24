using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedged martingale grid with symmetric initial triggers and basket profit close.
/// </summary>
public class MartiniMartingaleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _step;
	private readonly StrategyParam<decimal> _profitClose;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<Leg> _legs = [];
	private decimal? _buyTrigger;
	private decimal? _sellTrigger;
	private decimal _lastExecutionPrice;
	private decimal _lastLegVolume;
	private int _orderCount;

	public decimal Step { get => _step.Value; set => _step.Value = value; }
	public decimal ProfitClose { get => _profitClose.Value; set => _profitClose.Value = value; }
	public decimal InitialVolume { get => _initialVolume.Value; set => _initialVolume.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MartiniMartingaleStrategy()
	{
		_step = Param(nameof(Step), 10m).SetGreaterThanZero()
			.SetDisplay("Step", "Absolute price step between martingale actions.", "Grid");
		_profitClose = Param(nameof(ProfitClose), 10m).SetGreaterThanZero()
			.SetDisplay("Profit Close", "Combined floating PnL required to flatten the basket.", "Risk");
		_initialVolume = Param(nameof(InitialVolume), 0.1m).SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Starting volume for the first triggered order.", "Trading");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used to monitor trigger levels.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetCycle();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetCycle();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (_legs.Count > 0 && CalculateFloatingPnL(close) >= ProfitClose)
		{
			Flatten();
			ResetCycle();
			return;
		}

		if (_legs.Count == 0)
		{
			if (_buyTrigger is null || _sellTrigger is null)
			{
				_buyTrigger = close + Step;
				_sellTrigger = close - Step;
				return;
			}

			var buyHit = candle.HighPrice >= _buyTrigger.Value;
			var sellHit = candle.LowPrice <= _sellTrigger.Value;

			if (!buyHit && !sellHit)
				return;

			// With bar data the path is unknown when both stops were crossed. Resolve
			// deterministically from candle direction and still honour OCO semantics.
			var side = buyHit && sellHit
				? (candle.ClosePrice >= candle.OpenPrice ? Sides.Buy : Sides.Sell)
				: buyHit ? Sides.Buy : Sides.Sell;

			ExecuteLeg(side, InitialVolume, side == Sides.Buy ? _buyTrigger.Value : _sellTrigger.Value);
			_buyTrigger = null;
			_sellTrigger = null;
			return;
		}

		var adverseDistance = Step * _orderCount;

		if (Position > 0 && candle.LowPrice <= _lastExecutionPrice - adverseDistance)
			ExecuteLeg(Sides.Sell, _lastLegVolume * 2m, _lastExecutionPrice - adverseDistance);
		else if (Position < 0 && candle.HighPrice >= _lastExecutionPrice + adverseDistance)
			ExecuteLeg(Sides.Buy, _lastLegVolume * 2m, _lastExecutionPrice + adverseDistance);
	}

	private void ExecuteLeg(Sides side, decimal volume, decimal price)
	{
		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_legs.Add(new Leg(side, volume, price));
		_lastExecutionPrice = price;
		_lastLegVolume = volume;
		_orderCount++;
	}

	private decimal CalculateFloatingPnL(decimal marketPrice)
	{
		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;
		var multiplier = Security?.Multiplier ?? 1m;
		var total = 0m;

		foreach (var leg in _legs)
		{
			var direction = leg.Side == Sides.Buy ? 1m : -1m;
			var difference = (marketPrice - leg.Price) * direction;

			total += priceStep > 0m && stepPrice > 0m
				? difference / priceStep * stepPrice * leg.Volume
				: difference * multiplier * leg.Volume;
		}

		return total;
	}

	private void Flatten()
	{
		if (Position > 0)
			SellMarket(Math.Abs(Position));
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));
	}

	private void ResetCycle()
	{
		_legs.Clear();
		_buyTrigger = null;
		_sellTrigger = null;
		_lastExecutionPrice = 0m;
		_lastLegVolume = 0m;
		_orderCount = 0;
	}

	private readonly record struct Leg(Sides Side, decimal Volume, decimal Price);
}
