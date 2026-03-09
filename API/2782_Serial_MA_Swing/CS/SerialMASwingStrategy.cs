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
/// Serial moving average swing strategy converted from the MQL SerialMA EA.
/// It opens trades when the custom serial moving average flips across price.
/// </summary>
public class SerialMASwingStrategy : Strategy
{
	/// <summary>
	/// Mode describing how the strategy manages swing positions.
	/// </summary>
	public enum SerialMaOpenedModes
	{
		/// <summary>
		/// Open a new position on every signal, even if a same-direction position exists.
		/// </summary>
		AllSwing,

		/// <summary>
		/// Allow only a single swing position per direction.
		/// </summary>
		SingleSwing,
	}

	private readonly StrategyParam<SerialMaOpenedModes> _openedMode;
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _serialMaSum;
	private int _serialMaCount;
	private decimal? _serialMaPrevDiff;
	private int _serialMaHistory;
	private bool _previousBarHadCross;
	private decimal? _previousMovingAverage;
	private decimal? _previousClose;
	private bool _previousValuesReady;
	private decimal _entryPrice;

	/// <summary>
	/// Defines how many concurrent swing trades are allowed.
	/// </summary>
	public SerialMaOpenedModes OpenedMode
	{
		get => _openedMode.Value;
		set => _openedMode.Value = value;
	}

	/// <summary>
	/// Enables long trades.
	/// </summary>
	public bool EnableBuy
	{
		get => _enableBuy.Value;
		set => _enableBuy.Value = value;
	}

	/// <summary>
	/// Enables short trades.
	/// </summary>
	public bool EnableSell
	{
		get => _enableSell.Value;
		set => _enableSell.Value = value;
	}

	/// <summary>
	/// Reverses every generated signal when set to <c>true</c>.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Default order volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in points (price steps).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in points (price steps).
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SerialMASwingStrategy"/>.
	/// </summary>
	public SerialMASwingStrategy()
	{
		_openedMode = Param(nameof(OpenedMode), SerialMaOpenedModes.SingleSwing)
			.SetDisplay("Opened Mode", "How many swing positions may coexist", "Trading");

		_enableBuy = Param(nameof(EnableBuy), true)
			.SetDisplay("Enable Buy", "Allow opening long positions", "Trading");

		_enableSell = Param(nameof(EnableSell), true)
			.SetDisplay("Enable Sell", "Allow opening short positions", "Trading");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert the generated direction", "Trading");

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Default order volume", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Protective stop distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Target distance in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Data series used for calculations", "General");
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

		_previousBarHadCross = false;
		_previousMovingAverage = null;
		_previousClose = null;
		_previousValuesReady = false;
		_serialMaSum = 0m;
		_serialMaCount = 0;
		_serialMaPrevDiff = null;
		_serialMaHistory = 0;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		Volume = TradeVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Process serial MA inline
		var close = candle.ClosePrice;
		_serialMaHistory++;

		if (_serialMaCount == 0)
		{
			_serialMaSum = close;
			_serialMaCount = 1;
			_serialMaPrevDiff = 0m;
			_previousClose = close;
			_previousValuesReady = _serialMaHistory > 2;
			return;
		}

		_serialMaSum += close;
		_serialMaCount++;
		var movingAverage = _serialMaSum / _serialMaCount;
		var diff = movingAverage - close;
		var isCross = false;
		var signalFromCross = 0;

		if (_serialMaPrevDiff.HasValue && diff * _serialMaPrevDiff.Value < 0m)
		{
			isCross = true;
			signalFromCross = diff < 0m ? 1 : -1;
			movingAverage = close;
			diff = 0m;
			_serialMaSum = close;
			_serialMaCount = 1;
		}

		_serialMaPrevDiff = diff;

		if (!_previousValuesReady)
		{
			_previousBarHadCross = isCross;
			_previousMovingAverage = movingAverage;
			_previousClose = close;
			_previousValuesReady = _serialMaHistory > 2;
			return;
		}

		HandleProtectiveLevels(candle);

		var signal = signalFromCross != 0 ? signalFromCross : GetPendingSignal();
		if (signal != 0)
		{
			var openLong = signal > 0;
			var openShort = signal < 0;

			if (ReverseSignals)
			{
				(openLong, openShort) = (openShort, openLong);
			}

			if (!EnableBuy)
				openLong = false;

			if (!EnableSell)
				openShort = false;

			if (openLong)
				ExecuteLongEntry();

			if (openShort)
				ExecuteShortEntry();
		}

		_previousBarHadCross = isCross;
		_previousMovingAverage = movingAverage;
		_previousClose = close;
	}

	private void ExecuteLongEntry()
	{
		if (TradeVolume <= 0m)
			return;

		// Close short exposure before building a long swing.
		if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}

		// Add a new long swing if allowed by the opening mode.
		if (OpenedMode == SerialMaOpenedModes.AllSwing || Position <= 0m)
		{
			BuyMarket(TradeVolume);
		}
	}

	private void ExecuteShortEntry()
	{
		if (TradeVolume <= 0m)
			return;

		// Close long exposure before building a short swing.
		if (Position > 0m)
		{
			SellMarket(Position);
		}

		// Add a new short swing if allowed by the opening mode.
		if (OpenedMode == SerialMaOpenedModes.AllSwing || Position >= 0m)
		{
			SellMarket(TradeVolume);
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		if (trade?.Trade == null) return;
		if (Position != 0m && _entryPrice == 0m)
			_entryPrice = trade.Trade.Price;
		if (Position == 0m)
			_entryPrice = 0m;
	}

	private void HandleProtectiveLevels(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			return;

		if (Position > 0m)
		{
			if (StopLossPoints > 0m)
			{
				var stopPrice = _entryPrice - StopLossPoints * step;
				// Exit on stop loss for a long position.
				if (candle.LowPrice <= stopPrice)
				{
					SellMarket(Position);
					return;
				}
			}

			if (TakeProfitPoints > 0m)
			{
				var targetPrice = _entryPrice + TakeProfitPoints * step;
				// Lock in profit once the target is reached.
				if (candle.HighPrice >= targetPrice)
				{
					SellMarket(Position);
				}
			}
		}
		else if (Position < 0m)
		{
			var absPosition = Math.Abs(Position);

			if (StopLossPoints > 0m)
			{
				var stopPrice = _entryPrice + StopLossPoints * step;
				// Exit on stop loss for a short position.
				if (candle.HighPrice >= stopPrice)
				{
					BuyMarket(absPosition);
					return;
				}
			}

			if (TakeProfitPoints > 0m)
			{
				var targetPrice = _entryPrice - TakeProfitPoints * step;
				// Capture profit when the downside target is achieved.
				if (candle.LowPrice <= targetPrice)
				{
					BuyMarket(absPosition);
				}
			}
		}
	}

	private int GetPendingSignal()
	{
		if (!_previousBarHadCross || _previousMovingAverage == null || _previousClose == null)
			return 0;

		if (_previousClose > _previousMovingAverage)
			return 1;

		if (_previousClose < _previousMovingAverage)
			return -1;

		return 0;
	}

}
