using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class KarakaticaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _risk;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _optimizationDepth;
	private readonly StrategyParam<int> _reoptimizeEvery;
	private readonly StrategyParam<int> _optimizationStart;
	private readonly StrategyParam<int> _optimizationStep;
	private readonly StrategyParam<int> _optimizationEnd;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<CandleInfo> _history = new();

	private int _barsUntilOptimization;
	private int _lastOrderDirection;
	private bool _blockAllEntries;
	private bool _blockBuyEntries;
	private bool _blockSellEntries;
	private bool _needsOptimization = true;

	private readonly record struct CandleInfo(DateTimeOffset Time, decimal Open, decimal High, decimal Low, decimal Close);

	public KarakaticaStrategy()
	{
		_risk = Param(nameof(Risk), 0.5m)
		.SetCanOptimize(true)
		.SetDisplay("Risk percent (per 1000 balance units)");

		_stopLossPoints = Param(nameof(StopLossPoints), 50)
		.SetCanOptimize(true)
		.SetDisplay("Stop-loss in points");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150)
		.SetCanOptimize(true)
		.SetDisplay("Take-profit in points");

		_period = Param(nameof(Period), 70)
		.SetCanOptimize(true)
		.SetDisplay("Signal period");

		_optimizationDepth = Param(nameof(OptimizationDepth), 250)
		.SetDisplay("Bars used for optimization");

		_reoptimizeEvery = Param(nameof(ReoptimizeEvery), 50)
		.SetDisplay("Recalculate parameters every N bars");

		_optimizationStart = Param(nameof(OptimizationStart), 10)
		.SetDisplay("Optimization start period");

		_optimizationStep = Param(nameof(OptimizationStep), 5)
		.SetDisplay("Optimization step");

		_optimizationEnd = Param(nameof(OptimizationEnd), 150)
		.SetDisplay("Optimization end period");

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
		.SetDisplay("Primary candle type");
	}

	public decimal Risk
	{
		get => _risk.Value;
		set => _risk.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	public int OptimizationDepth
	{
		get => _optimizationDepth.Value;
		set => _optimizationDepth.Value = value;
	}

	public int ReoptimizeEvery
	{
		get => _reoptimizeEvery.Value;
		set => _reoptimizeEvery.Value = value;
	}

	public int OptimizationStart
	{
		get => _optimizationStart.Value;
		set => _optimizationStart.Value = value;
	}

	public int OptimizationStep
	{
		get => _optimizationStep.Value;
		set => _optimizationStep.Value = value;
	}

	public int OptimizationEnd
	{
		get => _optimizationEnd.Value;
		set => _optimizationEnd.Value = value;
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

		var priceStep = GetPriceStep();

		StartProtection(
		stopLoss: StopLossPoints > 0 ? new Unit(StopLossPoints * priceStep, UnitTypes.Price) : null,
		takeProfit: TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * priceStep, UnitTypes.Price) : null);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_history.Add(new CandleInfo(candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice));

		var maxHistory = OptimizationDepth + OptimizationEnd + 16;
		if (_history.Count > maxHistory)
		_history.RemoveRange(0, _history.Count - maxHistory);

		if (_barsUntilOptimization > 0)
		_barsUntilOptimization--;

		if ((_barsUntilOptimization <= 0 || _needsOptimization) && HasEnoughHistoryForOptimization())
		{
			RunOptimization();
			_barsUntilOptimization = ReoptimizeEvery;
			_needsOptimization = false;
		}
		else if (!HasEnoughHistoryForOptimization())
		{
			_needsOptimization = true;
		}

		if (_history.Count < Period + 2)
		return;

		if (!TryGetSignals(Period, 1, out var buySignal, out var sellSignal))
		return;

		if (Position > 0 && sellSignal)
		{
			SellMarket(Position);
			_lastOrderDirection = 2;
			return;
		}

		if (Position < 0 && buySignal)
		{
			BuyMarket(Math.Abs(Position));
			_lastOrderDirection = 1;
			return;
		}

		if (Position != 0 || _blockAllEntries)
		return;

		var volume = GetOrderVolume();
		if (volume <= 0)
		return;

		if (!_blockBuyEntries && buySignal && _lastOrderDirection != 1)
		{
			BuyMarket(volume);
			_lastOrderDirection = 1;
			return;
		}

		if (!_blockSellEntries && sellSignal && _lastOrderDirection != 2)
		{
			SellMarket(volume);
			_lastOrderDirection = 2;
		}
	}

	private void RunOptimization()
	{
		if (!HasEnoughHistoryForOptimization())
		return;

		var spread = GetSpreadEstimate();
		var bestCombined = decimal.MinValue;
		var bestCombinedPeriod = Period;
		var bestBuy = decimal.MinValue;
		var bestBuyPeriod = Period;
		var bestSell = decimal.MinValue;
		var bestSellPeriod = Period;

		var start = Math.Max(1, OptimizationStart);
		var end = Math.Max(start, OptimizationEnd);
		var step = Math.Max(1, OptimizationStep);

		for (var p = start; p <= end; p += step)
		{
			var (profit, buyProfit, sellProfit) = SimulatePeriod(p, spread);

			if (profit > bestCombined)
			{
				bestCombined = profit;
				bestCombinedPeriod = p;
			}

			if (buyProfit > bestBuy)
			{
				bestBuy = buyProfit;
				bestBuyPeriod = p;
			}

			if (sellProfit > bestSell)
			{
				bestSell = sellProfit;
				bestSellPeriod = p;
			}
		}

		_blockAllEntries = false;
		_blockBuyEntries = false;
		_blockSellEntries = false;

		if (bestBuy < 0 && bestSell < 0)
		{
			_blockAllEntries = true;
			_blockBuyEntries = true;
			_blockSellEntries = true;
			Period = bestCombinedPeriod;
			return;
		}

		if (bestBuy == bestSell)
		{
			Period = bestCombinedPeriod;
			return;
		}

		if (bestBuy > bestSell)
		{
			_blockSellEntries = true;
			Period = bestBuyPeriod;
		}
		else
		{
			_blockBuyEntries = true;
			Period = bestSellPeriod;
		}
	}

	private (decimal profit, decimal buyProfit, decimal sellProfit) SimulatePeriod(int period, decimal spread)
	{
		decimal profit = 0m;
		decimal buyProfit = 0m;
		decimal sellProfit = 0m;
		var orderType = 0;
		decimal entryPrice = 0m;

		for (var offset = OptimizationDepth; offset >= 0; offset--)
		{
			if (!TryGetSignals(period, offset + 1, out var buySignal, out var sellSignal))
			continue;

			var candle = GetCandle(offset);

			if (orderType == 1 && sellSignal)
			{
				var result = candle.Open - entryPrice - spread;
				buyProfit += result;
				profit += result;
				orderType = 0;
			}
			else if (orderType == 2 && buySignal)
			{
				var result = entryPrice - candle.Open - spread;
				sellProfit += result;
				profit += result;
				orderType = 0;
			}

			if (orderType != 0)
			continue;

			if (buySignal)
			{
				orderType = 1;
				entryPrice = candle.Open;
			}
			else if (sellSignal)
			{
				orderType = 2;
				entryPrice = candle.Open;
			}
		}

		if (orderType == 1)
		{
			var closeCandle = GetCandle(0);
			var result = closeCandle.Open - entryPrice - spread;
			buyProfit += result;
			profit += result;
		}
		else if (orderType == 2)
		{
			var closeCandle = GetCandle(0);
			var result = entryPrice - closeCandle.Open - spread;
			sellProfit += result;
			profit += result;
		}

		return (profit, buyProfit, sellProfit);
	}

	private bool TryGetSignals(int period, int shift, out bool buySignal, out bool sellSignal)
	{
		buySignal = false;
		sellSignal = false;

		if (period <= 0)
		return false;

		var count = _history.Count;
		var index = count - 1 - shift;
		if (index <= 0)
		return false;

		if (index - period + 1 < 0)
		return false;

		var currentClose = _history[index].Close;
		var previousClose = _history[index - 1].Close;
		var currentSma = CalculateSma(period, index);
		var previousSma = CalculateSma(period, index - 1);

		buySignal = previousClose <= previousSma && currentClose > currentSma;
		sellSignal = previousClose >= previousSma && currentClose < currentSma;

		return buySignal || sellSignal;
	}

	private decimal CalculateSma(int period, int index)
	{
		decimal sum = 0m;
		for (var i = 0; i < period; i++)
		{
			var idx = index - i;
			sum += _history[idx].Close;
		}

		return sum / period;
	}

	private CandleInfo GetCandle(int shift)
	{
		var index = _history.Count - 1 - shift;
		return _history[index];
	}

	private bool HasEnoughHistoryForOptimization()
	{
		var required = OptimizationDepth + OptimizationEnd + 2;
		return _history.Count >= required;
	}

	private decimal GetSpreadEstimate()
	{
		var bid = Security?.BestBidPrice;
		var ask = Security?.BestAskPrice;

		if (bid is decimal bidPrice && ask is decimal askPrice && askPrice > bidPrice)
		return askPrice - bidPrice;

		return GetPriceStep();
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep;
		return step.HasValue && step.Value > 0 ? step.Value : 0.0001m;
	}

	private decimal GetOrderVolume()
	{
		var security = Security;
		var portfolio = Portfolio;

		if (security is null || portfolio is null)
		return Volume > 0 ? Volume : 0m;

		var balance = portfolio.CurrentValue ?? 0m;
		var minVolume = security.MinVolume ?? 1m;
		var step = security.VolumeStep ?? minVolume;
		var maxVolume = security.MaxVolume ?? 0m;

		if (step <= 0)
		step = minVolume;

		var desired = Risk / 1000m * balance;

		if (desired <= 0)
		return minVolume;

		if (desired < minVolume)
		desired = minVolume;

		var steps = decimal.Floor((desired - minVolume) / step);
		if (steps < 0)
		steps = 0;

		var volume = minVolume + steps * step;

		if (maxVolume > 0 && volume > maxVolume)
		volume = maxVolume;

		return volume;
	}
}
