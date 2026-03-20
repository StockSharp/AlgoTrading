import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class doji_arrows_strategy(Strategy):
    def __init__(self):
        super(doji_arrows_strategy, self).__init__()

        self._stop_loss_points = self.Param("StopLossPoints", 30) \
            .SetDisplay("Stop Loss Points", "Stop loss distance in price steps.", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 90) \
            .SetDisplay("Stop Loss Points", "Stop loss distance in price steps.", "Risk")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 15) \
            .SetDisplay("Stop Loss Points", "Stop loss distance in price steps.", "Risk")
        self._trailing_step_points = self.Param("TrailingStepPoints", 5) \
            .SetDisplay("Stop Loss Points", "Stop loss distance in price steps.", "Risk")
        self._doji_body_points = self.Param("DojiBodyPoints", 1) \
            .SetDisplay("Stop Loss Points", "Stop loss distance in price steps.", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Stop Loss Points", "Stop loss distance in price steps.", "Risk")

        self._has_previous_candle = False
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(doji_arrows_strategy, self).OnReseted()
        self._has_previous_candle = False
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def OnStarted(self, time):
        super(doji_arrows_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return doji_arrows_strategy()
