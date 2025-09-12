using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// UT Bot trend strategy combined with RSI filter.
/// Opens long on trend reversal up with RSI oversold and
/// short on trend reversal down with RSI overbought.
/// </summary>
public class BacktestUtBotRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOver;
	private readonly StrategyParam<int> _rsiUnder;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _trail;
	private int _dir;
	private int _prevDir;

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public int RsiOver
	{
		get => _rsiOver.Value;
		set => _rsiOver.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public int RsiUnder
	{
		get => _rsiUnder.Value;
		set => _rsiUnder.Value = value;
	}

	/// <summary>
	/// ATR period length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// UT Bot factor.
	/// </summary>
	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
	}

	/// <summary>
	/// Take profit in percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
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
	/// Initializes <see cref="BacktestUtBotRsiStrategy"/>.
	/// </summary>
	public BacktestUtBotRsiStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Parameters");

		_rsiOver = Param(nameof(RsiOver), 60)
			.SetDisplay("RSI Overbought", "RSI overbought level", "Parameters");

		_rsiUnder = Param(nameof(RsiUnder), 40)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Parameters");

		_atrLength = Param(nameof(AtrLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Parameters");

		_factor = Param(nameof(Factor), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("UT Bot Factor", "ATR multiplier", "Parameters");

		_takeProfit = Param(nameof(TakeProfitPercent), 3.0m)
			.SetDisplay("Take Profit %", "Take profit percent", "Risk");

		_stopLoss = Param(nameof(StopLossPercent), 1.5m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_trail = null;
		_dir = 0;
		_prevDir = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, rsi, ProcessCandle).Start();

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upperBand = candle.ClosePrice + Factor * atrValue;
		var lowerBand = candle.ClosePrice - Factor * atrValue;

		if (_trail is null)
		{
			_trail = lowerBand;
			_dir = 0;
		}
		else if (candle.ClosePrice > _trail)
		{
			_trail = Math.Max(_trail.Value, lowerBand);
			_dir = 1;
		}
		else if (candle.ClosePrice < _trail)
		{
			_trail = Math.Min(_trail.Value, upperBand);
			_dir = -1;
		}

		var trendUp = _dir == 1 && _prevDir == -1;
		var trendDown = _dir == -1 && _prevDir == 1;

		if (trendUp && rsiValue < RsiUnder && Position <= 0)
		{
			BuyMarket();
		}
		else if (trendDown && rsiValue > RsiOver && Position >= 0)
		{
			SellMarket();
		}

		_prevDir = _dir;
	}
}
