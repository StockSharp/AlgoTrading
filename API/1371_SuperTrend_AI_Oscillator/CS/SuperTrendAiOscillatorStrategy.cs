using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining SuperTrend trailing stop with custom oscillator filter.
/// </summary>
public class SuperTrendAiOscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private StochasticOscillator _stochastic;

	private decimal _upper;
	private decimal _lower;
	private int _os;
	private int _prevOs;
	private decimal? _prevClose;
	private decimal _ts;
	private bool _initialized;
	private decimal _entryPrice;

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier.
	/// </summary>
	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
	}

	/// <summary>
	/// Risk to reward ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SuperTrendAiOscillatorStrategy"/>.
	/// </summary>
	public SuperTrendAiOscillatorStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation period", "Parameters")
			.SetCanOptimize(true);

		_factor = Param(nameof(Factor), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Factor", "ATR multiplier", "Parameters")
			.SetCanOptimize(true);

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk-Reward", "Risk to reward ratio", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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

		_atr = default;
		_stochastic = default;
		_upper = default;
		_lower = default;
		_os = default;
		_prevOs = default;
		_prevClose = default;
		_ts = default;
		_initialized = false;
		_entryPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrLength };
		_stochastic = new StochasticOscillator
		{
			Length = 13,
			K = { Length = 5 },
			D = { Length = 3 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_atr, _stochastic, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!atrValue.IsFinal || !stochValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.ToDecimal();
		var stoch = (StochasticOscillatorValue)stochValue;

		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		var superOsc = 3m * k - 2m * d;

		var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;
		var up = hl2 + atr * Factor;
		var dn = hl2 - atr * Factor;

		if (!_initialized)
		{
			_upper = up;
			_lower = dn;
			_prevClose = candle.ClosePrice;
			_ts = _lower;
			_initialized = true;
			return;
		}

		_upper = _prevClose < _upper ? Math.Min(up, _upper) : up;
		_lower = _prevClose > _lower ? Math.Max(dn, _lower) : dn;

		_prevClose = candle.ClosePrice;
		_prevOs = _os;
		_os = candle.ClosePrice > _upper ? 1 : candle.ClosePrice < _lower ? 0 : _os;
		_ts = _os == 1 ? _lower : _upper;

		var longCondition = _prevOs == 0 && _os == 1 && superOsc > 50m;
		var shortCondition = _prevOs == 1 && _os == 0 && superOsc < 50m;

		if (longCondition && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
		}
		else if (shortCondition && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
		}

		if (Position > 0)
		{
			var takeProfit = _entryPrice + (_entryPrice - _ts) * RiskReward;
			if (candle.LowPrice <= _ts || candle.HighPrice >= takeProfit)
			{
				SellMarket(Position);
				_entryPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			var takeProfit = _entryPrice - (_ts - _entryPrice) * RiskReward;
			if (candle.HighPrice >= _ts || candle.LowPrice <= takeProfit)
			{
				BuyMarket(-Position);
				_entryPrice = 0m;
			}
		}
	}
}
