using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-symbol perceptron strategy that evaluates the Acceleration/Deceleration oscillator on three markets.
/// </summary>
public class PerceptronMultStrategy : Strategy
{
	private readonly StrategyParam<Security> _firstSecurity;
	private readonly StrategyParam<Security> _secondSecurity;
	private readonly StrategyParam<Security> _thirdSecurity;

	private readonly StrategyParam<decimal> _firstOrderVolume;
	private readonly StrategyParam<decimal> _secondOrderVolume;
	private readonly StrategyParam<decimal> _thirdOrderVolume;

	private readonly StrategyParam<int> _firstWeight1;
	private readonly StrategyParam<int> _firstWeight2;
	private readonly StrategyParam<int> _firstWeight3;
	private readonly StrategyParam<int> _firstWeight4;

	private readonly StrategyParam<int> _secondWeight1;
	private readonly StrategyParam<int> _secondWeight2;
	private readonly StrategyParam<int> _secondWeight3;
	private readonly StrategyParam<int> _secondWeight4;

	private readonly StrategyParam<int> _thirdWeight1;
	private readonly StrategyParam<int> _thirdWeight2;
	private readonly StrategyParam<int> _thirdWeight3;
	private readonly StrategyParam<int> _thirdWeight4;

	private readonly StrategyParam<decimal> _firstStopLossPoints;
	private readonly StrategyParam<decimal> _firstTakeProfitPoints;
	private readonly StrategyParam<decimal> _secondStopLossPoints;
	private readonly StrategyParam<decimal> _secondTakeProfitPoints;
	private readonly StrategyParam<decimal> _thirdStopLossPoints;
	private readonly StrategyParam<decimal> _thirdTakeProfitPoints;

	private readonly StrategyParam<DataType> _candleType;

	private SymbolContext _firstContext;
	private SymbolContext _secondContext;
	private SymbolContext _thirdContext;

	private sealed class SymbolContext
	{
		public AwesomeOscillator Ao = null!;
		public SimpleMovingAverage AoAverage = null!;
		public readonly decimal?[] AcValues = new decimal?[22];
		public int ValuesStored;
		public decimal PointValue;
		public decimal? StopPrice;
		public decimal? TakePrice;

		public void ResetBuffers()
		{
			Array.Clear(AcValues, 0, AcValues.Length);
			ValuesStored = 0;
		}

