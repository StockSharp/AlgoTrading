using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy using zero lag moving average and EMA breakout boxes.
/// </summary>
public class ZeroLagMaTrendFollowingStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private ZeroLagExponentialMovingAverage _zlma = null!;
	private ExponentialMovingAverage _ema = null!;
	private AverageTrueRange _atr = null!;

	private decimal _prevZlma;
	private decimal _prevEma;
	private bool _longSetup;
	private bool _shortSetup;
	private decimal _boxTop;
	private decimal _boxBottom;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private bool _entryPlaced;

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// ATR period for box height.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Risk-reward ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ZeroLagMaTrendFollowingStrategy"/>.
	/// </summary>
	public ZeroLagMaTrendFollowingStrategy()
	{
		_length = Param(nameof(Length), 34).SetDisplay("Length", "MA length", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetDisplay("ATR Period", "ATR length", "Indicators");
		_riskReward = Param(nameof(RiskReward), 2m).SetDisplay("Risk/Reward", "Take profit ratio", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_zlma = new ZeroLagExponentialMovingAverage { Length = Length };
		_ema = new ExponentialMovingAverage { Length = Length };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_zlma, _ema, _atr, ProcessCandle)
			.Start();

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal zlma, decimal ema, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = _prevZlma <= _prevEma && zlma > ema;
		var crossDown = _prevZlma >= _prevEma && zlma < ema;
		_prevZlma = zlma;
		_prevEma = ema;

		if (crossUp)
		{
			_boxTop = zlma;
			_boxBottom = zlma - atrValue;
			_longSetup = true;
			_shortSetup = false;
		}
		else if (crossDown)
		{
			_boxTop = zlma + atrValue;
			_boxBottom = zlma;
			_shortSetup = true;
			_longSetup = false;
		}

		var price = candle.ClosePrice;

		if (!_entryPlaced)
		{
			if (_longSetup && candle.LowPrice > _boxTop && Position <= 0)
			{
			    CancelActiveOrders();
			    var volume = Volume + Math.Abs(Position);
			    BuyMarket(volume);
			    _entryPlaced = true;
			    _stopPrice = _boxBottom;
			    _takeProfitPrice = price + (price - _stopPrice) * RiskReward;
			    _longSetup = false;
			}
			else if (_shortSetup && candle.HighPrice < _boxBottom && Position >= 0)
			{
			    CancelActiveOrders();
			    var volume = Volume + Math.Abs(Position);
			    SellMarket(volume);
			    _entryPlaced = true;
			    _stopPrice = _boxTop;
			    _takeProfitPrice = price - (_stopPrice - price) * RiskReward;
			    _shortSetup = false;
			}
		}
		else
		{
			if (Position > 0)
			{
			    if (candle.LowPrice <= _stopPrice)
			    {
			        SellMarket(Position);
			        _entryPlaced = false;
			    }
			    else if (candle.HighPrice >= _takeProfitPrice)
			    {
			        SellMarket(Position);
			        _entryPlaced = false;
			    }
			}
			else if (Position < 0)
			{
			    if (candle.HighPrice >= _stopPrice)
			    {
			        BuyMarket(Math.Abs(Position));
			        _entryPlaced = false;
			    }
			    else if (candle.LowPrice <= _takeProfitPrice)
			    {
			        BuyMarket(Math.Abs(Position));
			        _entryPlaced = false;
			    }
			}
		}
	}
}
