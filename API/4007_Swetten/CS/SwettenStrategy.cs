using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Swetten neural network strategy converted from MetaTrader 4.
/// Calculates SMA spreads on one-minute candles and feeds them into a radial basis network.
/// Generates trades every two hours when the network output flips sign.
/// </summary>
public class SwettenStrategy : Strategy
{
	private static readonly int[] FastPeriods = { 144, 89, 55, 34, 21, 13, 8, 5, 3, 2 };

	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;

	private SimpleMovingAverage _slowSma = null!;
	private readonly SimpleMovingAverage[] _fastSmas = new SimpleMovingAverage[FastPeriods.Length];
	private decimal _priceStep;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public SwettenStrategy()
	{
		_slowPeriod = Param(nameof(SlowPeriod), 233)
			.SetDisplay("Slow SMA Period", "Length of the base moving average", "Indicators")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type for indicator calculations", "General");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Base order volume in lots", "Trading")
			.SetGreaterThanZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150)
			.SetDisplay("Take Profit", "Distance to the profit target in price steps", "Risk")
			.SetNotNegative();

		_stopLossPoints = Param(nameof(StopLossPoints), 40)
			.SetDisplay("Stop Loss", "Distance to the protective stop in price steps", "Risk")
			.SetNotNegative();
	}

	/// <summary>
	/// Period of the slowest moving average (baseline 233).
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles used for neural network inputs.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base trading volume used when entering a new position.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Profit target in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_priceStep = 0m;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;

		_slowSma = new SimpleMovingAverage { Length = SlowPeriod };

		for (var i = 0; i < _fastSmas.Length; i++)
		{
			_fastSmas[i] = new SimpleMovingAverage { Length = FastPeriods[i] };
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(
				_slowSma,
				_fastSmas[0],
				_fastSmas[1],
				_fastSmas[2],
				_fastSmas[3],
				_fastSmas[4],
				_fastSmas[5],
				_fastSmas[6],
				_fastSmas[7],
				_fastSmas[8],
				_fastSmas[9],
				ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _slowSma);
			DrawOwnTrades(area);
		}
	}

	/// <summary>
	/// Processes completed candles, updates exits, and generates new orders when required conditions are met.
	/// </summary>
	private void ProcessCandle(
		ICandleMessage candle,
		decimal slow,
		decimal ma144,
		decimal ma89,
		decimal ma55,
		decimal ma34,
		decimal ma21,
		decimal ma13,
		decimal ma8,
		decimal ma5,
		decimal ma3,
		decimal ma2)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (ManagePosition(candle))
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (!_slowSma.IsFormed)
		{
			return;
		}

		for (var i = 0; i < _fastSmas.Length; i++)
		{
			if (!_fastSmas[i].IsFormed)
			{
				return;
			}
		}

		var time = candle.CloseTime;
		if (time.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
		{
			return;
		}

				if (time.Minute != 0 || (time.Hour % 2) != 0)
		{
			return;
		}

		var inputs = new double[FastPeriods.Length];
		var slowValue = (double)slow;

		inputs[0] = slowValue - (double)ma144;
		inputs[1] = slowValue - (double)ma89;
		inputs[2] = slowValue - (double)ma55;
		inputs[3] = slowValue - (double)ma34;
		inputs[4] = slowValue - (double)ma21;
		inputs[5] = slowValue - (double)ma13;
		inputs[6] = slowValue - (double)ma8;
		inputs[7] = slowValue - (double)ma5;
		inputs[8] = slowValue - (double)ma3;
		inputs[9] = slowValue - (double)ma2;

		var output = EvaluateNetwork(inputs);
		var volume = TradeVolume + Math.Abs(Position);

		if (output > 0d)
		{
			if (Position <= 0 && volume > 0m)
			{
				BuyMarket(volume);
			}
		}
		else if (output < 0d)
		{
			if (Position >= 0 && volume > 0m)
			{
				SellMarket(volume);
			}
		}
	}

	/// <summary>
	/// Applies take-profit and stop-loss rules using candle extremums.
	/// </summary>
	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position == 0)
		{
			return false;
		}

		var takeOffset = _priceStep > 0m ? TakeProfitPoints * _priceStep : 0m;
		var stopOffset = _priceStep > 0m ? StopLossPoints * _priceStep : 0m;

		if (Position > 0)
		{
			var entry = _longEntryPrice ?? (PositionPrice != 0m ? PositionPrice : (decimal?)null) ?? candle.OpenPrice;

			if (takeOffset > 0m && candle.HighPrice - entry >= takeOffset)
			{
				SellMarket(Math.Abs(Position));
				return true;
			}

			if (stopOffset > 0m && entry - candle.LowPrice >= stopOffset)
			{
				SellMarket(Math.Abs(Position));
				return true;
			}
		}
		else if (Position < 0)
		{
			var entry = _shortEntryPrice ?? (PositionPrice != 0m ? PositionPrice : (decimal?)null) ?? candle.OpenPrice;

			if (takeOffset > 0m && entry - candle.LowPrice >= takeOffset)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}

			if (stopOffset > 0m && candle.HighPrice - entry >= stopOffset)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}
		}

		return false;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position > 0)
		{
			_longEntryPrice = trade.Trade.Price;
			_shortEntryPrice = null;
		}
		else if (Position < 0)
		{
			_shortEntryPrice = trade.Trade.Price;
			_longEntryPrice = null;
		}
		else
		{
			_longEntryPrice = null;
			_shortEntryPrice = null;
		}
	}

	/// <summary>
	/// Neural network implementation migrated from the original EURUSDn routine.
	/// </summary>
	private static double EvaluateNetwork(double[] rawInputs)
	{
		var inarray = new double[rawInputs.Length];
		for (var i = 0; i < rawInputs.Length; i++)
		{
			inarray[i] = rawInputs[i];
		}

		var minValues = new[]
		{
			-0.9968153,
			-0.9876356,
			-0.9455376,
			-0.8731447,
			-0.9228548,
			-0.8458624,
			-0.8250612,
			-0.8170462,
			-0.7128723,
			-0.5390543,
		};

		var maxValues = new[]
		{
			0.9912279,
			0.9751329,
			0.9703116,
			0.9568092,
			0.9776543,
			0.9684225,
			0.9103971,
			0.7589043,
			0.5189937,
			0.4367434,
		};

		var offsets = new[]
		{
			0.9968153,
			0.9876356,
			0.9455376,
			0.8731447,
			0.9228548,
			0.8458624,
			0.8250612,
			0.8170462,
			0.7128723,
			0.5390543,
		};

		var divisors = new[]
		{
			1.988043,
			1.962769,
			1.915849,
			1.829954,
			1.900509,
			1.814285,
			1.735458,
			1.575951,
			1.231866,
			0.9757977,
		};

		var multipliers = new[]
		{
			0.4823529,
			0.09411765,
			2.376471,
			0.8588235,
			2.317647,
			1.741176,
			0.09411765,
			1.870588,
			1.305882,
			2.447059,
		};
		for (var i = 0; i < inarray.Length; i++)
		{
			var value = inarray[i];

			if (value < minValues[i])
			{
				value = minValues[i];
			}
			else if (value > maxValues[i])
			{
				value = maxValues[i];
			}

			value = (value + offsets[i]) / divisors[i];
			inarray[i] = value * multipliers[i];
		}

		var feature2 = new double[38];

		feature2[0] = Math.Pow(inarray[0] - 0.000889163, 2);
		feature2[0] += Math.Pow(inarray[1] - 0.0812654, 2);
		feature2[0] += Math.Pow(inarray[2] - 1.960218, 2);
		feature2[0] += Math.Pow(inarray[3] - 0.2229109, 2);
		feature2[0] += Math.Pow(inarray[4] - 0.3805427, 2);
		feature2[0] += Math.Pow(inarray[5] - 0.445791, 2);
		feature2[0] += Math.Pow(inarray[6] - 0.05064157, 2);
		feature2[0] += Math.Pow(inarray[7] - 1.096478, 2);
		feature2[0] += Math.Pow(inarray[8] - 0.4719081, 2);
		feature2[0] += Math.Pow(inarray[9] - 0.4417005, 2);
		feature2[0] = Math.Exp(-feature2[0] / 0.1861218);

		feature2[1] = Math.Pow(inarray[0] - 0.005004976, 2);
		feature2[1] += Math.Pow(inarray[1] - 0.001067986, 2);
		feature2[1] += Math.Pow(inarray[2] - 0.03456692, 2);
		feature2[1] += Math.Pow(inarray[3] - 0.0375561, 2);
		feature2[1] += Math.Pow(inarray[4] - 1.363077, 2);
		feature2[1] += Math.Pow(inarray[5] - 1.234163, 2);
		feature2[1] += Math.Pow(inarray[6] - 0.04992515, 2);
		feature2[1] += Math.Pow(inarray[7] - 1.326206, 2);
		feature2[1] += Math.Pow(inarray[8] - 1.108252, 2);
		feature2[1] += Math.Pow(inarray[9] - 1.952089, 2);
		feature2[1] = Math.Exp(-feature2[1] / 0.1861218);

		feature2[2] = Math.Pow(inarray[0] - 0.02484213, 2);
		feature2[2] += Math.Pow(inarray[1] - 0.07485696, 2);
		feature2[2] += Math.Pow(inarray[2] - 2.221637, 2);
		feature2[2] += Math.Pow(inarray[3] - 0.7971078, 2);
		feature2[2] += Math.Pow(inarray[4] - 1.345066, 2);
		feature2[2] += Math.Pow(inarray[5] - 0.263566, 2);
		feature2[2] += Math.Pow(inarray[6] - 0.03033854, 2);
		feature2[2] += Math.Pow(inarray[7] - 0.4391496, 2);
		feature2[2] += Math.Pow(inarray[8] - 0.4931421, 2);
		feature2[2] += Math.Pow(inarray[9] - 1.405976, 2);
		feature2[2] = Math.Exp(-feature2[2] / 0.1861218);

		feature2[3] = Math.Pow(inarray[0] - 0.477237, 2);
		feature2[3] += Math.Pow(inarray[1] - 0.02954985, 2);
		feature2[3] += Math.Pow(inarray[2] - 0.02116284, 2);
		feature2[3] += Math.Pow(inarray[3] - 0.07086566, 2);
		feature2[3] += Math.Pow(inarray[4] - 1.131745, 2);
		feature2[3] += Math.Pow(inarray[5] - 1.447294, 2);
		feature2[3] += Math.Pow(inarray[6] - 0.056236, 2);
		feature2[3] += Math.Pow(inarray[7] - 0.1194166, 2);
		feature2[3] += Math.Pow(inarray[8] - 0.000000063186, 2);
		feature2[3] += Math.Pow(inarray[9], 2);
		feature2[3] = Math.Exp(-feature2[3] / 0.1861218);

		feature2[4] = Math.Pow(inarray[0] - 0.03997645, 2);
		feature2[4] += Math.Pow(inarray[1] - 0.008338865, 2);
		feature2[4] += Math.Pow(inarray[2] - 0.4439969, 2);
		feature2[4] += Math.Pow(inarray[3] - 0.2434077, 2);
		feature2[4] += Math.Pow(inarray[4] - 1.582111, 2);
		feature2[4] += Math.Pow(inarray[5] - 1.077382, 2);
		feature2[4] += Math.Pow(inarray[6] - 0.02703695, 2);
		feature2[4] += Math.Pow(inarray[7] - 0.7009466, 2);
		feature2[4] += Math.Pow(inarray[8] - 0.7786316, 2);
		feature2[4] += Math.Pow(inarray[9] - 1.821606, 2);
		feature2[4] = Math.Exp(-feature2[4] / 0.1861218);

		feature2[5] = Math.Pow(inarray[0] - 0.03997645, 2);
		feature2[5] += Math.Pow(inarray[1] - 0.03795453, 2);
		feature2[5] += Math.Pow(inarray[2] - 0.4211179, 2);
		feature2[5] += Math.Pow(inarray[3] - 0.2650536, 2);
		feature2[5] += Math.Pow(inarray[4] - 1.442005, 2);
		feature2[5] += Math.Pow(inarray[5] - 1.215619, 2);
		feature2[5] += Math.Pow(inarray[6] - 0.05647669, 2);
		feature2[5] += Math.Pow(inarray[7] - 0.2027616, 2);
		feature2[5] += Math.Pow(inarray[8] - 0.1092168, 2);
		feature2[5] += Math.Pow(inarray[9] - 0.3853737, 2);
		feature2[5] = Math.Exp(-feature2[5] / 0.1861218);

		feature2[6] = Math.Pow(inarray[0] - 0.1333482, 2);
		feature2[6] += Math.Pow(inarray[1] - 0.02954985, 2);
		feature2[6] += Math.Pow(inarray[2] - 0.4680712, 2);
		feature2[6] += Math.Pow(inarray[3] - 0.4329588, 2);
		feature2[6] += Math.Pow(inarray[4] - 1.331459, 2);
		feature2[6] += Math.Pow(inarray[5] - 0.6223203, 2);
		feature2[6] += Math.Pow(inarray[6] - 0.03947195, 2);
		feature2[6] += Math.Pow(inarray[7] - 0.8871927, 2);
		feature2[6] += Math.Pow(inarray[8] - 0.8125428, 2);
		feature2[6] += Math.Pow(inarray[9] - 1.284233, 2);
		feature2[6] = Math.Exp(-feature2[6] / 0.1861218);

		feature2[7] = Math.Pow(inarray[0] - 0.002048658, 2);
		feature2[7] += Math.Pow(inarray[1] - 0.008338865, 2);
		feature2[7] += Math.Pow(inarray[2] - 1.348293, 2);
		feature2[7] += Math.Pow(inarray[3] - 0.1752524, 2);
		feature2[7] += Math.Pow(inarray[4] - 0.8967286, 2);
		feature2[7] += Math.Pow(inarray[5] - 0.4706461, 2);
		feature2[7] += Math.Pow(inarray[6] - 0.04881833, 2);
		feature2[7] += Math.Pow(inarray[7] - 1.141258, 2);
		feature2[7] += Math.Pow(inarray[8] - 0.5672179, 2);
		feature2[7] += Math.Pow(inarray[9] - 1.286871, 2);
		feature2[7] = Math.Exp(-feature2[7] / 0.1861218);

		feature2[8] = Math.Pow(inarray[0] - 0.00856264, 2);
		feature2[8] += Math.Pow(inarray[1] - 0.03357983, 2);
		feature2[8] += Math.Pow(inarray[2] - 1.099166, 2);
		feature2[8] += Math.Pow(inarray[3] - 0.7331175, 2);
		feature2[8] += Math.Pow(inarray[4] - 1.943795, 2);
		feature2[8] += Math.Pow(inarray[5] - 1.505324, 2);
		feature2[8] += Math.Pow(inarray[6] - 0.06419788, 2);
		feature2[8] += Math.Pow(inarray[7] - 1.206465, 2);
		feature2[8] += Math.Pow(inarray[8] - 1.080792, 2);
		feature2[8] += Math.Pow(inarray[9] - 1.291121, 2);
		feature2[8] = Math.Exp(-feature2[8] / 0.1861218);

		feature2[9] = Math.Pow(inarray[0] - 0.1333482, 2);
		feature2[9] += Math.Pow(inarray[1] - 0.07731364, 2);
		feature2[9] += Math.Pow(inarray[2] - 2.283653, 2);
		feature2[9] += Math.Pow(inarray[3] - 0.8243476, 2);
		feature2[9] += Math.Pow(inarray[4] - 2.180786, 2);
		feature2[9] += Math.Pow(inarray[5] - 1.427877, 2);
		feature2[9] += Math.Pow(inarray[6] - 0.06812724, 2);
		feature2[9] += Math.Pow(inarray[7] - 1.245306, 2);
		feature2[9] += Math.Pow(inarray[8] - 0.6635692, 2);
		feature2[9] += Math.Pow(inarray[9] - 1.431758, 2);
		feature2[9] = Math.Exp(-feature2[9] / 0.1861218);
		for (var i = 0; i < inarray.Length; i++)
		{
			var value = inarray[i];

			if (value < minValues[i])
			{
				value = minValues[i];
			}
			else if (value > maxValues[i])
			{
				value = maxValues[i];
			}

			value = (value + offsets[i]) / divisors[i];
			inarray[i] = value * multipliers[i];
		}

		var feature2 = new double[38];

		feature2[0] = Math.Pow(inarray[0] - 0.000889163, 2);
		feature2[0] += Math.Pow(inarray[1] - 0.0812654, 2);
		feature2[0] += Math.Pow(inarray[2] - 1.960218, 2);
		feature2[0] += Math.Pow(inarray[3] - 0.2229109, 2);
		feature2[0] += Math.Pow(inarray[4] - 0.3805427, 2);
		feature2[0] += Math.Pow(inarray[5] - 0.445791, 2);
		feature2[0] += Math.Pow(inarray[6] - 0.05064157, 2);
		feature2[0] += Math.Pow(inarray[7] - 1.096478, 2);
		feature2[0] += Math.Pow(inarray[8] - 0.4719081, 2);
		feature2[0] += Math.Pow(inarray[9] - 0.4417005, 2);
		feature2[0] = Math.Exp(-feature2[0] / 0.1861218);

		feature2[1] = Math.Pow(inarray[0] - 0.005004976, 2);
		feature2[1] += Math.Pow(inarray[1] - 0.001067986, 2);
		feature2[1] += Math.Pow(inarray[2] - 0.03456692, 2);
		feature2[1] += Math.Pow(inarray[3] - 0.0375561, 2);
		feature2[1] += Math.Pow(inarray[4] - 1.363077, 2);
		feature2[1] += Math.Pow(inarray[5] - 1.234163, 2);
		feature2[1] += Math.Pow(inarray[6] - 0.04992515, 2);
		feature2[1] += Math.Pow(inarray[7] - 1.326206, 2);
		feature2[1] += Math.Pow(inarray[8] - 1.108252, 2);
		feature2[1] += Math.Pow(inarray[9] - 1.952089, 2);
		feature2[1] = Math.Exp(-feature2[1] / 0.1861218);

		feature2[2] = Math.Pow(inarray[0] - 0.02484213, 2);
		feature2[2] += Math.Pow(inarray[1] - 0.07485696, 2);
		feature2[2] += Math.Pow(inarray[2] - 2.221637, 2);
		feature2[2] += Math.Pow(inarray[3] - 0.7971078, 2);
		feature2[2] += Math.Pow(inarray[4] - 1.345066, 2);
		feature2[2] += Math.Pow(inarray[5] - 0.263566, 2);
		feature2[2] += Math.Pow(inarray[6] - 0.03033854, 2);
		feature2[2] += Math.Pow(inarray[7] - 0.4391496, 2);
		feature2[2] += Math.Pow(inarray[8] - 0.4931421, 2);
		feature2[2] += Math.Pow(inarray[9] - 1.405976, 2);
		feature2[2] = Math.Exp(-feature2[2] / 0.1861218);

		feature2[3] = Math.Pow(inarray[0] - 0.477237, 2);
		feature2[3] += Math.Pow(inarray[1] - 0.02954985, 2);
		feature2[3] += Math.Pow(inarray[2] - 0.02116284, 2);
		feature2[3] += Math.Pow(inarray[3] - 0.07086566, 2);
		feature2[3] += Math.Pow(inarray[4] - 1.131745, 2);
		feature2[3] += Math.Pow(inarray[5] - 1.447294, 2);
		feature2[3] += Math.Pow(inarray[6] - 0.056236, 2);
		feature2[3] += Math.Pow(inarray[7] - 0.1194166, 2);
		feature2[3] += Math.Pow(inarray[8] - 0.000000063186, 2);
		feature2[3] += Math.Pow(inarray[9], 2);
		feature2[3] = Math.Exp(-feature2[3] / 0.1861218);

		feature2[4] = Math.Pow(inarray[0] - 0.03997645, 2);
		feature2[4] += Math.Pow(inarray[1] - 0.008338865, 2);
		feature2[4] += Math.Pow(inarray[2] - 0.4439969, 2);
		feature2[4] += Math.Pow(inarray[3] - 0.2434077, 2);
		feature2[4] += Math.Pow(inarray[4] - 1.582111, 2);
		feature2[4] += Math.Pow(inarray[5] - 1.077382, 2);
		feature2[4] += Math.Pow(inarray[6] - 0.02703695, 2);
		feature2[4] += Math.Pow(inarray[7] - 0.7009466, 2);
		feature2[4] += Math.Pow(inarray[8] - 0.7786316, 2);
		feature2[4] += Math.Pow(inarray[9] - 1.821606, 2);
		feature2[4] = Math.Exp(-feature2[4] / 0.1861218);

		feature2[5] = Math.Pow(inarray[0] - 0.03997645, 2);
		feature2[5] += Math.Pow(inarray[1] - 0.03795453, 2);
		feature2[5] += Math.Pow(inarray[2] - 0.4211179, 2);
		feature2[5] += Math.Pow(inarray[3] - 0.2650536, 2);
		feature2[5] += Math.Pow(inarray[4] - 1.442005, 2);
		feature2[5] += Math.Pow(inarray[5] - 1.215619, 2);
		feature2[5] += Math.Pow(inarray[6] - 0.05647669, 2);
		feature2[5] += Math.Pow(inarray[7] - 0.2027616, 2);
		feature2[5] += Math.Pow(inarray[8] - 0.1092168, 2);
		feature2[5] += Math.Pow(inarray[9] - 0.3853737, 2);
		feature2[5] = Math.Exp(-feature2[5] / 0.1861218);

		feature2[6] = Math.Pow(inarray[0] - 0.1333482, 2);
		feature2[6] += Math.Pow(inarray[1] - 0.02954985, 2);
		feature2[6] += Math.Pow(inarray[2] - 0.4680712, 2);
		feature2[6] += Math.Pow(inarray[3] - 0.4329588, 2);
		feature2[6] += Math.Pow(inarray[4] - 1.331459, 2);
		feature2[6] += Math.Pow(inarray[5] - 0.6223203, 2);
		feature2[6] += Math.Pow(inarray[6] - 0.03947195, 2);
		feature2[6] += Math.Pow(inarray[7] - 0.8871927, 2);
		feature2[6] += Math.Pow(inarray[8] - 0.8125428, 2);
		feature2[6] += Math.Pow(inarray[9] - 1.284233, 2);
		feature2[6] = Math.Exp(-feature2[6] / 0.1861218);

		feature2[7] = Math.Pow(inarray[0] - 0.002048658, 2);
		feature2[7] += Math.Pow(inarray[1] - 0.008338865, 2);
		feature2[7] += Math.Pow(inarray[2] - 1.348293, 2);
		feature2[7] += Math.Pow(inarray[3] - 0.1752524, 2);
		feature2[7] += Math.Pow(inarray[4] - 0.8967286, 2);
		feature2[7] += Math.Pow(inarray[5] - 0.4706461, 2);
		feature2[7] += Math.Pow(inarray[6] - 0.04881833, 2);
		feature2[7] += Math.Pow(inarray[7] - 1.141258, 2);
		feature2[7] += Math.Pow(inarray[8] - 0.5672179, 2);
		feature2[7] += Math.Pow(inarray[9] - 1.286871, 2);
		feature2[7] = Math.Exp(-feature2[7] / 0.1861218);

		feature2[8] = Math.Pow(inarray[0] - 0.00856264, 2);
		feature2[8] += Math.Pow(inarray[1] - 0.03357983, 2);
		feature2[8] += Math.Pow(inarray[2] - 1.099166, 2);
		feature2[8] += Math.Pow(inarray[3] - 0.7331175, 2);
		feature2[8] += Math.Pow(inarray[4] - 1.943795, 2);
		feature2[8] += Math.Pow(inarray[5] - 1.505324, 2);
		feature2[8] += Math.Pow(inarray[6] - 0.06419788, 2);
		feature2[8] += Math.Pow(inarray[7] - 1.206465, 2);
		feature2[8] += Math.Pow(inarray[8] - 1.080792, 2);
		feature2[8] += Math.Pow(inarray[9] - 1.291121, 2);
		feature2[8] = Math.Exp(-feature2[8] / 0.1861218);

		feature2[9] = Math.Pow(inarray[0] - 0.1333482, 2);
		feature2[9] += Math.Pow(inarray[1] - 0.07731364, 2);
		feature2[9] += Math.Pow(inarray[2] - 2.283653, 2);
		feature2[9] += Math.Pow(inarray[3] - 0.8243476, 2);
		feature2[9] += Math.Pow(inarray[4] - 2.180786, 2);
		feature2[9] += Math.Pow(inarray[5] - 1.427877, 2);
		feature2[9] += Math.Pow(inarray[6] - 0.06812724, 2);
		feature2[9] += Math.Pow(inarray[7] - 1.245306, 2);
		feature2[9] += Math.Pow(inarray[8] - 0.6635692, 2);
		feature2[9] += Math.Pow(inarray[9] - 1.431758, 2);
		feature2[9] = Math.Exp(-feature2[9] / 0.1861218);

		feature2[10] = Math.Pow(inarray[0] - 0.0165808, 2);
		feature2[10] += Math.Pow(inarray[1] - 0.001067986, 2);
		feature2[10] += Math.Pow(inarray[2] - 0.02921889, 2);
		feature2[10] += Math.Pow(inarray[3] - 0.03873064, 2);
		feature2[10] += Math.Pow(inarray[4] - 0.2049543, 2);
		feature2[10] += Math.Pow(inarray[5] - 1.091212, 2);
		feature2[10] += Math.Pow(inarray[6] - 0.07272343, 2);
		feature2[10] += Math.Pow(inarray[7] - 0.4463593, 2);
		feature2[10] += Math.Pow(inarray[8] - 0.3753075, 2);
		feature2[10] += Math.Pow(inarray[9] - 1.12231, 2);
		feature2[10] = Math.Exp(-feature2[10] / 0.1861218);

		feature2[11] = Math.Pow(inarray[0] - 0.2418543, 2);
		feature2[11] += Math.Pow(inarray[1] - 0.06880314, 2);
		feature2[11] += Math.Pow(inarray[2] - 0.04994078, 2);
		feature2[11] += Math.Pow(inarray[3] - 0.1397965, 2);
		feature2[11] += Math.Pow(inarray[4] - 0.4064695, 2);
		feature2[11] += Math.Pow(inarray[5] - 0.7573185, 2);
		feature2[11] += Math.Pow(inarray[6] - 0.02113329, 2);
		feature2[11] += Math.Pow(inarray[7] - 0.3229572, 2);
		feature2[11] += Math.Pow(inarray[8] - 0.3608783, 2);
		feature2[11] += Math.Pow(inarray[9] - 1.261533, 2);
		feature2[11] = Math.Exp(-feature2[11] / 0.1861218);

		feature2[12] = Math.Pow(inarray[0] - 0.07029111, 2);
		feature2[12] += Math.Pow(inarray[1] - 0.0828398, 2);
		feature2[12] += Math.Pow(inarray[2] - 2.028337, 2);
		feature2[12] += Math.Pow(inarray[3] - 0.5885905, 2);
		feature2[12] += Math.Pow(inarray[4] - 0.5318826, 2);
		feature2[12] += Math.Pow(inarray[5] - 0.4206511, 2);
		feature2[12] += Math.Pow(inarray[6] - 0.03244338, 2);
		feature2[12] += Math.Pow(inarray[7] - 0.3602117, 2);
		feature2[12] += Math.Pow(inarray[8] - 0.577378, 2);
		feature2[12] += Math.Pow(inarray[9] - 1.761364, 2);
		feature2[12] = Math.Exp(-feature2[12] / 0.1861218);

		feature2[13] = Math.Pow(inarray[0] - 0.4134175, 2);
		feature2[13] += Math.Pow(inarray[1] - 0.07202942, 2);
		feature2[13] += Math.Pow(inarray[2] - 2.044716, 2);
		feature2[13] += Math.Pow(inarray[3] - 0.7512222, 2);
		feature2[13] += Math.Pow(inarray[4] - 1.648931, 2);
		feature2[13] += Math.Pow(inarray[5] - 0.491877, 2);
		feature2[13] += Math.Pow(inarray[6] - 0.03325736, 2);
		feature2[13] += Math.Pow(inarray[7] - 0.6765239, 2);
		feature2[13] += Math.Pow(inarray[8] - 0.4526261, 2);
		feature2[13] += Math.Pow(inarray[9] - 1.342896, 2);
		feature2[13] = Math.Exp(-feature2[13] / 0.1861218);

		feature2[14] = Math.Pow(inarray[0] - 0.01167812, 2);
		feature2[14] += Math.Pow(inarray[1] - 0.002026555, 2);
		feature2[14] += Math.Pow(inarray[2] - 1.305017, 2);
		feature2[14] += Math.Pow(inarray[3] - 0.7808279, 2);
		feature2[14] += Math.Pow(inarray[4] - 2.317647, 2);
		feature2[14] += Math.Pow(inarray[5] - 1.741176, 2);
		feature2[14] += Math.Pow(inarray[6] - 0.09411765, 2);
		feature2[14] += Math.Pow(inarray[7] - 1.824782, 2);
		feature2[14] += Math.Pow(inarray[8] - 0.9379704, 2);
		feature2[14] += Math.Pow(inarray[9] - 1.126272, 2);
		feature2[14] = Math.Exp(-feature2[14] / 0.1861218);

		feature2[15] = Math.Pow(inarray[0] - 0.02484213, 2);
		feature2[15] += Math.Pow(inarray[1] - 0.01740354, 2);
		feature2[15] += Math.Pow(inarray[2] - 0.6076435, 2);
		feature2[15] += Math.Pow(inarray[3] - 0.1440797, 2);
		feature2[15] += Math.Pow(inarray[4] - 0.5070454, 2);
		feature2[15] += Math.Pow(inarray[5] - 0.5617672, 2);
		feature2[15] += Math.Pow(inarray[6] - 0.004748089, 2);
		feature2[15] += Math.Pow(inarray[7], 2);
		feature2[15] += Math.Pow(inarray[8] - 0.1619062, 2);
		feature2[15] += Math.Pow(inarray[9] - 1.714904, 2);
		feature2[15] = Math.Exp(-feature2[15] / 0.1861218);

		feature2[16] = Math.Pow(inarray[0] - 0.4797696, 2);
		feature2[16] += Math.Pow(inarray[1] - 0.09336158, 2);
		feature2[16] += Math.Pow(inarray[2] - 2.356911, 2);
		feature2[16] += Math.Pow(inarray[3] - 0.0364001, 2);
		feature2[16] += Math.Pow(inarray[4] - 0.08372539, 2);
		feature2[16] += Math.Pow(inarray[5] - 0.106237, 2);
		feature2[16] += Math.Pow(inarray[6] - 0.007292937, 2);
		feature2[16] += Math.Pow(inarray[7] - 0.2185573, 2);
		feature2[16] += Math.Pow(inarray[8] - 0.6566861, 2);
		feature2[16] += Math.Pow(inarray[9] - 1.550643, 2);
		feature2[16] = Math.Exp(-feature2[16] / 0.1861218);

		feature2[17] = Math.Pow(inarray[0] - 0.003140907, 2);
		feature2[17] += Math.Pow(inarray[1] - 0.003383724, 2);
		feature2[17] += Math.Pow(inarray[2] - 0.07914829, 2);
		feature2[17] += Math.Pow(inarray[3] - 0.3211505, 2);
		feature2[17] += Math.Pow(inarray[4] - 1.134912, 2);
		feature2[17] += Math.Pow(inarray[5] - 0.9465042, 2);
		feature2[17] += Math.Pow(inarray[6] - 0.07714743, 2);
		feature2[17] += Math.Pow(inarray[7] - 1.670727, 2);
		feature2[17] += Math.Pow(inarray[8] - 1.127436, 2);
		feature2[17] += Math.Pow(inarray[9] - 1.75758, 2);
		feature2[17] = Math.Exp(-feature2[17] / 0.1861218);

		feature2[18] = Math.Pow(inarray[0] - 0.1333482, 2);
		feature2[18] += Math.Pow(inarray[1] - 0.004925451, 2);
		feature2[18] += Math.Pow(inarray[2] - 2.358818, 2);
		feature2[18] += Math.Pow(inarray[3] - 0.01149535, 2);
		feature2[18] += Math.Pow(inarray[4] - 1.904187, 2);
		feature2[18] += Math.Pow(inarray[5] - 1.600977, 2);
		feature2[18] += Math.Pow(inarray[6] - 0.08518686, 2);
		feature2[18] += Math.Pow(inarray[7] - 1.7597, 2);
		feature2[18] += Math.Pow(inarray[8] - 0.7659574, 2);
		feature2[18] += Math.Pow(inarray[9] - 1.493686, 2);
		feature2[18] = Math.Exp(-feature2[18] / 0.1861218);

		feature2[19] = Math.Pow(inarray[0] - 0.0000000144617, 2);
		feature2[19] += Math.Pow(inarray[1] - 0.00000000285813, 2);
		feature2[19] += Math.Pow(inarray[2] - 0.0000000739352, 2);
		feature2[19] += Math.Pow(inarray[3], 2);
		feature2[19] += Math.Pow(inarray[4] - 0.01601893, 2);
		feature2[19] += Math.Pow(inarray[5] - 0.05264656, 2);
		feature2[19] += Math.Pow(inarray[6] - 0.01949112, 2);
		feature2[19] += Math.Pow(inarray[7] - 1.165342, 2);
		feature2[19] += Math.Pow(inarray[8] - 1.00786, 2);
		feature2[19] += Math.Pow(inarray[9] - 1.539615, 2);
		feature2[19] = Math.Exp(-feature2[19] / 0.1861218);

		feature2[20] = Math.Pow(inarray[0] - 0.000320225, 2);
		feature2[20] += Math.Pow(inarray[1] - 0.0000189151, 2);
		feature2[20] += Math.Pow(inarray[2] - 0.01893473, 2);
		feature2[20] += Math.Pow(inarray[3] - 0.01383689, 2);
		feature2[20] += Math.Pow(inarray[4], 2);
		feature2[20] += Math.Pow(inarray[5], 2);
		feature2[20] += Math.Pow(inarray[6] - 0.02966899, 2);
		feature2[20] += Math.Pow(inarray[7] - 0.9710125, 2);
		feature2[20] += Math.Pow(inarray[8] - 0.9873776, 2);
		feature2[20] += Math.Pow(inarray[9] - 2.161196, 2);
		feature2[20] = Math.Exp(-feature2[20] / 0.1861218);

		feature2[21] = Math.Pow(inarray[0] - 0.005004976, 2);
		feature2[21] += Math.Pow(inarray[1] - 0.002836758, 2);
		feature2[21] += Math.Pow(inarray[2] - 0.04994078, 2);
		feature2[21] += Math.Pow(inarray[3] - 0.03249545, 2);
		feature2[21] += Math.Pow(inarray[4] - 1.357091, 2);
		feature2[21] += Math.Pow(inarray[5] - 1.343082, 2);
		feature2[21] += Math.Pow(inarray[6] - 0.06585187, 2);
		feature2[21] += Math.Pow(inarray[7] - 1.202506, 2);
		feature2[21] += Math.Pow(inarray[8] - 0.9318905, 2);
		feature2[21] += Math.Pow(inarray[9] - 0.4229908, 2);
		feature2[21] = Math.Exp(-feature2[21] / 0.1861218);

		feature2[22] = Math.Pow(inarray[0] - 0.4588665, 2);
		feature2[22] += Math.Pow(inarray[1] - 0.09065208, 2);
		feature2[22] += Math.Pow(inarray[2] - 2.286221, 2);
		feature2[22] += Math.Pow(inarray[3] - 0.7106162, 2);
		feature2[22] += Math.Pow(inarray[4] - 1.968035, 2);
		feature2[22] += Math.Pow(inarray[5] - 1.458471, 2);
		feature2[22] += Math.Pow(inarray[6] - 0.04956202, 2);
		feature2[22] += Math.Pow(inarray[7] - 1.257551, 2);
		feature2[22] += Math.Pow(inarray[8] - 0.8764666, 2);
		feature2[22] += Math.Pow(inarray[9] - 0.5849004, 2);
		feature2[22] = Math.Exp(-feature2[22] / 0.1861218);

		feature2[23] = Math.Pow(inarray[0] - 0.4671278, 2);
		feature2[23] += Math.Pow(inarray[1] - 0.09024769, 2);
		feature2[23] += Math.Pow(inarray[2] - 2.217067, 2);
		feature2[23] += Math.Pow(inarray[3] - 0.6357771, 2);
		feature2[23] += Math.Pow(inarray[4] - 0.1898238, 2);
		feature2[23] += Math.Pow(inarray[5] - 0.1001203, 2);
		feature2[23] += Math.Pow(inarray[6] - 0.007896511, 2);
		feature2[23] += Math.Pow(inarray[7] - 0.5215328, 2);
		feature2[23] += Math.Pow(inarray[8] - 0.5950916, 2);
		feature2[23] += Math.Pow(inarray[9] - 1.563175, 2);
		feature2[23] = Math.Exp(-feature2[23] / 0.1861218);

		feature2[24] = Math.Pow(inarray[0] - 0.01167812, 2);
		feature2[24] += Math.Pow(inarray[1] - 0.005441452, 2);
		feature2[24] += Math.Pow(inarray[2] - 0.2351743, 2);
		feature2[24] += Math.Pow(inarray[3] - 0.01383689, 2);
		feature2[24] += Math.Pow(inarray[4] - 0.1531568, 2);
		feature2[24] += Math.Pow(inarray[5] - 0.3552061, 2);
		feature2[24] += Math.Pow(inarray[6] - 0.04040769, 2);
		feature2[24] += Math.Pow(inarray[7] - 1.257551, 2);
		feature2[24] += Math.Pow(inarray[8] - 0.9742168, 2);
		feature2[24] += Math.Pow(inarray[9] - 1.728614, 2);
		feature2[24] = Math.Exp(-feature2[24] / 0.1861218);

		feature2[25] = Math.Pow(inarray[0] - 0.4437321, 2);
		feature2[25] += Math.Pow(inarray[1] - 0.01052124, 2);
		feature2[25] += Math.Pow(inarray[2] - 0.08071024, 2);
		feature2[25] += Math.Pow(inarray[3] - 0.02579022, 2);
		feature2[25] += Math.Pow(inarray[4] - 0.1812214, 2);
		feature2[25] += Math.Pow(inarray[5] - 0.3193101, 2);
		feature2[25] += Math.Pow(inarray[6] - 0.02369904, 2);
		feature2[25] += Math.Pow(inarray[7] - 0.7097268, 2);
		feature2[25] += Math.Pow(inarray[8] - 0.7359379, 2);
		feature2[25] += Math.Pow(inarray[9] - 1.762847, 2);
		feature2[25] = Math.Exp(-feature2[25] / 0.1861218);

		feature2[26] = Math.Pow(inarray[0] - 0.2418543, 2);
		feature2[26] += Math.Pow(inarray[1] - 0.05212994, 2);
		feature2[26] += Math.Pow(inarray[2] - 0.6616061, 2);
		feature2[26] += Math.Pow(inarray[3] - 0.2434077, 2);
		feature2[26] += Math.Pow(inarray[4] - 0.9345729, 2);
		feature2[26] += Math.Pow(inarray[5] - 0.6033763, 2);
		feature2[26] += Math.Pow(inarray[6] - 0.03585919, 2);
		feature2[26] += Math.Pow(inarray[7] - 0.946506, 2);
		feature2[26] += Math.Pow(inarray[8] - 0.727547, 2);
		feature2[26] += Math.Pow(inarray[9] - 1.323893, 2);
		feature2[26] = Math.Exp(-feature2[26] / 0.1861218);

		feature2[27] = Math.Pow(inarray[0] - 0.4134175, 2);
		feature2[27] += Math.Pow(inarray[1] - 0.06880314, 2);
		feature2[27] += Math.Pow(inarray[2] - 2.376471, 2);
		feature2[27] += Math.Pow(inarray[3] - 0.8588235, 2);
		feature2[27] += Math.Pow(inarray[4] - 2.216153, 2);
		feature2[27] += Math.Pow(inarray[5] - 1.636531, 2);
		feature2[27] += Math.Pow(inarray[6] - 0.08925647, 2);
		feature2[27] += Math.Pow(inarray[7] - 1.73262, 2);
		feature2[27] += Math.Pow(inarray[8] - 0.9227096, 2);
		feature2[27] += Math.Pow(inarray[9] - 0.7684286, 2);
		feature2[27] = Math.Exp(-feature2[27] / 0.1861218);

		feature2[28] = Math.Pow(inarray[0] - 0.003140907, 2);
		feature2[28] += Math.Pow(inarray[1] - 0.002026555, 2);
		feature2[28] += Math.Pow(inarray[2] - 0.1903487, 2);
		feature2[28] += Math.Pow(inarray[3] - 0.1101131, 2);
		feature2[28] += Math.Pow(inarray[4] - 0.5755196, 2);
		feature2[28] += Math.Pow(inarray[5] - 0.8396258, 2);
		feature2[28] += Math.Pow(inarray[6] - 0.06927142, 2);
		feature2[28] += Math.Pow(inarray[7] - 1.19529, 2);
		feature2[28] += Math.Pow(inarray[8] - 0.9351867, 2);
		feature2[28] += Math.Pow(inarray[9] - 1.508576, 2);
		feature2[28] = Math.Exp(-feature2[28] / 0.1861218);

		feature2[29] = Math.Pow(inarray[0] - 0.006471551, 2);
		feature2[29] += Math.Pow(inarray[1] - 0.004469482, 2);
		feature2[29] += Math.Pow(inarray[2] - 0.2707826, 2);
		feature2[29] += Math.Pow(inarray[3] - 0.03358909, 2);
		feature2[29] += Math.Pow(inarray[4] - 0.4098204, 2);
		feature2[29] += Math.Pow(inarray[5] - 0.2399384, 2);
		feature2[29] += Math.Pow(inarray[6] - 0.02301284, 2);
		feature2[29] += Math.Pow(inarray[7] - 0.9791961, 2);
		feature2[29] += Math.Pow(inarray[8] - 0.7661448, 2);
		feature2[29] += Math.Pow(inarray[9] - 1.484248, 2);
		feature2[29] = Math.Exp(-feature2[29] / 0.1861218);

		feature2[30] = Math.Pow(inarray[0] - 0.4588665, 2);
		feature2[30] += Math.Pow(inarray[1] - 0.07485696, 2);
		feature2[30] += Math.Pow(inarray[2] - 0.5973035, 2);
		feature2[30] += Math.Pow(inarray[3] - 0.01670468, 2);
		feature2[30] += Math.Pow(inarray[4] - 0.2150007, 2);
		feature2[30] += Math.Pow(inarray[5] - 0.1481559, 2);
		feature2[30] += Math.Pow(inarray[6] - 0.01565715, 2);
		feature2[30] += Math.Pow(inarray[7] - 0.6264769, 2);
		feature2[30] += Math.Pow(inarray[8] - 0.8419943, 2);
		feature2[30] += Math.Pow(inarray[9] - 1.380335, 2);
		feature2[30] = Math.Exp(-feature2[30] / 0.1861218);

		feature2[31] = Math.Pow(inarray[0] - 0.03997645, 2);
		feature2[31] += Math.Pow(inarray[1] - 0.01052124, 2);
		feature2[31] += Math.Pow(inarray[2] - 1.513642, 2);
		feature2[31] += Math.Pow(inarray[3] - 0.8544291, 2);
		feature2[31] += Math.Pow(inarray[4] - 2.296429, 2);
		feature2[31] += Math.Pow(inarray[5] - 1.678599, 2);
		feature2[31] += Math.Pow(inarray[6] - 0.06536879, 2);
		feature2[31] += Math.Pow(inarray[7] - 0.4521235, 2);
		feature2[31] += Math.Pow(inarray[8] - 0.5051789, 2);
		feature2[31] += Math.Pow(inarray[9] - 1.82941, 2);
		feature2[31] = Math.Exp(-feature2[31] / 0.1861218);

		feature2[32] = Math.Pow(inarray[0] - 0.003938961, 2);
		feature2[32] += Math.Pow(inarray[1] - 0.02954985, 2);
		feature2[32] += Math.Pow(inarray[2] - 1.44653, 2);
		feature2[32] += Math.Pow(inarray[3] - 0.8099366, 2);
		feature2[32] += Math.Pow(inarray[4] - 2.201304, 2);
		feature2[32] += Math.Pow(inarray[5] - 1.492814, 2);
		feature2[32] += Math.Pow(inarray[6] - 0.08035071, 2);
		feature2[32] += Math.Pow(inarray[7] - 1.291567, 2);
		feature2[32] += Math.Pow(inarray[8] - 0.7814157, 2);
		feature2[32] += Math.Pow(inarray[9] - 1.187166, 2);
		feature2[32] = Math.Exp(-feature2[32] / 0.1861218);

		feature2[33] = Math.Pow(inarray[0] - 0.03997645, 2);
		feature2[33] += Math.Pow(inarray[1] - 0.01345177, 2);
		feature2[33] += Math.Pow(inarray[2] - 0.0888809, 2);
		feature2[33] += Math.Pow(inarray[3] - 0.09145273, 2);
		feature2[33] += Math.Pow(inarray[4] - 0.9544851, 2);
		feature2[33] += Math.Pow(inarray[5] - 0.6133671, 2);
		feature2[33] += Math.Pow(inarray[6] - 0.08232641, 2);
		feature2[33] += Math.Pow(inarray[7] - 1.560351, 2);
		feature2[33] += Math.Pow(inarray[8] - 0.771435, 2);
		feature2[33] += Math.Pow(inarray[9] - 0.8860366, 2);
		feature2[33] = Math.Exp(-feature2[33] / 0.1861218);

		feature2[34] = Math.Pow(inarray[0] - 0.4134175, 2);
		feature2[34] += Math.Pow(inarray[1] - 0.01986022, 2);
		feature2[34] += Math.Pow(inarray[2] - 1.158104, 2);
		feature2[34] += Math.Pow(inarray[3] - 0.598632, 2);
		feature2[34] += Math.Pow(inarray[4] - 2.118678, 2);
		feature2[34] += Math.Pow(inarray[5] - 1.524671, 2);
		feature2[34] += Math.Pow(inarray[6] - 0.08608676, 2);
		feature2[34] += Math.Pow(inarray[7] - 1.870588, 2);
		feature2[34] += Math.Pow(inarray[8] - 0.8369381, 2);
		feature2[34] += Math.Pow(inarray[9] - 0.7220582, 2);
		feature2[34] = Math.Exp(-feature2[34] / 0.1861218);

		feature2[35] = Math.Pow(inarray[0] - 0.4588665, 2);
		feature2[35] += Math.Pow(inarray[1] - 0.03357983, 2);
		feature2[35] += Math.Pow(inarray[2] - 1.602334, 2);
		feature2[35] += Math.Pow(inarray[3] - 0.2717606, 2);
		feature2[35] += Math.Pow(inarray[4] - 0.1902421, 2);
		feature2[35] += Math.Pow(inarray[5] - 0.00084889, 2);
		feature2[35] += Math.Pow(inarray[6], 2);
		feature2[35] += Math.Pow(inarray[7] - 0.259377, 2);
		feature2[35] += Math.Pow(inarray[8] - 0.7457957, 2);
		feature2[35] += Math.Pow(inarray[9] - 1.958541, 2);
		feature2[35] = Math.Exp(-feature2[35] / 0.1861218);

		feature2[36] = Math.Pow(inarray[0] - 0.005004976, 2);
		feature2[36] += Math.Pow(inarray[1] - 0.002392147, 2);
		feature2[36] += Math.Pow(inarray[2] - 0.01608778, 2);
		feature2[36] += Math.Pow(inarray[3] - 0.1124831, 2);
		feature2[36] += Math.Pow(inarray[4] - 1.982063, 2);
		feature2[36] += Math.Pow(inarray[5] - 1.456154, 2);
		feature2[36] += Math.Pow(inarray[6] - 0.08319337, 2);
		feature2[36] += Math.Pow(inarray[7] - 1.753423, 2);
		feature2[36] += Math.Pow(inarray[8] - 1.045702, 2);
		feature2[36] += Math.Pow(inarray[9] - 0.9721034, 2);
		feature2[36] = Math.Exp(-feature2[36] / 0.1861218);

		feature2[37] = Math.Pow(inarray[0] - 0.0165808, 2);
		feature2[37] += Math.Pow(inarray[1] - 0.01345177, 2);
		feature2[37] += Math.Pow(inarray[2] - 0.9129646, 2);
		feature2[37] += Math.Pow(inarray[3] - 0.007112046, 2);
		feature2[37] += Math.Pow(inarray[4] - 0.05920475, 2);
		feature2[37] += Math.Pow(inarray[5] - 0.08653653, 2);
		feature2[37] += Math.Pow(inarray[6] - 0.01888973, 2);
		feature2[37] += Math.Pow(inarray[7] - 0.2948249, 2);
		feature2[37] += Math.Pow(inarray[8] - 0.6147208, 2);
		feature2[37] += Math.Pow(inarray[9] - 1.940528, 2);
		feature2[37] = Math.Exp(-feature2[37] / 0.1861218);

		var output = feature2[3] + feature2[10] + feature2[11] + feature2[12] + feature2[13] + feature2[15] + feature2[16] + feature2[22] + feature2[23] + feature2[25] + feature2[26] + feature2[29] + feature2[30] + feature2[35] + feature2[37];
		output /= 15d;
		return output;
	}
}
