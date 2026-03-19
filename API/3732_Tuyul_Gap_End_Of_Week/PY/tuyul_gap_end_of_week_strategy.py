import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class tuyul_gap_end_of_week_strategy(Strategy):
    """Breakout above highest / below lowest with stop loss management."""
    def __init__(self):
        super(tuyul_gap_end_of_week_strategy, self).__init__()
        self._sl_points = self.Param("StopLossPoints", 60).SetDisplay("Stop Loss", "SL in points", "Risk")
        self._lookback = self.Param("LookbackBars", 12).SetDisplay("Lookback", "Bars for high/low", "Setup")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "Data")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(tuyul_gap_end_of_week_strategy, self).OnReseted()
        self._entry_price = 0
        self._stop_price = 0
        self._prev_highest = 0
        self._prev_lowest = 0

    def OnStarted(self, time):
        super(tuyul_gap_end_of_week_strategy, self).OnStarted(time)
        self._entry_price = 0
        self._stop_price = 0
        self._prev_highest = 0
        self._prev_lowest = 0

        hi = Highest()
        hi.Length = max(2, self._lookback.Value)
        lo = Lowest()
        lo.Length = max(2, self._lookback.Value)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(hi, lo, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, hi_val, lo_val):
        if candle.State != CandleStates.Finished:
            return

        highest = float(hi_val)
        lowest = float(lo_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        sl = self._sl_points.Value

        # Check stop
        if self.Position > 0 and self._stop_price > 0 and low <= self._stop_price:
            self.SellMarket()
            self._stop_price = 0
            self._entry_price = 0
            self._prev_highest = highest
            self._prev_lowest = lowest
            return
        if self.Position < 0 and self._stop_price > 0 and high >= self._stop_price:
            self.BuyMarket()
            self._stop_price = 0
            self._entry_price = 0
            self._prev_highest = highest
            self._prev_lowest = lowest
            return

        # Entry on breakout
        if self.Position == 0 and self._prev_highest > 0 and self._prev_lowest > 0:
            if close > self._prev_highest:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = close - sl if sl > 0 else 0
            elif close < self._prev_lowest:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = close + sl if sl > 0 else 0

        self._prev_highest = highest
        self._prev_lowest = lowest

    def CreateClone(self):
        return tuyul_gap_end_of_week_strategy()
