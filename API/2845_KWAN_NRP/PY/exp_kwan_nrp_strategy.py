import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, Momentum
from StockSharp.Algo.Strategies import Strategy


class exp_kwan_nrp_strategy(Strategy):
    def __init__(self):
        super(exp_kwan_nrp_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI length", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetDisplay("Momentum Period", "Momentum length", "Indicators")

        self._prev_rsi = 0.0
        self._prev_mom = 0.0
        self._initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def MomentumPeriod(self):
        return self._momentum_period.Value

    def OnReseted(self):
        super(exp_kwan_nrp_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._prev_mom = 0.0
        self._initialized = False

    def OnStarted2(self, time):
        super(exp_kwan_nrp_strategy, self).OnStarted2(time)

        self._prev_rsi = 0.0
        self._prev_mom = 0.0
        self._initialized = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        momentum = Momentum()
        momentum.Length = self.MomentumPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(rsi, momentum, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_value, mom_value):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_value)
        mv = float(mom_value)

        if not self._initialized:
            self._prev_rsi = rv
            self._prev_mom = mv
            self._initialized = True
            return

        rsi_up = rv > self._prev_rsi
        rsi_down = rv < self._prev_rsi
        mom_up = mv > self._prev_mom
        mom_down = mv < self._prev_mom

        if rsi_up and mom_up and self.Position <= 0:
            self.BuyMarket()
        elif rsi_down and mom_down and self.Position >= 0:
            self.SellMarket()

        self._prev_rsi = rv
        self._prev_mom = mv

    def CreateClone(self):
        return exp_kwan_nrp_strategy()
