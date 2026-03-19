import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class forex_sky_strategy(Strategy):
    """
    Forex Sky: MACD zero-line cross with EMA trend filter and ATR-based exits.
    """

    def __init__(self):
        super(forex_sky_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast EMA", "MACD fast period", "Indicators")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("Slow EMA", "MACD slow period", "Indicators")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "Trend filter", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Indicators")

        self._prev_macd = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(forex_sky_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(forex_sky_strategy, self).OnStarted(time)

        self._prev_macd = 0.0
        self._entry_price = 0.0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_length.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_val)
        slow_val = float(slow_val)
        ema_val = float(ema_val)
        atr_val = float(atr_val)

        if atr_val <= 0:
            return

        macd = fast_val - slow_val
        close = float(candle.ClosePrice)

        if self._prev_macd == 0:
            self._prev_macd = macd
            return

        if self.Position > 0:
            if (close >= self._entry_price + atr_val * 3.0 or
                close <= self._entry_price - atr_val * 2.0 or
                (macd < 0 and self._prev_macd >= 0)):
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if (close <= self._entry_price - atr_val * 3.0 or
                close >= self._entry_price + atr_val * 2.0 or
                (macd > 0 and self._prev_macd <= 0)):
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if macd > 0 and self._prev_macd <= 0 and close > ema_val:
                self._entry_price = close
                self.BuyMarket()
            elif macd < 0 and self._prev_macd >= 0 and close < ema_val:
                self._entry_price = close
                self.SellMarket()

        self._prev_macd = macd

    def CreateClone(self):
        return forex_sky_strategy()
