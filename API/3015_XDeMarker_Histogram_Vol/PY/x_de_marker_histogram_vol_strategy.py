import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DeMarker
from StockSharp.Algo.Strategies import Strategy


class x_de_marker_histogram_vol_strategy(Strategy):
    def __init__(self):
        super(x_de_marker_histogram_vol_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._demarker_period = self.Param("DemarkerPeriod", 14) \
            .SetDisplay("DeMarker Period", "DeMarker lookback", "Indicators")

        self._prev_dm = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def DemarkerPeriod(self):
        return self._demarker_period.Value

    def OnReseted(self):
        super(x_de_marker_histogram_vol_strategy, self).OnReseted()
        self._prev_dm = None

    def OnStarted2(self, time):
        super(x_de_marker_histogram_vol_strategy, self).OnStarted2(time)
        self._prev_dm = None

        dm = DeMarker()
        dm.Length = self.DemarkerPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(dm, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, dm_value):
        if candle.State != CandleStates.Finished:
            return
        dv = float(dm_value)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_dm = dv
            return
        if self._prev_dm is None:
            self._prev_dm = dv
            return
        if self._prev_dm < 0.45 and dv >= 0.55 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_dm > 0.55 and dv <= 0.45 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_dm = dv

    def CreateClone(self):
        return x_de_marker_histogram_vol_strategy()
