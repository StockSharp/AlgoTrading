using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long/short MACD expert strategy converted from the MetaTrader example.
/// The strategy opens positions on MACD crossovers and applies fixed stop-loss and take-profit distances.
/// Allowed trade direction can be restricted to long only, short only, or both sides.
/// </summary>
public class LongShortExpertMacdStrategy : Strategy
{
	/// <summary>
	/// Trade directions supported by the strategy.
	/// </summary>
	public enum AllowedPositionType
	{
		/// <summary>
		/// Long trades only.
		/// </summary>
		Long,
		
		/// <summary>
		/// Short trades only.
		/// </summary>
		Short,
		
		/// <summary>
		/// Long and short trades are allowed.
		/// </summary>
		Both
	}

	private readonly StrategyParam<AllowedPositionType> _allowedPosition;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _prevIsMacdAboveSignal;
	private decimal _longStopPrice;
	private decimal _longTakePrice;
	private decimal _shortStopPrice;
	private decimal _shortTakePrice;

	/// <summary>
	/// Initializes a new instance of <see cref="LongShortExpertMacdStrategy"/>.
	/// </summary>
	public LongShortExpertMacdStrategy()
	{
		_allowedPosition = Param(nameof(AllowedPosition), AllowedPositionType.Both)
			.SetDisplay("Allowed Positions", "Permitted trade direction", "General");

		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast MACD EMA length", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(8, 16, 2);

		_slowLength = Param(nameof(SlowLength), 24)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow MACD EMA length", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal EMA", "MACD signal EMA length", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit", "Take profit distance in price points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 150, 10);

		_stopLossPoints = Param(nameof(StopLossPoints), 20)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss", "Stop loss distance in price points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 100, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");

		Volume = 1;
	}

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public AllowedPositionType AllowedPosition
	{
		get => _allowedPosition.Value;
		set => _allowedPosition.Value = value;
	}

	/// <summary>
	/// Fast EMA length used by MACD.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length used by MACD.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal EMA length used by MACD.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private bool CanEnterLong => AllowedPosition != AllowedPositionType.Short;
	private bool CanEnterShort => AllowedPosition != AllowedPositionType.Long;
	private bool AllowReverse => AllowedPosition == AllowedPositionType.Both;

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevIsMacdAboveSignal = null;
		ResetProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		UpdateProtectionLevels();

		var isMacdAboveSignal = macd > signal;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevIsMacdAboveSignal = isMacdAboveSignal;
			return;
		}

		if (TryExitWithProtection(candle))
		{
			_prevIsMacdAboveSignal = isMacdAboveSignal;
			return;
		}

		if (_prevIsMacdAboveSignal is null)
		{
			_prevIsMacdAboveSignal = isMacdAboveSignal;
			return;
		}

		var crossUp = isMacdAboveSignal && _prevIsMacdAboveSignal == false;
		var crossDown = !isMacdAboveSignal && _prevIsMacdAboveSignal == true;

		if (crossUp)
		{
			if (CanEnterLong)
			{
				if (Position < 0)
				{
					if (AllowReverse)
					{
						var volume = Volume + Math.Abs(Position);

						if (volume > 0)
						{
							ResetProtection();
							BuyMarket(volume);
						}
					}
					else
					{
						var volume = Math.Abs(Position);
						if (volume > 0)
						{
							BuyMarket(volume);
							ResetProtection();
						}
					}
				}
				else if (Position == 0)
				{
					if (Volume > 0)
					{
						ResetProtection();
						BuyMarket(Volume);
					}
				}
			}
			else if (Position < 0)
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
				{
					BuyMarket(volume);
					ResetProtection();
				}
			}
		}
		else if (crossDown)
		{
			if (CanEnterShort)
			{
				if (Position > 0)
				{
					if (AllowReverse)
					{
						var volume = Volume + Math.Abs(Position);

						if (volume > 0)
						{
							ResetProtection();
							SellMarket(volume);
						}
					}
					else
					{
						var volume = Math.Abs(Position);
						if (volume > 0)
						{
							SellMarket(volume);
							ResetProtection();
						}
					}
				}
				else if (Position == 0)
				{
					if (Volume > 0)
					{
						ResetProtection();
						SellMarket(Volume);
					}
				}
			}
			else if (Position > 0)
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
				{
					SellMarket(volume);
					ResetProtection();
				}
			}
		}

		_prevIsMacdAboveSignal = isMacdAboveSignal;
	}

	private void UpdateProtectionLevels()
	{
		if (Position > 0)
		{
			var step = GetPriceStep();
			_longStopPrice = StopLossPoints > 0 ? PositionAvgPrice - StopLossPoints * step : 0m;
			_longTakePrice = TakeProfitPoints > 0 ? PositionAvgPrice + TakeProfitPoints * step : 0m;
			_shortStopPrice = 0m;
			_shortTakePrice = 0m;
		}
		else if (Position < 0)
		{
			var step = GetPriceStep();
			_shortStopPrice = StopLossPoints > 0 ? PositionAvgPrice + StopLossPoints * step : 0m;
			_shortTakePrice = TakeProfitPoints > 0 ? PositionAvgPrice - TakeProfitPoints * step : 0m;
			_longStopPrice = 0m;
			_longTakePrice = 0m;
		}
		else
		{
			ResetProtection();
		}
	}

	private bool TryExitWithProtection(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var volume = Math.Abs(Position);

			if (volume > 0)
			{
				if (StopLossPoints > 0 && _longStopPrice > 0m && candle.LowPrice <= _longStopPrice)
				{
					SellMarket(volume);
					ResetProtection();
					return true;
				}

				if (TakeProfitPoints > 0 && _longTakePrice > 0m && candle.HighPrice >= _longTakePrice)
				{
					SellMarket(volume);
					ResetProtection();
					return true;
				}
			}
		}
		else if (Position < 0)
		{
			var volume = Math.Abs(Position);

			if (volume > 0)
			{
				if (StopLossPoints > 0 && _shortStopPrice > 0m && candle.HighPrice >= _shortStopPrice)
				{
					BuyMarket(volume);
					ResetProtection();
					return true;
				}

				if (TakeProfitPoints > 0 && _shortTakePrice > 0m && candle.LowPrice <= _shortTakePrice)
				{
					BuyMarket(volume);
					ResetProtection();
					return true;
				}
			}
		}
		return false;
	}

	private void ResetProtection()
	{
		_longStopPrice = 0m;
		_longTakePrice = 0m;
		_shortStopPrice = 0m;
		_shortTakePrice = 0m;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}
}
