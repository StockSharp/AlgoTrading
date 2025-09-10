using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ChopFlow ATR Scalp strategy.
/// Enters when market transitions out of choppy conditions with OBV confirmation.
/// Uses ATR-based stop-loss and take-profit levels.
/// </summary>
public class ChopFlowAtrScalpStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _chopLength;
	private readonly StrategyParam<decimal> _chopThreshold;
	private readonly StrategyParam<int> _obvEmaLength;
	private readonly StrategyParam<string> _sessionInput;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _obvEma;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// ATR multiplier for exits.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Choppiness Index period.
	/// </summary>
	public int ChopLength { get => _chopLength.Value; set => _chopLength.Value = value; }

	/// <summary>
	/// Choppiness threshold.
	/// </summary>
	public decimal ChopThreshold { get => _chopThreshold.Value; set => _chopThreshold.Value = value; }

	/// <summary>
	/// OBV EMA period.
	/// </summary>
	public int ObvEmaLength { get => _obvEmaLength.Value; set => _obvEmaLength.Value = value; }

	/// <summary>
	/// Trading session in HHmm-HHmm format.
	/// </summary>
	public string SessionInput { get => _sessionInput.Value; set => _sessionInput.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="ChopFlowAtrScalpStrategy"/>.
	/// </summary>
	public ChopFlowAtrScalpStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR calculation period", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
		.SetRange(0.1m, 5m)
		.SetDisplay("ATR Multiplier", "ATR multiplier for exits", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);

		_chopLength = Param(nameof(ChopLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Chop Length", "Choppiness Index period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 5);

		_chopThreshold = Param(nameof(ChopThreshold), 60m)
		.SetRange(20m, 100m)
		.SetDisplay("Chop Threshold", "Choppiness threshold", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(40m, 70m, 5m);

		_obvEmaLength = Param(nameof(ObvEmaLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("OBV EMA Length", "OBV EMA period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 5);

		_sessionInput = Param(nameof(SessionInput), "1700-1600")
		.SetDisplay("Session", "Trading session", "Time");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_obvEma = new ExponentialMovingAverage { Length = ObvEmaLength };

		var obv = new OnBalanceVolume();
		var atr = new AverageTrueRange { Length = AtrLength };
		var choppiness = new ChoppinessIndex { Length = ChopLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(obv, atr, choppiness, ProcessCandle)
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var obvArea = CreateChartArea();
			DrawIndicator(obvArea, obv);
			DrawIndicator(obvArea, _obvEma);

			var chopArea = CreateChartArea();
			DrawIndicator(chopArea, choppiness);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue obvValue, IIndicatorValue atrValue, IIndicatorValue chopValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!obvValue.IsFinal || !atrValue.IsFinal || !chopValue.IsFinal)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!InSession(candle.CloseTime))
		return;

		var obv = obvValue.ToDecimal();
		var obvEma = _obvEma.Process(obvValue).ToDecimal();
		var atr = atrValue.ToDecimal();
		var chop = chopValue.ToDecimal();
		var price = candle.ClosePrice;

		if (chop < ChopThreshold)
		{
			if (obv > obvEma && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_entryPrice = price;
				_stopPrice = price - atr * AtrMultiplier;
				_takeProfitPrice = price + atr * AtrMultiplier;
			}
			else if (obv < obvEma && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_entryPrice = price;
				_stopPrice = price + atr * AtrMultiplier;
				_takeProfitPrice = price - atr * AtrMultiplier;
			}
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = _stopPrice = _takeProfitPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = _stopPrice = _takeProfitPrice = 0m;
			}
		}
	}

	private bool InSession(DateTimeOffset time)
	{
		ParseSession(SessionInput, out var start, out var end);
		var t = time.TimeOfDay;
		return start <= end ? t >= start && t <= end : t >= start || t <= end;
	}

	private static void ParseSession(string input, out TimeSpan start, out TimeSpan end)
	{
		start = TimeSpan.Zero;
		end = TimeSpan.FromHours(24);
		if (string.IsNullOrWhiteSpace(input))
		return;

		var parts = input.Split('-', ':');
		if (parts.Length < 2)
		return;

		TimeSpan.TryParseExact(parts[0], "hhmm", null, out start);
		TimeSpan.TryParseExact(parts[1], "hhmm", null, out end);
	}
}
