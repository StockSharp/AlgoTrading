import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class rsi_eraser_strategy(Strategy):
    def __init__(self):
        super(rsi_eraser_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._rsi_neutral = self.Param("RsiNeutralLevel", 50.0).SetDisplay("RSI Neutral", "Neutral level", "Indicators")
        self._sl_pips = self.Param("StopLossPips", 500.0).SetDisplay("Stop Loss (pips)", "SL distance", "Risk")
        self._tp_multiplier = self.Param("TakeProfitMultiplier", 3.0).SetGreaterThanZero().SetDisplay("TP Multiplier", "TP as multiple of SL", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Primary timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(rsi_eraser_strategy, self).OnReseted()
        self._stop_price = None
        self._take_price = None
        self._entry_price = 0
        self._stop_distance = 0
        self._break_even = False

    def OnStarted2(self, time):
        super(rsi_eraser_strategy, self).OnStarted2(time)
        self._stop_price = None
        self._take_price = None
        self._entry_price = 0
        self._stop_distance = 0
        self._break_even = False
        self._pip_size = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._pip_size = float(self.Security.PriceStep)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        try:
            rsi = float(rsi_val.Value) if hasattr(rsi_val, 'Value') else float(rsi_val)
        except:
            rsi = float(rsi_val)
        close = float(candle.ClosePrice)
        neutral = float(self._rsi_neutral.Value)

        # Manage existing position
        if self.Position > 0:
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._reset()
                return
            if self._take_price is not None and float(candle.HighPrice) >= self._take_price:
                self.SellMarket()
                self._reset()
                return
            if not self._break_even and self._stop_distance > 0:
                if close - self._entry_price >= self._stop_distance:
                    self._stop_price = self._entry_price
                    self._break_even = True
        elif self.Position < 0:
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._reset()
                return
            if self._take_price is not None and float(candle.LowPrice) <= self._take_price:
                self.BuyMarket()
                self._reset()
                return
            if not self._break_even and self._stop_distance > 0:
                if self._entry_price - close >= self._stop_distance:
                    self._stop_price = self._entry_price
                    self._break_even = True
        else:
            if self._stop_price is not None or self._take_price is not None:
                self._reset()

        # Entry signals
        if self.Position == 0:
            sd = float(self._sl_pips.Value) * self._pip_size
            if sd <= 0:
                return

            if rsi > neutral:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = close - sd
                self._take_price = close + sd * float(self._tp_multiplier.Value)
                self._stop_distance = sd
                self._break_even = False
            elif rsi < neutral:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = close + sd
                self._take_price = close - sd * float(self._tp_multiplier.Value)
                self._stop_distance = sd
                self._break_even = False

    def _reset(self):
        self._stop_price = None
        self._take_price = None
        self._entry_price = 0
        self._stop_distance = 0
        self._break_even = False

    def CreateClone(self):
        return rsi_eraser_strategy()
