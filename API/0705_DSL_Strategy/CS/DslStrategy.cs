namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class DslStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _offset;
	private readonly StrategyParam<decimal> _bandWidth;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _belugaLength;
	private readonly StrategyParam<bool> _dslFastMode;

	private SimpleMovingAverage _sma;
	private Highest _highest;
	private Lowest _lowest;
	private AverageTrueRange _atr;
	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _belugaSma;
	private ZeroLagExponentialMovingAverage _zlema;
	private SimpleMovingAverage _oscSma;

	private decimal? _dslUp;
	private decimal? _dslDn;

	private decimal _lvlu;
	private decimal _lvld;
	private decimal _lvlUp;
	private decimal _lvlDn;
	private decimal _prevDslOsc;

	private bool _prevAbove1;
	private bool _prevAbove2;
	private bool _prevBelow1;
	private bool _prevBelow2;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	public DslStrategy()
	{
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type");
		_length = Param(nameof(Length), 34).SetDisplay("Length");
		_offset = Param(nameof(Offset), 30).SetDisplay("Offset");
		_bandWidth = Param(nameof(BandsWidth), 1m).SetDisplay("Bands Width");
		_riskReward = Param(nameof(RiskReward), 1.5m).SetDisplay("Risk Reward");
		_belugaLength = Param(nameof(BelugaLength), 10).SetDisplay("Beluga Length");
		_dslFastMode = Param(nameof(DslFastMode), true).SetDisplay("DSL Fast Mode");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public int Offset
	{
		get => _offset.Value;
		set => _offset.Value = value;
	}

	public decimal BandsWidth
	{
		get => _bandWidth.Value;
		set => _bandWidth.Value = value;
	}

	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	public int BelugaLength
	{
		get => _belugaLength.Value;
		set => _belugaLength.Value = value;
	}

	public bool DslFastMode
	{
		get => _dslFastMode.Value;
		set => _dslFastMode.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new() { Length = Length };
		_highest = new() { Length = Length };
		_lowest = new() { Length = Length };
		_atr = new() { Length = 200 };
		_rsi = new() { Length = 10 };
		_belugaSma = new() { Length = BelugaLength };
		_zlema = new() { Length = 10 };
		_oscSma = new() { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_sma, _highest, _lowest, _atr, _rsi, ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal highestValue, decimal lowestValue, decimal atrValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;

		var thresholdUp = highestValue;
		var thresholdDn = lowestValue;

		_dslUp = close > thresholdUp ? smaValue : _dslUp ?? smaValue;
		_dslDn = close < thresholdDn ? smaValue : _dslDn ?? smaValue;

		var dslUp1 = _dslUp is decimal up ? up - atrValue * BandsWidth : (decimal?)null;
		var dslDn1 = _dslDn is decimal dn ? dn + atrValue * BandsWidth : (decimal?)null;

		var smaRsiValue = _belugaSma.Process(new DecimalIndicatorValue(_belugaSma, rsiValue));
		var mode = DslFastMode ? 2m : 1m;

		if (smaRsiValue.IsFinal)
		{
			var smaRsi = smaRsiValue.GetValue<decimal>();

			if (rsiValue > smaRsi)
				_lvlu += mode / BelugaLength * (rsiValue - _lvlu);
			if (rsiValue < smaRsi)
				_lvld += mode / BelugaLength * (rsiValue - _lvld);

			var avg = (_lvlu + _lvld) / 2m;
			var zlemaValue = _zlema.Process(new DecimalIndicatorValue(_zlema, avg));

			if (zlemaValue.IsFinal)
			{
				var osc = zlemaValue.GetValue<decimal>();
				var smaOscValue = _oscSma.Process(new DecimalIndicatorValue(_oscSma, osc));

				if (smaOscValue.IsFinal)
				{
					var smaOsc = smaOscValue.GetValue<decimal>();

					if (osc > smaOsc)
						_lvlUp += mode / 10m * (osc - _lvlUp);
					if (osc < smaOsc)
						_lvlDn += mode / 10m * (osc - _lvlDn);

					var upSignal = _prevDslOsc <= _lvlDn && osc > _lvlDn && osc < 55m;
					var dnSignal = _prevDslOsc >= _lvlUp && osc < _lvlUp && osc > 50m;
					_prevDslOsc = osc;

					var above = _dslUp is decimal u && open > u && close > u;
					var below = dslDn1 is decimal d1 && open < d1 && close < d1;

					var longCondition1 = dslUp1 is decimal up1 && _dslDn is decimal dn1 && up1 > dn1;
					var longCondition2 = above && _prevAbove1 && _prevAbove2;
					var longCondition3 = upSignal && Position == 0;
					var longCondition = longCondition1 && longCondition2 && longCondition3;

					var shortCondition1 = dslDn1 is decimal && dslUp1 is decimal upBand && _dslDn is decimal dnBand && dnBand < upBand;
					var shortCondition2 = below && _prevBelow1 && _prevBelow2;
					var shortCondition3 = dnSignal && Position == 0;
					var shortCondition = shortCondition1 && shortCondition2 && shortCondition3;

					_prevAbove2 = _prevAbove1;
					_prevAbove1 = above;
					_prevBelow2 = _prevBelow1;
					_prevBelow1 = below;

					if (longCondition && dslUp1 is decimal stopLong)
					{
						var risk = close - stopLong;
						if (risk > 0)
						{
							var take = close + risk * RiskReward;
							BuyMarket();
							_longStop = stopLong;
							_longTake = take;
						}
					}
					else if (shortCondition && dslDn1 is decimal stopShort)
					{
						var risk = stopShort - close;
						if (risk > 0)
						{
							var take = close - risk * RiskReward;
							SellMarket();
							_shortStop = stopShort;
							_shortTake = take;
						}
					}

					if (Position > 0 && _longStop is decimal ls && _longTake is decimal lt)
					{
						if (close <= ls || close >= lt)
						{
							SellMarket(Position);
							_longStop = null;
							_longTake = null;
						}
					}
					else if (Position < 0 && _shortStop is decimal ss && _shortTake is decimal st)
					{
						if (close >= ss || close <= st)
						{
							BuyMarket(-Position);
							_shortStop = null;
							_shortTake = null;
						}
					}
				}
			}
		}
	}
}
