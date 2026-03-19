import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class heiken_ashi_smoothed_mtf_strategy(Strategy):
    """
    Heiken Ashi Smoothed MTF: EMA trend with RSI filter and ATR stops.
    Buys on EMA cross up with RSI > 50. Sells on EMA cross down with RSI < 50.
    Uses ATR for dynamic SL/TP.
    """

    def __init__(self):
        super(heiken_ashi_smoothed_mtf_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "Trend filter", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_close = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(heiken_ashi_smoothed_mtf_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(heiken_ashi_smoothed_mtf_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, rsi, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_val, rsi_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ema = float(ema_val)
        rsi = float(rsi_val)
        atr = float(atr_val)

        if self._prev_close == 0.0 or atr <= 0:
            self._prev_close = close
            return

        if self.Position > 0:
            if close < ema or close <= self._entry_price - atr * 1.5 or close >= self._entry_price + atr * 2.5:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close > ema or close >= self._entry_price + atr * 1.5 or close <= self._entry_price - atr * 2.5:
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if close > ema and self._prev_close <= ema and rsi > 50:
                self._entry_price = close
                self.BuyMarket()
            elif close < ema and self._prev_close >= ema and rsi < 50:
                self._entry_price = close
                self.SellMarket()

        self._prev_close = close

    def CreateClone(self):
        return heiken_ashi_smoothed_mtf_strategy()
