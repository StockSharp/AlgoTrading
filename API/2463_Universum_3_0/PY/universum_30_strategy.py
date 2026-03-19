import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DeMarker
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class universum_30_strategy(Strategy):
    """DeMarker indicator crossing 0.3/0.7 thresholds with martingale volume and StartProtection."""
    def __init__(self):
        super(universum_30_strategy, self).__init__()
        self._demarker_period = self.Param("DemarkerPeriod", 10).SetGreaterThanZero().SetDisplay("DeMarker Period", "Length of DeMarker indicator", "Indicators")
        self._tp = self.Param("TakeProfitPoints", 50).SetGreaterThanZero().SetDisplay("Take Profit", "TP in absolute points", "Risk")
        self._sl = self.Param("StopLossPoints", 50).SetGreaterThanZero().SetDisplay("Stop Loss", "SL in absolute points", "Risk")
        self._losses_limit = self.Param("LossesLimit", 100).SetGreaterThanZero().SetDisplay("Losses Limit", "Max consecutive losses", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(universum_30_strategy, self).OnReseted()
        self._prev_demarker = 0
        self._has_prev = False

    def OnStarted(self, time):
        super(universum_30_strategy, self).OnStarted(time)
        self._prev_demarker = 0
        self._has_prev = False

        demarker = DeMarker()
        demarker.Length = self._demarker_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(demarker, self.OnProcess).Start()

        sl = self._sl.Value
        tp = self._tp.Value
        if sl > 0 or tp > 0:
            self.StartProtection(self.CreateProtection(sl if sl > 0 else 0, tp if tp > 0 else 0))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, demarker)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, demarker_val):
        if candle.State != CandleStates.Finished:
            return

        dv = float(demarker_val)

        buy_signal = self._has_prev and self._prev_demarker <= 0.3 and dv > 0.3
        sell_signal = self._has_prev and self._prev_demarker >= 0.7 and dv < 0.7

        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_demarker = dv
        self._has_prev = True

    def CreateClone(self):
        return universum_30_strategy()