		public void ResetProtection()
		{
			StopPrice = null;
			TakePrice = null;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PerceptronMultStrategy"/> class.
	/// </summary>
	public PerceptronMultStrategy()
	{
		_firstSecurity = Param(nameof(FirstSecurity), default(Security))
			.SetDisplay("First Security", "Primary instrument processed by the perceptron", "General");

		_secondSecurity = Param(nameof(SecondSecurity), default(Security))
			.SetDisplay("Second Security", "Secondary instrument processed by the perceptron", "General");

		_thirdSecurity = Param(nameof(ThirdSecurity), default(Security))
			.SetDisplay("Third Security", "Third instrument processed by the perceptron", "General");

		_firstOrderVolume = Param(nameof(FirstOrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("First Volume", "Order volume for the first security", "Trading");

		_secondOrderVolume = Param(nameof(SecondOrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Second Volume", "Order volume for the second security", "Trading");

		_thirdOrderVolume = Param(nameof(ThirdOrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Third Volume", "Order volume for the third security", "Trading");

		_firstWeight1 = Param(nameof(FirstWeight1), 100)
			.SetDisplay("First Weight 1", "First perceptron weight for the primary instrument", "Perceptron");

		_firstWeight2 = Param(nameof(FirstWeight2), 20)
			.SetDisplay("First Weight 2", "Second perceptron weight for the primary instrument", "Perceptron");

		_firstWeight3 = Param(nameof(FirstWeight3), 60)
			.SetDisplay("First Weight 3", "Third perceptron weight for the primary instrument", "Perceptron");

		_firstWeight4 = Param(nameof(FirstWeight4), 40)
			.SetDisplay("First Weight 4", "Fourth perceptron weight for the primary instrument", "Perceptron");

		_secondWeight1 = Param(nameof(SecondWeight1), 100)
			.SetDisplay("Second Weight 1", "First perceptron weight for the secondary instrument", "Perceptron");

		_secondWeight2 = Param(nameof(SecondWeight2), 20)
			.SetDisplay("Second Weight 2", "Second perceptron weight for the secondary instrument", "Perceptron");

		_secondWeight3 = Param(nameof(SecondWeight3), 60)
			.SetDisplay("Second Weight 3", "Third perceptron weight for the secondary instrument", "Perceptron");

		_secondWeight4 = Param(nameof(SecondWeight4), 40)
			.SetDisplay("Second Weight 4", "Fourth perceptron weight for the secondary instrument", "Perceptron");

		_thirdWeight1 = Param(nameof(ThirdWeight1), 100)
			.SetDisplay("Third Weight 1", "First perceptron weight for the third instrument", "Perceptron");

		_thirdWeight2 = Param(nameof(ThirdWeight2), 20)
			.SetDisplay("Third Weight 2", "Second perceptron weight for the third instrument", "Perceptron");

		_thirdWeight3 = Param(nameof(ThirdWeight3), 60)
			.SetDisplay("Third Weight 3", "Third perceptron weight for the third instrument", "Perceptron");

		_thirdWeight4 = Param(nameof(ThirdWeight4), 40)
			.SetDisplay("Third Weight 4", "Fourth perceptron weight for the third instrument", "Perceptron");

		_firstStopLossPoints = Param(nameof(FirstStopLossPoints), 40m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("First Stop Loss", "Stop-loss distance in points for the first security", "Risk");

		_firstTakeProfitPoints = Param(nameof(FirstTakeProfitPoints), 95m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("First Take Profit", "Take-profit distance in points for the first security", "Risk");

		_secondStopLossPoints = Param(nameof(SecondStopLossPoints), 40m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Second Stop Loss", "Stop-loss distance in points for the second security", "Risk");

		_secondTakeProfitPoints = Param(nameof(SecondTakeProfitPoints), 95m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Second Take Profit", "Take-profit distance in points for the second security", "Risk");

		_thirdStopLossPoints = Param(nameof(ThirdStopLossPoints), 40m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Third Stop Loss", "Stop-loss distance in points for the third security", "Risk");

		_thirdTakeProfitPoints = Param(nameof(ThirdTakeProfitPoints), 95m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Third Take Profit", "Take-profit distance in points for the third security", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type shared by all instruments", "Data");
	}

	/// <summary>
	/// Primary security traded by the strategy.
	/// </summary>
	public Security FirstSecurity
	{
		get => _firstSecurity.Value;
		set => _firstSecurity.Value = value;
	}

	/// <summary>
	/// Secondary security traded by the strategy.
	/// </summary>
	public Security SecondSecurity
	{
		get => _secondSecurity.Value;
		set => _secondSecurity.Value = value;
	}

	/// <summary>
	/// Third security traded by the strategy.
	/// </summary>
	public Security ThirdSecurity
	{
		get => _thirdSecurity.Value;
		set => _thirdSecurity.Value = value;
	}

	/// <summary>
	/// Order volume for the first security.
	/// </summary>
	public decimal FirstOrderVolume
	{
		get => _firstOrderVolume.Value;
		set => _firstOrderVolume.Value = value;
	}

	/// <summary>
	/// Order volume for the second security.
	/// </summary>
	public decimal SecondOrderVolume
	{
		get => _secondOrderVolume.Value;
		set => _secondOrderVolume.Value = value;
	}

	/// <summary>
	/// Order volume for the third security.
	/// </summary>
	public decimal ThirdOrderVolume
	{
		get => _thirdOrderVolume.Value;
		set => _thirdOrderVolume.Value = value;
	}

	/// <summary>
	/// First perceptron weight for the primary instrument.
	/// </summary>
	public int FirstWeight1
	{
		get => _firstWeight1.Value;
		set => _firstWeight1.Value = value;
	}

	/// <summary>
	/// Second perceptron weight for the primary instrument.
	/// </summary>
	public int FirstWeight2
	{
		get => _firstWeight2.Value;
		set => _firstWeight2.Value = value;
	}

	/// <summary>
	/// Third perceptron weight for the primary instrument.
	/// </summary>
	public int FirstWeight3
	{
		get => _firstWeight3.Value;
		set => _firstWeight3.Value = value;
	}

	/// <summary>
	/// Fourth perceptron weight for the primary instrument.
	/// </summary>
	public int FirstWeight4
	{
		get => _firstWeight4.Value;
		set => _firstWeight4.Value = value;
	}

	/// <summary>
	/// First perceptron weight for the secondary instrument.
	/// </summary>
	public int SecondWeight1
	{
		get => _secondWeight1.Value;
		set => _secondWeight1.Value = value;
	}

	/// <summary>
	/// Second perceptron weight for the secondary instrument.
	/// </summary>
	public int SecondWeight2
	{
		get => _secondWeight2.Value;
		set => _secondWeight2.Value = value;
	}

	/// <summary>
	/// Third perceptron weight for the secondary instrument.
	/// </summary>
	public int SecondWeight3
	{
		get => _secondWeight3.Value;
		set => _secondWeight3.Value = value;
	}

	/// <summary>
	/// Fourth perceptron weight for the secondary instrument.
	/// </summary>
	public int SecondWeight4
	{
		get => _secondWeight4.Value;
		set => _secondWeight4.Value = value;
	}

	/// <summary>
	/// First perceptron weight for the third instrument.
	/// </summary>
	public int ThirdWeight1
	{
		get => _thirdWeight1.Value;
		set => _thirdWeight1.Value = value;
	}

	/// <summary>
	/// Second perceptron weight for the third instrument.
	/// </summary>
	public int ThirdWeight2
	{
		get => _thirdWeight2.Value;
		set => _thirdWeight2.Value = value;
	}

	/// <summary>
	/// Third perceptron weight for the third instrument.
	/// </summary>
	public int ThirdWeight3
	{
		get => _thirdWeight3.Value;
		set => _thirdWeight3.Value = value;
	}

	/// <summary>
	/// Fourth perceptron weight for the third instrument.
	/// </summary>
	public int ThirdWeight4
	{
		get => _thirdWeight4.Value;
		set => _thirdWeight4.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points for the first security.
	/// </summary>
	public decimal FirstStopLossPoints
	{
		get => _firstStopLossPoints.Value;
		set => _firstStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points for the first security.
	/// </summary>
	public decimal FirstTakeProfitPoints
	{
		get => _firstTakeProfitPoints.Value;
		set => _firstTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points for the second security.
	/// </summary>
	public decimal SecondStopLossPoints
	{
		get => _secondStopLossPoints.Value;
		set => _secondStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points for the second security.
	/// </summary>
	public decimal SecondTakeProfitPoints
	{
		get => _secondTakeProfitPoints.Value;
		set => _secondTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points for the third security.
	/// </summary>
	public decimal ThirdStopLossPoints
	{
		get => _thirdStopLossPoints.Value;
		set => _thirdStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points for the third security.
	/// </summary>
	public decimal ThirdTakeProfitPoints
	{
		get => _thirdTakeProfitPoints.Value;
		set => _thirdTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type shared by all instruments.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (FirstSecurity is not null)
			yield return (FirstSecurity, CandleType);

		if (SecondSecurity is not null)
			yield return (SecondSecurity, CandleType);

		if (ThirdSecurity is not null)
			yield return (ThirdSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_firstContext?.ResetBuffers();
		_firstContext?.ResetProtection();
		_secondContext?.ResetBuffers();
		_secondContext?.ResetProtection();
		_thirdContext?.ResetBuffers();
		_thirdContext?.ResetProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_firstContext = CreateContext(FirstSecurity);
		_secondContext = CreateContext(SecondSecurity);
		_thirdContext = CreateContext(ThirdSecurity);

		if (_firstContext != null && FirstSecurity != null)
		{
			var subscription = SubscribeCandles(CandleType, security: FirstSecurity);
			subscription.Bind(ProcessFirstCandle).Start();
		}

		if (_secondContext != null && SecondSecurity != null)
		{
			var subscription = SubscribeCandles(CandleType, security: SecondSecurity);
			subscription.Bind(ProcessSecondCandle).Start();
		}

		if (_thirdContext != null && ThirdSecurity != null)
		{
			var subscription = SubscribeCandles(CandleType, security: ThirdSecurity);
			subscription.Bind(ProcessThirdCandle).Start();
		}
	}

	private SymbolContext CreateContext(Security security)
	{
		if (security is null)
			return null;

		var point = security.PriceStep;
		if (point <= 0m)
			point = security.MinStep ?? 1m;

		var context = new SymbolContext
		{
			Ao = new AwesomeOscillator
			{
				ShortPeriod = 5,
				LongPeriod = 34
			},
			AoAverage = new SimpleMovingAverage
			{
				Length = 5
			},
			PointValue = point
		};

		context.ResetBuffers();
		context.ResetProtection();
		return context;
	}

	private void ProcessFirstCandle(ICandleMessage candle)
	{
		ProcessCandle(candle, _firstContext, FirstSecurity, FirstOrderVolume, FirstStopLossPoints, FirstTakeProfitPoints, FirstWeight1, FirstWeight2, FirstWeight3, FirstWeight4);
	}

	private void ProcessSecondCandle(ICandleMessage candle)
	{
		ProcessCandle(candle, _secondContext, SecondSecurity, SecondOrderVolume, SecondStopLossPoints, SecondTakeProfitPoints, SecondWeight1, SecondWeight2, SecondWeight3, SecondWeight4);
	}

	private void ProcessThirdCandle(ICandleMessage candle)
	{
		ProcessCandle(candle, _thirdContext, ThirdSecurity, ThirdOrderVolume, ThirdStopLossPoints, ThirdTakeProfitPoints, ThirdWeight1, ThirdWeight2, ThirdWeight3, ThirdWeight4);
	}

	private void ProcessCandle(ICandleMessage candle, SymbolContext context, Security security, decimal orderVolume, decimal stopLossPoints, decimal takeProfitPoints, int weight1, int weight2, int weight3, int weight4)
	{
		if (context is null || security is null)
			return;

		// Check whether existing positions should be closed by protective levels.
		if (HandleProtection(candle, context, security))
			return;

		if (candle.State != CandleStates.Finished)
			return;

		var acValue = CalculateAcceleration(candle, context);
		if (acValue is null)
			return;

		UpdateAcBuffer(context, acValue.Value);

		var signal = CalculatePerceptron(context, weight1, weight2, weight3, weight4);
		if (signal is null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (PositionBy(security) != 0m)
			return;

		if (signal > 0m)
			OpenPosition(security, context, true, orderVolume, stopLossPoints, takeProfitPoints, candle.ClosePrice);
		else if (signal < 0m)
			OpenPosition(security, context, false, orderVolume, stopLossPoints, takeProfitPoints, candle.ClosePrice);
	}

	private decimal? CalculateAcceleration(ICandleMessage candle, SymbolContext context)
	{
		var aoValue = context.Ao.Process(candle.HighPrice, candle.LowPrice);
		if (!aoValue.IsFinal)
			return null;

		var ao = aoValue.GetValue<decimal>();
		var averageValue = context.AoAverage.Process(ao);
		if (!averageValue.IsFinal)
			return null;

		var average = averageValue.GetValue<decimal>();
		return ao - average;
	}

	private void UpdateAcBuffer(SymbolContext context, decimal value)
	{
		var buffer = context.AcValues;
		for (var i = buffer.Length - 1; i > 0; i--)
			buffer[i] = buffer[i - 1];

		buffer[0] = value;

		if (context.ValuesStored < buffer.Length)
			context.ValuesStored++;
	}

	private decimal? CalculatePerceptron(SymbolContext context, int w1, int w2, int w3, int w4)
	{
		if (context.ValuesStored < context.AcValues.Length)
			return null;

		if (context.AcValues[0] is not decimal a1 ||
			context.AcValues[7] is not decimal a2 ||
			context.AcValues[14] is not decimal a3 ||
			context.AcValues[21] is not decimal a4)
			return null;

		var weight1 = w1 - 100m;
		var weight2 = w2 - 100m;
		var weight3 = w3 - 100m;
		var weight4 = w4 - 100m;

		return weight1 * a1 + weight2 * a2 + weight3 * a3 + weight4 * a4;
	}

	private void OpenPosition(Security security, SymbolContext context, bool isLong, decimal orderVolume, decimal stopLossPoints, decimal takeProfitPoints, decimal referencePrice)
	{
		if (orderVolume <= 0m)
			return;

		if (isLong)
		{
			BuyMarket(orderVolume, security);
		}
		else
		{
			SellMarket(orderVolume, security);
		}

		var stopDistance = stopLossPoints > 0m ? stopLossPoints * context.PointValue : (decimal?)null;
		var takeDistance = takeProfitPoints > 0m ? takeProfitPoints * context.PointValue : (decimal?)null;

		context.StopPrice = stopDistance.HasValue
			? referencePrice + (isLong ? -stopDistance.Value : stopDistance.Value)
			: null;

		context.TakePrice = takeDistance.HasValue
			? referencePrice + (isLong ? takeDistance.Value : -takeDistance.Value)
			: null;
	}

	private bool HandleProtection(ICandleMessage candle, SymbolContext context, Security security)
	{
		var position = PositionBy(security);
		if (position == 0m)
		{
			context.ResetProtection();
			return false;
		}

		if (position > 0m)
		{
			if (context.StopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(position, security);
				context.ResetProtection();
				return true;
			}

			if (context.TakePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(position, security);
				context.ResetProtection();
				return true;
			}
		}
		else
		{
			var volume = Math.Abs(position);

			if (context.StopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(volume, security);
				context.ResetProtection();
				return true;
			}

			if (context.TakePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(volume, security);
				context.ResetProtection();
				return true;
			}
		}

		return false;
	}

	private decimal PositionBy(Security security)
	{
		return GetPositionValue(security, Portfolio) ?? 0m;
	}
}