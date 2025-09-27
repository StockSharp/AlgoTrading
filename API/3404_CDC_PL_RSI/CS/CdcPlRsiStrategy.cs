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
/// Strategy that combines candlestick reversal patterns with RSI confirmation.
/// Long trades rely on the Piercing Line pattern with oversold RSI readings,
/// while short trades use the Dark Cloud Cover pattern confirmed by an overbought RSI.
/// </summary>
public class CdcPlRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _bodyAveragePeriod;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _bodyAverage;
	private SimpleMovingAverage _closeAverage;

	private CandleSnapshot _previous;
	private CandleSnapshot _previous2;

	private decimal _rsiPrevious;
	private decimal _rsiPrevious2;
	private bool _hasRsiPrevious;
	private bool _hasRsiPrevious2;

	/// <summary>
	/// RSI look-back period.
	/// </summary>
	public int RsiPeriod
	{
	        get => _rsiPeriod.Value;
	        set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars used to smooth candle body size and close price trend.
	/// </summary>
	public int BodyAveragePeriod
	{
	        get => _bodyAveragePeriod.Value;
	        set => _bodyAveragePeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for all calculations.
	/// </summary>
	public DataType CandleType
	{
	        get => _candleType.Value;
	        set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CdcPlRsiStrategy"/> class.
	/// </summary>
	public CdcPlRsiStrategy()
	{
	        _rsiPeriod = Param(nameof(RsiPeriod), 20)
	                .SetGreaterThanZero()
	                .SetDisplay("RSI Period", "Number of bars for RSI calculation", "Indicators")
	                .SetCanOptimize(true)
	                .SetOptimize(10, 40, 5);

	        _bodyAveragePeriod = Param(nameof(BodyAveragePeriod), 14)
	                .SetGreaterThanZero()
	                .SetDisplay("Body Average", "Period for candle body average and close trend", "Indicators")
	                .SetCanOptimize(true)
	                .SetOptimize(10, 30, 5);

	        _candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
	                .SetDisplay("Candle Type", "Type of candles used by the strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	        => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
	        base.OnReseted();

	        _previous = null;
	        _previous2 = null;
	        _rsiPrevious = 0m;
	        _rsiPrevious2 = 0m;
	        _hasRsiPrevious = false;
	        _hasRsiPrevious2 = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	        base.OnStarted(time);

	        _rsi = new RelativeStrengthIndex
	        {
	                Length = RsiPeriod
	        };

	        _bodyAverage = new SimpleMovingAverage
	        {
	                Length = BodyAveragePeriod
	        };

	        _closeAverage = new SimpleMovingAverage
	        {
	                Length = BodyAveragePeriod
	        };

	        var subscription = SubscribeCandles(CandleType);
	        subscription
	                .Bind(_rsi, ProcessCandle)
	                .Start();

	        var area = CreateChartArea();
	        if (area != null)
	        {
	                DrawCandles(area, subscription);
	                DrawIndicator(area, _rsi);
	                DrawIndicator(area, _closeAverage);
	                DrawOwnTrades(area);
	        }

	        StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal currentRsi)
	{
	        // Skip any incomplete candles to avoid double counting partially formed bars.
	        if (candle.State != CandleStates.Finished)
	                return;

	        var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
	        var bodyValue = _bodyAverage.Process(new DecimalIndicatorValue(_bodyAverage, body, candle.OpenTime));

	        if (bodyValue is not DecimalIndicatorValue { IsFinal: true, Value: var bodyAverage })
	        {
	                // Store RSI history even when the body average is still warming up.
	                ShiftRsi(currentRsi);
	                return;
	        }

	        var closeValue = _closeAverage.Process(new DecimalIndicatorValue(_closeAverage, candle.ClosePrice, candle.OpenTime));

	        if (closeValue is not DecimalIndicatorValue { IsFinal: true, Value: var closeAverage })
	        {
	                // Wait for the close average to provide a stable trend reference.
	                ShiftRsi(currentRsi);
	                return;
	        }

	        var currentSnapshot = new CandleSnapshot
	        {
	                Open = candle.OpenPrice,
	                High = candle.HighPrice,
	                Low = candle.LowPrice,
	                Close = candle.ClosePrice,
	                BodyAverage = bodyAverage,
	                CloseAverage = closeAverage,
	        };

	        if (IsFormedAndOnlineAndAllowTrading())
	        {
	                ProcessSignals();
	        }

	        ShiftCandles(currentSnapshot);
	        ShiftRsi(currentRsi);
	}

	private void ProcessSignals()
	{
	        if (_previous == null || _previous2 == null || !_hasRsiPrevious)
	                return;

	        var rsi1 = _rsiPrevious;
	        var hasRsi2 = _hasRsiPrevious2;
	        var rsi2 = _rsiPrevious2;

	        var longSignal = IsPiercingPattern(_previous, _previous2) && rsi1 < 40m;
	        var shortSignal = IsDarkCloudCoverPattern(_previous, _previous2) && rsi1 > 60m;

	        // Exit a long when RSI falls back from an overbought zone or recovers from oversold levels.
	        var exitLong = Position > 0 && hasRsi2 &&
	                ((rsi1 < 70m && rsi2 > 70m) || (rsi1 < 30m && rsi2 > 30m));
	        // Exit a short when RSI rises above oversold levels or drops below overbought levels.
	        var exitShort = Position < 0 && hasRsi2 &&
	                ((rsi1 > 30m && rsi2 < 30m) || (rsi1 > 70m && rsi2 < 70m));

	        if (exitLong || exitShort)
	        {
	                ClosePosition();
	                return;
	        }

	        var volume = Volume ?? 1m;

	        if (longSignal)
	        {
	                if (Position < 0)
	                {
	                        BuyMarket(volume + Math.Abs(Position));
	                }
	                else if (Position == 0)
	                {
	                        BuyMarket(volume);
	                }
	        }
	        else if (shortSignal)
	        {
	                if (Position > 0)
	                {
	                        SellMarket(volume + Math.Abs(Position));
	                }
	                else if (Position == 0)
	                {
	                        SellMarket(volume);
	                }
	        }
	}

	private void ShiftCandles(CandleSnapshot snapshot)
	{
	        _previous2 = _previous;
	        _previous = snapshot;
	}

	private void ShiftRsi(decimal currentRsi)
	{
	        if (_hasRsiPrevious)
	        {
	                _rsiPrevious2 = _rsiPrevious;
	                _hasRsiPrevious2 = true;
	        }

	        _rsiPrevious = currentRsi;
	        _hasRsiPrevious = true;
	}

	private static bool IsPiercingPattern(CandleSnapshot latest, CandleSnapshot older)
	{
	        var latestBody = latest.Close - latest.Open;
	        var olderBody = older.Open - older.Close;
	        var longWhite = latestBody > latest.BodyAverage;
	        var longBlack = olderBody > latest.BodyAverage;
	        var closeInside = older.Close > latest.Close && latest.Close < older.Open;
	        var downTrend = ((older.Open + older.Close) / 2m) < older.CloseAverage;
	        var gapOpen = latest.Open < older.Low;
	        return longWhite && longBlack && closeInside && downTrend && gapOpen;
	}

	private static bool IsDarkCloudCoverPattern(CandleSnapshot latest, CandleSnapshot older)
	{
	        var olderBody = older.Close - older.Open;
	        var longWhite = olderBody > latest.BodyAverage;
	        var closeWithin = latest.Close < older.Close && latest.Close > older.Open;
	        var upTrend = ((older.Open + older.Close) / 2m) > latest.CloseAverage;
	        var gapOpen = latest.Open > older.High;
	        return longWhite && closeWithin && upTrend && gapOpen;
	}

	private sealed class CandleSnapshot
	{
	        public decimal Open { get; init; }
	        public decimal High { get; init; }
	        public decimal Low { get; init; }
	        public decimal Close { get; init; }
	        public decimal BodyAverage { get; init; }
	        public decimal CloseAverage { get; init; }
	}
}

