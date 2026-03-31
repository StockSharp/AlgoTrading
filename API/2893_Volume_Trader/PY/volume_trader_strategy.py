import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class volume_trader_strategy(Strategy):
    """Volume-based reversal: rising volume suggests long, falling volume suggests short."""
    def __init__(self):
        super(volume_trader_strategy, self).__init__()
        self._start_hour = self.Param("StartHour", 9).SetDisplay("Start Hour", "Trading session start", "Session")
        self._end_hour = self.Param("EndHour", 18).SetDisplay("End Hour", "Trading session end", "Session")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(volume_trader_strategy, self).OnReseted()
        self._prev_vol = None
        self._prev_prev_vol = None

    def OnStarted2(self, time):
        super(volume_trader_strategy, self).OnStarted2(time)
        self._prev_vol = None
        self._prev_prev_vol = None

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        vol = float(candle.TotalVolume)

        if self._prev_vol is not None and self._prev_prev_vol is not None:
            hour = candle.CloseTime.Hour
            start_h = self._start_hour.Value
            end_h = self._end_hour.Value
            in_session = hour >= start_h and hour <= end_h

            if in_session:
                if self._prev_vol > self._prev_prev_vol * 1.1 and self.Position <= 0:
                    vol_to_trade = self.Volume + (abs(self.Position) if self.Position < 0 else 0)
                    if vol_to_trade > 0:
                        self.BuyMarket(vol_to_trade)
                elif self._prev_vol < self._prev_prev_vol * 0.9 and self.Position >= 0:
                    vol_to_trade = self.Volume + (abs(self.Position) if self.Position > 0 else 0)
                    if vol_to_trade > 0:
                        self.SellMarket(vol_to_trade)

        self._prev_prev_vol = self._prev_vol
        self._prev_vol = vol

    def CreateClone(self):
        return volume_trader_strategy()
