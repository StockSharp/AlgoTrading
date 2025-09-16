using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic Martingale strategy.
/// Uses Stochastic oscillator for signals and martingale averaging.
/// </summary>
public class StochasticMartingaleStrategy : Strategy
{
	private readonly StrategyParam<int> _step;
	private readonly StrategyParam<int> _stepMode;
	private readonly StrategyParam<int> _profitFactor;
	private readonly StrategyParam<decimal> _mult;
	private readonly StrategyParam<decimal> _buyVolume;
	private readonly StrategyParam<decimal> _sellVolume;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _zoneBuy;
	private readonly StrategyParam<decimal> _zoneSell;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastBuyPrice;
	private decimal _lastBuyVolume;
	private decimal _lastSellPrice;
	private decimal _lastSellVolume;
	private int _buyCount;
	private int _sellCount;

	/// <summary>Price step in points for averaging.</summary>
	public int Step { get => _step.Value; set => _step.Value = value; }

	/// <summary>Step mode: 0 - fixed, 1 - multiplied by orders count.</summary>
	public int StepMode { get => _stepMode.Value; set => _stepMode.Value = value; }

	/// <summary>Points for take profit per order.</summary>
	public int ProfitFactor { get => _profitFactor.Value; set => _profitFactor.Value = value; }

	/// <summary>Volume multiplier for averaging.</summary>
	public decimal Mult { get => _mult.Value; set => _mult.Value = value; }

	/// <summary>Initial buy volume.</summary>
	public decimal BuyVolume { get => _buyVolume.Value; set => _buyVolume.Value = value; }

	/// <summary>Initial sell volume.</summary>
	public decimal SellVolume { get => _sellVolume.Value; set => _sellVolume.Value = value; }

	/// <summary>Stochastic %K period.</summary>
	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }

	/// <summary>Stochastic %D period.</summary>
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }

	/// <summary>Oversold level.</summary>
	public decimal ZoneBuy { get => _zoneBuy.Value; set => _zoneBuy.Value = value; }

	/// <summary>Overbought level.</summary>
	public decimal ZoneSell { get => _zoneSell.Value; set => _zoneSell.Value = value; }

	/// <summary>Reverse entry direction.</summary>
	public bool Reverse { get => _reverse.Value; set => _reverse.Value = value; }

	/// <summary>Candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Initialize <see cref="StochasticMartingaleStrategy"/>.</summary>
	public StochasticMartingaleStrategy()
	{
		_step = Param(nameof(Step), 25)
			.SetGreaterThanZero()
			.SetDisplay("Step", "Price step in points for averaging", "Martingale");

		_stepMode = Param(nameof(StepMode), 0)
			.SetDisplay("Step Mode", "0 - fixed step, 1 - step multiplied by orders count", "Martingale");

		_profitFactor = Param(nameof(ProfitFactor), 20)
			.SetGreaterThanZero()
			.SetDisplay("Profit Factor", "Points for take profit per order", "Martingale");

		_mult = Param(nameof(Mult), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Volume multiplier for averaging", "Martingale");

		_buyVolume = Param(nameof(BuyVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Buy Volume", "Initial buy volume", "General");

		_sellVolume = Param(nameof(SellVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Sell Volume", "Initial sell volume", "General");

		_kPeriod = Param(nameof(KPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Stochastic %K period", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Stochastic %D period", "Indicators");

		_zoneBuy = Param(nameof(ZoneBuy), 65m)
			.SetDisplay("Zone Buy", "Oversold level", "Indicators");

		_zoneSell = Param(nameof(ZoneSell), 70m)
			.SetDisplay("Zone Sell", "Overbought level", "Indicators");

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse", "Reverses entry direction", "General");

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
		_lastBuyPrice = 0;
		_lastBuyVolume = 0;
		_lastSellPrice = 0;
		_lastSellVolume = 0;
		_buyCount = 0;
		_sellCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var stochastic = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod },
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal kValue || stoch.D is not decimal dValue)
			return;

		var step = Step * (Security.PriceStep ?? 0m);
		var profit = ProfitFactor * (Security.PriceStep ?? 0m);
		var price = candle.ClosePrice;

		if (_buyCount > 0 && Position > 0)
		{
			if ((StepMode == 0 && price <= _lastBuyPrice - step) ||
				(StepMode == 1 && price <= _lastBuyPrice - step * _buyCount))
			{
				var volume = CheckVolume(_lastBuyVolume * Mult);
				if (volume > 0)
				{
					BuyMarket(volume);
					_lastBuyPrice = price;
					_lastBuyVolume = volume;
					_buyCount++;
				}
			}

			if (price >= _lastBuyPrice + profit * _buyCount)
			{
				SellMarket(Math.Abs(Position));
				_buyCount = 0;
			}
		}
		else if (_sellCount > 0 && Position < 0)
		{
			if ((StepMode == 0 && price >= _lastSellPrice + step) ||
				(StepMode == 1 && price >= _lastSellPrice + step * _sellCount))
			{
				var volume = CheckVolume(_lastSellVolume * Mult);
				if (volume > 0)
				{
					SellMarket(volume);
					_lastSellPrice = price;
					_lastSellVolume = volume;
					_sellCount++;
				}
			}

			if (price <= _lastSellPrice - profit * _sellCount)
			{
				BuyMarket(Math.Abs(Position));
				_sellCount = 0;
			}
		}
		else if (Position == 0)
		{
			if (kValue > dValue && dValue > ZoneBuy)
			{
				if (!Reverse)
				{
					var volume = CheckVolume(BuyVolume);
					if (volume > 0)
					{
						BuyMarket(volume);
						_lastBuyPrice = price;
						_lastBuyVolume = volume;
						_buyCount = 1;
					}
				}
				else
				{
					var volume = CheckVolume(SellVolume);
					if (volume > 0)
					{
						SellMarket(volume);
						_lastSellPrice = price;
						_lastSellVolume = volume;
						_sellCount = 1;
					}
				}
			}
			else if (kValue < dValue && dValue < ZoneSell)
			{
				if (!Reverse)
				{
					var volume = CheckVolume(SellVolume);
					if (volume > 0)
					{
						SellMarket(volume);
						_lastSellPrice = price;
						_lastSellVolume = volume;
						_sellCount = 1;
					}
				}
				else
				{
					var volume = CheckVolume(BuyVolume);
					if (volume > 0)
					{
						BuyMarket(volume);
						_lastBuyPrice = price;
						_lastBuyVolume = volume;
						_buyCount = 1;
					}
				}
			}
		}
	}

	private decimal CheckVolume(decimal volume)
	{
		var step = Security.VolumeStep ?? 0m;
		if (step > 0)
			volume = step * Math.Floor(volume / step);

		var min = Security.VolumeMin ?? 0m;
		if (volume < min)
			volume = 0m;

		var max = Security.VolumeMax ?? decimal.MaxValue;
		if (volume > max)
			volume = max;

		return volume;
	}
}
