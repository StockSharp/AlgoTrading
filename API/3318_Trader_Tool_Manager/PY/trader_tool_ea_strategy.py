import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Momentum
from StockSharp.Algo.Strategies import Strategy


class trader_tool_ea_strategy(Strategy):
    def __init__(self):
        super(trader_tool_ea_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 30) \
            .SetDisplay("EMA Period", "EMA period", "Indicators")
        self._mom_period = self.Param("MomPeriod", 20) \
            .SetDisplay("Momentum", "Momentum period", "Indicators")

        self._ema = None
        self._mom = None
        self._prev_close = None
        self._prev_ema = None

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def mom_period(self):
        return self._mom_period.Value

    def OnReseted(self):
        super(trader_tool_ea_strategy, self).OnReseted()
        self._ema = None
        self._mom = None
        self._prev_close = None
        self._prev_ema = None

    def OnStarted2(self, time):
        super(trader_tool_ea_strategy, self).OnStarted2(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period
        self._mom = Momentum()
        self._mom.Length = self.mom_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromHours(1)))
        subscription.Bind(self._ema, self._mom, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, ema_value, mom_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed or not self._mom.IsFormed:
            return

        close = float(candle.ClosePrice)
        ema_val = float(ema_value)
        mom_val = float(mom_value)

        if self._prev_close is not None and self._prev_ema is not None:
            cross_up = self._prev_close <= self._prev_ema and close > ema_val
            cross_down = self._prev_close >= self._prev_ema and close < ema_val

            if cross_up and mom_val > 100.0 and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and mom_val < 100.0 and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_ema = ema_val

    def CreateClone(self):
        return trader_tool_ea_strategy()
