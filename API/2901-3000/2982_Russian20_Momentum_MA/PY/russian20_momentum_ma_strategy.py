import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, Momentum
from StockSharp.Algo.Strategies import Strategy


class russian20_momentum_ma_strategy(Strategy):
    def __init__(self):
        super(russian20_momentum_ma_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "SMA period for trend filter", "Indicators")
        self._mom_period = self.Param("MomPeriod", 10) \
            .SetDisplay("Momentum Period", "Momentum lookback", "Indicators")

        self._prev_mom = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def MaPeriod(self):
        return self._ma_period.Value
    @property
    def MomPeriod(self):
        return self._mom_period.Value

    def OnReseted(self):
        super(russian20_momentum_ma_strategy, self).OnReseted()
        self._prev_mom = None

    def OnStarted2(self, time):
        super(russian20_momentum_ma_strategy, self).OnStarted2(time)
        self._prev_mom = None

        sma = SimpleMovingAverage()
        sma.Length = self.MaPeriod
        mom = Momentum()
        mom.Length = self.MomPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, mom, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ma_value, mom_value):
        if candle.State != CandleStates.Finished:
            return
        mv = float(ma_value)
        momv = float(mom_value)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_mom = momv
            return
        if self._prev_mom is None:
            self._prev_mom = momv
            return
        close = float(candle.ClosePrice)
        # Price above MA + momentum crosses above 100
        if close > mv and self._prev_mom <= 100.0 and momv > 100.0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Price below MA + momentum crosses below 100
        elif close < mv and self._prev_mom >= 100.0 and momv < 100.0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_mom = momv

    def CreateClone(self):
        return russian20_momentum_ma_strategy()
