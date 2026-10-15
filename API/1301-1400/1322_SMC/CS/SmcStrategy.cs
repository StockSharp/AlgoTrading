using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Premium/discount SMC zone strategy with SMA and simple order-block confirmation.
/// </summary>
public class SmcStrategy : Strategy
{
	private readonly StrategyParam<int> _swingHighLength;
	private readonly StrategyParam<int> _swingLowLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _orderBlockLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _candles = [];

	public int SwingHighLength { get => _swingHighLength.Value; set => _swingHighLength.Value = value; }
	public int SwingLowLength { get => _swingLowLength.Value; set => _swingLowLength.Value = value; }
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public int OrderBlockLength { get => _orderBlockLength.Value; set => _orderBlockLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SmcStrategy()
	{
		_swingHighLength = Param(nameof(SwingHighLength), 8).SetGreaterThanZero();
		_swingLowLength = Param(nameof(SwingLowLength), 8).SetGreaterThanZero();
		_smaLength = Param(nameof(SmaLength), 50).SetGreaterThanZero();
		_orderBlockLength = Param(nameof(OrderBlockLength), 20).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_candles.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candles.Add(candle);
		var required = Math.Max(Math.Max(SwingHighLength, SwingLowLength), Math.Max(SmaLength, OrderBlockLength)) + 1;
		if (_candles.Count > required)
			_candles.RemoveRange(0, _candles.Count - required);

		if (_candles.Count < required)
			return;

		var previous = _candles.Take(_candles.Count - 1).ToArray();
		var swingHigh = previous.Skip(previous.Length - SwingHighLength).Max(c => c.HighPrice);
		var swingLow = previous.Skip(previous.Length - SwingLowLength).Min(c => c.LowPrice);
		var sma = _candles.Skip(_candles.Count - SmaLength).Average(c => c.ClosePrice);

		var orderBlock = previous.Skip(previous.Length - OrderBlockLength).ToArray();
		var support = orderBlock.Min(c => c.LowPrice);
		var resistance = orderBlock.Max(c => c.HighPrice);

		var hasSupport = candle.LowPrice <= support && candle.ClosePrice >= support;
		var hasResistance = candle.HighPrice >= resistance && candle.ClosePrice <= resistance;

		var signal = GetSignal(
			candle.ClosePrice, swingLow, swingHigh, sma, hasSupport, hasResistance);

		if (signal > 0 && Position <= 0m)
			BuyMarket(Volume + Math.Abs(Position));
		else if (signal < 0 && Position >= 0m)
			SellMarket(Volume + Math.Abs(Position));
	}

	internal static int GetSignal(
		decimal price,
		decimal swingLow,
		decimal swingHigh,
		decimal sma,
		bool hasSupport,
		bool hasResistance)
	{
		if (swingHigh <= swingLow)
			return 0;

		var equilibrium = (swingHigh + swingLow) / 2m;

		if (price <= equilibrium && price > sma && hasSupport)
			return 1;

		if (price >= equilibrium && price < sma && hasResistance)
			return -1;

		return 0;
	}
}
