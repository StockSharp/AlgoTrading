import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class ten_pips_eurusd_strategy(Strategy):
    def __init__(self):
        super(ten_pips_eurusd_strategy, self).__init__()
        self._sl_mult = self.Param("StopLossMult", 1.0)
        self._tp_mult = self.Param("TakeProfitMult", 2.0)
        self._trail_mult = self.Param("TrailingMult", 0.8)
        self._atr_period = self.Param("AtrPeriod", 14)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(ten_pips_eurusd_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._has_prev = False

    def OnStarted2(self, time):
        super(ten_pips_eurusd_strategy, self).OnStarted2(time)
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._has_prev = False

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(atr, self.OnProcess).Start()

    def OnProcess(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            self._has_prev = True
            return

        atr_v = float(atr_val)
        close = float(candle.ClosePrice)

        pos = float(self.Position)
        if pos > 0:
            trail = close - float(self._trail_mult.Value) * atr_v
            if self._stop_price is None or trail > self._stop_price:
                self._stop_price = trail
            if close <= self._stop_price or (self._take_price is not None and close >= self._take_price):
                self.SellMarket(abs(pos))
                self._stop_price = None
                self._take_price = None
                self._entry_price = 0.0
        elif pos < 0:
            trail = close + float(self._trail_mult.Value) * atr_v
            if self._stop_price is None or trail < self._stop_price:
                self._stop_price = trail
            if close >= self._stop_price or (self._take_price is not None and close <= self._take_price):
                self.BuyMarket(abs(pos))
                self._stop_price = None
                self._take_price = None
                self._entry_price = 0.0

        pos = float(self.Position)
        if self._has_prev and pos == 0:
            if close > self._prev_high + atr_v * 0.5:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = close - float(self._sl_mult.Value) * atr_v
                self._take_price = close + float(self._tp_mult.Value) * atr_v
            elif close < self._prev_low - atr_v * 0.5:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = close + float(self._sl_mult.Value) * atr_v
                self._take_price = close - float(self._tp_mult.Value) * atr_v

        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._has_prev = True

    def CreateClone(self):
        return ten_pips_eurusd_strategy()
