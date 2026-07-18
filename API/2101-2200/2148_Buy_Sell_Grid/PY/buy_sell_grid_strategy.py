import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class buy_sell_grid_strategy(Strategy):
    def __init__(self):
        super(buy_sell_grid_strategy, self).__init__()
        self._grid_step_pct = self.Param("GridStepPct", 0.3) \
            .SetDisplay("Grid Step %", "Distance from EMA for grid entry", "General")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA period for grid center", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type for processing", "General")
        self._entry_price = 0.0

    @property
    def grid_step_pct(self):
        return self._grid_step_pct.Value

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(buy_sell_grid_strategy, self).OnReseted()
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(buy_sell_grid_strategy, self).OnStarted2(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.process_candle).Start()

    def process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        ema_value = float(ema_value)
        close = float(candle.ClosePrice)
        step = float(self.grid_step_pct)
        lower_grid = ema_value * (1.0 - step / 100.0)
        upper_grid = ema_value * (1.0 + step / 100.0)
        if self.Position == 0:
            if close <= lower_grid:
                self.BuyMarket()
                self._entry_price = close
            elif close >= upper_grid:
                self.SellMarket()
                self._entry_price = close
        elif self.Position > 0:
            if close >= ema_value:
                self.SellMarket()
            elif close <= self._entry_price * (1.0 - step / 100.0):
                self.BuyMarket()
                self._entry_price = close
        elif self.Position < 0:
            if close <= ema_value:
                self.BuyMarket()
            elif close >= self._entry_price * (1.0 + step / 100.0):
                self.SellMarket()
                self._entry_price = close

    def CreateClone(self):
        return buy_sell_grid_strategy()
