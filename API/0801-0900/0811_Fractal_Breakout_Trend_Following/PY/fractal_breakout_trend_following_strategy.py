import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

TEETH_LENGTH = 8
TEETH_SHIFT = 5
PERCENTILE_LOOKBACK = 100

class fractal_breakout_trend_following_strategy(Strategy):
    """
    Fractal Breakout Trend Following strategy.
    An up fractal (a high above the two highs on each side) that lies above the Alligator teeth (SMMA 8 of the median price, shifted
    5 bars) arms a buy stop at its high. While flat, inside the TradeStart..TradeStop window and with the AtrPeriod average of the
    ATR percentile rank over the last 100 bars below AtrThreshold, a candle trading through that level goes long. The stop is the
    higher of the StopLossPercent (a fraction) stop below the entry and the latest down fractal that lies below the teeth.
    """

    def __init__(self):
        super(fractal_breakout_trend_following_strategy, self).__init__()
        self._stop_loss_percent = self.Param("StopLossPercent", 0.03).SetNotNegative().SetDisplay("Stop Loss", "Stop loss distance as a fraction of the entry price", "Risk")
        self._atr_threshold = self.Param("AtrThreshold", 50.0).SetDisplay("ATR Threshold", "Maximum averaged ATR percentile for entries", "Volatility")
        self._atr_period = self.Param("AtrPeriod", 5).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period, also the averaging period of its percentile", "Volatility")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._trade_start = self.Param("TradeStart", DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("Trade Start", "Start of the trading window", "Time")
        self._trade_stop = self.Param("TradeStop", DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("Trade Stop", "End of the trading window", "Time")
        self._smma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._highs = []
        self._lows = []
        self._teeth_source = []
        self._atr_values = []
        self._percentiles = []
        self._buy_level = None
        self._down_fractal = None
        self._entry_price = 0.0

    def OnReseted(self):
        super(fractal_breakout_trend_following_strategy, self).OnReseted()
        self._reset_state()
        self._smma = None

    def OnStarted2(self, time):
        super(fractal_breakout_trend_following_strategy, self).OnStarted2(time)

        self._reset_state()
        self._smma = SmoothedMovingAverage()
        self._smma.Length = TEETH_LENGTH

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        median = (high + low) / 2.0
        smma = process_float(self._smma, Decimal(median), candle.OpenTime, True)

        self._highs.append(high)
        self._lows.append(low)
        self._teeth_source.append(float(smma) if self._smma.IsFormed else 0.0)
        self._trim(self._highs, 5)
        self._trim(self._lows, 5)
        self._trim(self._teeth_source, TEETH_SHIFT + 3)

        avg_percentile = self._update_percentile(float(atr_value))

        self._update_fractals()

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            stop = self._entry_price * (1.0 - float(self._stop_loss_percent.Value))
            if self._down_fractal is not None and self._down_fractal > stop:
                stop = self._down_fractal
            if low <= stop:
                self.SellMarket(self.Position)
            return

        if self.Position < 0 or self._buy_level is None or avg_percentile is None:
            return

        time = candle.OpenTime
        if time.CompareTo(self._trade_start.Value.UtcDateTime) < 0 or time.CompareTo(self._trade_stop.Value.UtcDateTime) >= 0:
            return

        if avg_percentile < float(self._atr_threshold.Value) and high >= self._buy_level:
            self._entry_price = max(self._buy_level, float(candle.OpenPrice))
            self._buy_level = None
            self.BuyMarket(self.Volume)

    def _trim(self, items, size):
        while len(items) > size:
            del items[0]

    def _update_percentile(self, atr):
        rank = 0.0
        has_history = len(self._atr_values) >= PERCENTILE_LOOKBACK

        if has_history:
            below = sum(1 for value in self._atr_values if value < atr)
            rank = 100.0 * below / len(self._atr_values)

        self._atr_values.append(atr)
        self._trim(self._atr_values, PERCENTILE_LOOKBACK)

        if not has_history:
            return None

        period = self._atr_period.Value
        self._percentiles.append(rank)
        self._trim(self._percentiles, period)

        if len(self._percentiles) < period:
            return None

        return sum(self._percentiles) / len(self._percentiles)

    def _update_fractals(self):
        if len(self._highs) < 5 or len(self._teeth_source) < TEETH_SHIFT + 3:
            return

        # The middle of the last five bars is the fractal bar; the teeth on it are the SMMA from 5 bars earlier.
        teeth = self._teeth_source[len(self._teeth_source) - 3 - TEETH_SHIFT]
        if teeth <= 0:
            return

        h = self._highs
        if h[2] > h[0] and h[2] > h[1] and h[2] > h[3] and h[2] > h[4] and h[2] > teeth:
            self._buy_level = h[2]

        l = self._lows
        if l[2] < l[0] and l[2] < l[1] and l[2] < l[3] and l[2] < l[4] and l[2] < teeth:
            self._down_fractal = l[2]

    def CreateClone(self):
        return fractal_breakout_trend_following_strategy()
