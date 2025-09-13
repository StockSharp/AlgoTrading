namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Grid strategy that opens trades when price crosses dynamic levels.
/// </summary>
public class CollectorV10Strategy : Strategy
{
	private readonly StrategyParam<decimal> _distance;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _volumeStep;
	private readonly StrategyParam<int> _increaseTrade;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _profitClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _buyLevel;
	private decimal _sellLevel;
	private int _tradeCount;
	private decimal _currentVolume;

	public decimal Distance
	{
		get => _distance.Value;
		set => _distance.Value = value;
	}

	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	public decimal VolumeStep
	{
		get => _volumeStep.Value;
		set => _volumeStep.Value = value;
	}

	public int IncreaseTrade
	{
		get => _increaseTrade.Value;
		set => _increaseTrade.Value = value;
	}

	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	public decimal ProfitClose
	{
		get => _profitClose.Value;
		set => _profitClose.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public CollectorV10Strategy()
	{
		_distance = Param<decimal>(nameof(Distance), 10m);
		_initialVolume = Param<decimal>(nameof(InitialVolume), 0.01m);
		_volumeStep = Param<decimal>(nameof(VolumeStep), 0.01m);
		_increaseTrade = Param<int>(nameof(IncreaseTrade), 3);
		_maxTrades = Param<int>(nameof(MaxTrades), 200);
		_profitClose = Param<decimal>(nameof(ProfitClose), 500000m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentVolume = InitialVolume;
		StartProtection();

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

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		if (_buyLevel == 0m && _sellLevel == 0m)
		{
			SetLevels(price);
			return;
		}

		if (PnL >= ProfitClose)
		{
			ClosePosition();
			Stop();
			return;
		}

		if (_tradeCount < MaxTrades && price >= _buyLevel)
		{
			BuyMarket(_currentVolume);
			_tradeCount++;
			if (_tradeCount % IncreaseTrade == 0)
				_currentVolume += VolumeStep;
			SetLevels(price);
			return;
		}

		if (_tradeCount < MaxTrades && price <= _sellLevel)
		{
			SellMarket(_currentVolume);
			_tradeCount++;
			if (_tradeCount % IncreaseTrade == 0)
				_currentVolume += VolumeStep;
			SetLevels(price);
		}
	}

	private void SetLevels(decimal price)
	{
		var half = Distance / 2m;
		_buyLevel = price + half;
		_sellLevel = price - half;
	}
}
