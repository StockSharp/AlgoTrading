import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class multi_lot_scalper_strategy(Strategy):
    """
    Multi Lot Scalper: MACD slope scalping with EMA filter and ATR stops.
    """

    def __init__(self):
        super(multi_lot_scalper_strategy, self).__init__()
        self._fast_ema = self.Param("FastEmaLength", 12).SetDisplay("Fast EMA", "Fast EMA", "Indicators")
        self._slow_ema = self.Param("SlowEmaLength", 26).SetDisplay("Slow EMA", "Slow EMA", "Indicators")
        self._ema_filter = self.Param("EmaFilterLength", 50).SetDisplay("EMA Filter", "Trend filter", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR", "ATR period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_macd = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(multi_lot_scalper_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(multi_lot_scalper_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_ema.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_ema.Value
        ema_filter = ExponentialMovingAverage()
        ema_filter.Length = self._ema_filter.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, ema_filter, atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_filter)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast_val)
        slow = float(slow_val)
        ema = float(ema_val)
        atr = float(atr_val)
        macd = fast - slow
        close = float(candle.ClosePrice)
        if self._prev_macd == 0 or atr <= 0:
            self._prev_macd = macd
            return
        if self.Position > 0:
            if close >= self._entry_price + atr * 2.0 or close <= self._entry_price - atr * 1.5 or (macd < self._prev_macd and macd < 0):
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0:
            if close <= self._entry_price - atr * 2.0 or close >= self._entry_price + atr * 1.5 or (macd > self._prev_macd and macd > 0):
                self.BuyMarket()
                self._entry_price = 0
        if self.Position == 0:
            if macd > 0 and self._prev_macd <= 0 and close > ema:
                self._entry_price = close
                self.BuyMarket()
            elif macd < 0 and self._prev_macd >= 0 and close < ema:
                self._entry_price = close
                self.SellMarket()
        self._prev_macd = macd

    def CreateClone(self):
        return multi_lot_scalper_strategy()
