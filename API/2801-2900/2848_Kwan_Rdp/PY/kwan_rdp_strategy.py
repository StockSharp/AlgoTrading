import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DeMarker, Momentum
from StockSharp.Algo.Strategies import Strategy


class kwan_rdp_strategy(Strategy):
    def __init__(self):
        super(kwan_rdp_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary candle series", "General")
        self._demarker_period = self.Param("DeMarkerPeriod", 14) \
            .SetDisplay("DeMarker Period", "DeMarker indicator length", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetDisplay("Momentum Period", "Momentum indicator length", "Indicators")

        self._prev_dem = 0.0
        self._prev_mom = 0.0
        self._initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def DeMarkerPeriod(self):
        return self._demarker_period.Value

    @property
    def MomentumPeriod(self):
        return self._momentum_period.Value

    def OnReseted(self):
        super(kwan_rdp_strategy, self).OnReseted()
        self._prev_dem = 0.0
        self._prev_mom = 0.0
        self._initialized = False

    def OnStarted2(self, time):
        super(kwan_rdp_strategy, self).OnStarted2(time)

        self._prev_dem = 0.0
        self._prev_mom = 0.0
        self._initialized = False

        demarker = DeMarker()
        demarker.Length = self.DeMarkerPeriod

        momentum = Momentum()
        momentum.Length = self.MomentumPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(demarker, momentum, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, demarker)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, dem_value, mom_value):
        if candle.State != CandleStates.Finished:
            return

        dv = float(dem_value)
        mv = float(mom_value)

        if not self._initialized:
            self._prev_dem = dv
            self._prev_mom = mv
            self._initialized = True
            return

        dem_up = dv > self._prev_dem
        dem_down = dv < self._prev_dem
        mom_up = mv > self._prev_mom
        mom_down = mv < self._prev_mom

        if dem_up and mom_up and self.Position <= 0:
            self.BuyMarket()
        elif dem_down and mom_down and self.Position >= 0:
            self.SellMarket()

        self._prev_dem = dv
        self._prev_mom = mv

    def CreateClone(self):
        return kwan_rdp_strategy()
