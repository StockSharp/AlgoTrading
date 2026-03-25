import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class re_open_positions_strategy(Strategy):
    def __init__(self):
        super(re_open_positions_strategy, self).__init__()
        self._profit_threshold = self.Param("ProfitThreshold", 300.0).SetDisplay("Profit Threshold", "Points to reopen", "Parameters")
        self._max_positions = self.Param("MaxPositions", 1).SetDisplay("Max Positions", "Maximum positions", "Parameters")
        self._sl_points = self.Param("StopLossPoints", 1000.0).SetDisplay("Stop Loss (pts)", "SL distance", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 2000.0).SetDisplay("Take Profit (pts)", "TP distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle type", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(re_open_positions_strategy, self).OnReseted()
        self._opened_count = 0
        self._last_entry = 0
        self._current_stop = 0
        self._current_take = 0

    def OnStarted(self, time):
        super(re_open_positions_strategy, self).OnStarted(time)
        self._opened_count = 0
        self._last_entry = 0
        self._current_stop = 0
        self._current_take = 0

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice

        if self.Position > 0:
            if close <= self._current_stop or close >= self._current_take:
                self.SellMarket()
                self._opened_count = 0
            elif self._opened_count < self._max_positions.Value and close - self._last_entry >= self._profit_threshold.Value:
                self.BuyMarket()
                self._last_entry = close
                self._opened_count += 1
                self._current_stop = self._last_entry - self._sl_points.Value
                self._current_take = self._last_entry + self._tp_points.Value
        elif self.Position < 0:
            if close >= self._current_stop or close <= self._current_take:
                self.BuyMarket()
                self._opened_count = 0
            elif self._opened_count < self._max_positions.Value and self._last_entry - close >= self._profit_threshold.Value:
                self.SellMarket()
                self._last_entry = close
                self._opened_count += 1
                self._current_stop = self._last_entry + self._sl_points.Value
                self._current_take = self._last_entry - self._tp_points.Value
        else:
            self.BuyMarket()
            self._last_entry = close
            self._opened_count = 1
            self._current_stop = self._last_entry - self._sl_points.Value
            self._current_take = self._last_entry + self._tp_points.Value

    def CreateClone(self):
        return re_open_positions_strategy()
