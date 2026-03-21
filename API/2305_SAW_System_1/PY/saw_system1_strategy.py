import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class saw_system1_strategy(Strategy):
    def __init__(self):
        super(saw_system1_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        self._breakout_multiplier = self.Param("BreakoutMultiplier", 0.5) \
            .SetDisplay("Breakout Multiplier", "Fraction of ATR for breakout offset", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_atr = None
        self._session_open = 0.0
        self._traded = False
        self._current_date = None

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def breakout_multiplier(self):
        return self._breakout_multiplier.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(saw_system1_strategy, self).OnReseted()
        self._prev_atr = None
        self._session_open = 0.0
        self._traded = False
        self._current_date = None

    def OnStarted(self, time):
        super(saw_system1_strategy, self).OnStarted(time)
        self._prev_atr = None
        self._session_open = 0.0
        self._traded = False
        self._current_date = None
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return
        atr_value = float(atr_value)
        date = candle.OpenTime.Date
        if self._current_date is None or date != self._current_date:
            self._current_date = date
            self._session_open = float(candle.OpenPrice)
            self._traded = False
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._prev_atr = atr_value
            return
        if self._traded or self._prev_atr is None or self._session_open == 0.0:
            self._prev_atr = atr_value
            return
        offset = self._prev_atr * float(self.breakout_multiplier)
        upper_break = self._session_open + offset
        lower_break = self._session_open - offset
        close_price = float(candle.ClosePrice)
        if close_price > upper_break and self.Position <= 0:
            self.BuyMarket()
            self._traded = True
        elif close_price < lower_break and self.Position >= 0:
            self.SellMarket()
            self._traded = True
        self._prev_atr = atr_value

    def CreateClone(self):
        return saw_system1_strategy()
