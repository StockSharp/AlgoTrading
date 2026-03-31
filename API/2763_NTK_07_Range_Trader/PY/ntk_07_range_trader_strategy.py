import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class ntk_07_range_trader_strategy(Strategy):
    def __init__(self):
        super(ntk_07_range_trader_strategy, self).__init__()
        self._entry_volume = self.Param("EntryVolume", 1.0).SetGreaterThanZero().SetDisplay("Entry Volume", "Base volume for each entry order", "Risk")
        self._sl_points = self.Param("StopLossPoints", 11.0).SetNotNegative().SetDisplay("Stop Loss", "Initial stop distance in price steps", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 30.0).SetNotNegative().SetDisplay("Take Profit", "Take-profit in price steps", "Risk")
        self._trailing_points = self.Param("TrailingStopPoints", 8.0).SetGreaterThanZero().SetDisplay("Trailing Stop", "Trailing stop distance in price steps", "Risk")
        self._net_step = self.Param("NetStepPoints", 5.0).SetGreaterThanZero().SetDisplay("Net Step", "Offset for stop entries in price steps", "Entries")
        self._ma_period = self.Param("MovingAveragePeriod", 100).SetGreaterThanZero().SetDisplay("MA Period", "Moving average length for trailing", "Risk")
        self._candle_type = self.Param("CandleType", tf(5)).SetDisplay("Candle Type", "Primary timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(ntk_07_range_trader_strategy, self).OnReseted()
        self._prev_candle = None
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None

    def OnStarted2(self, time):
        super(ntk_07_range_trader_strategy, self).OnStarted2(time)
        self._prev_candle = None
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None
        self._price_step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._price_step = float(self.Security.PriceStep)

        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ma, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def _to_price(self, points):
        if points <= 0:
            return 0
        step = self._price_step if self._price_step > 0 else 1
        return points * step

    def OnProcess(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        # Check SL/TP
        if self.Position > 0:
            if self._stop_price is not None and candle.LowPrice <= self._stop_price:
                self.SellMarket(self.Position)
                self._reset()
                self._prev_candle = candle
                return
            if self._take_price is not None and candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
                self._reset()
                self._prev_candle = candle
                return
            # trailing
            trail_offset = self._to_price(self._trailing_points.Value)
            if trail_offset > 0:
                candidate = candle.ClosePrice - trail_offset
                if self._stop_price is None or candidate > self._stop_price:
                    self._stop_price = min(candidate, candle.ClosePrice - self._price_step)
        elif self.Position < 0:
            if self._stop_price is not None and candle.HighPrice >= self._stop_price:
                self.BuyMarket(Math.Abs(self.Position))
                self._reset()
                self._prev_candle = candle
                return
            if self._take_price is not None and candle.LowPrice <= self._take_price:
                self.BuyMarket(Math.Abs(self.Position))
                self._reset()
                self._prev_candle = candle
                return
            trail_offset = self._to_price(self._trailing_points.Value)
            if trail_offset > 0:
                candidate = candle.ClosePrice + trail_offset
                if self._stop_price is None or candidate < self._stop_price:
                    self._stop_price = max(candidate, candle.ClosePrice + self._price_step)

        # Entry
        net_offset = self._to_price(self._net_step.Value)
        if self.Position == 0 and net_offset > 0 and self._prev_candle is not None:
            buy_level = self._prev_candle.ClosePrice + net_offset
            sell_level = self._prev_candle.ClosePrice - net_offset
            if candle.HighPrice >= buy_level:
                self.BuyMarket(self._entry_volume.Value)
                self._entry_price = candle.ClosePrice
                sl_offset = self._to_price(self._sl_points.Value)
                tp_offset = self._to_price(self._tp_points.Value)
                self._stop_price = self._entry_price - sl_offset if sl_offset > 0 else None
                self._take_price = self._entry_price + tp_offset if tp_offset > 0 else None
            elif candle.LowPrice <= sell_level:
                self.SellMarket(self._entry_volume.Value)
                self._entry_price = candle.ClosePrice
                sl_offset = self._to_price(self._sl_points.Value)
                tp_offset = self._to_price(self._tp_points.Value)
                self._stop_price = self._entry_price + sl_offset if sl_offset > 0 else None
                self._take_price = self._entry_price - tp_offset if tp_offset > 0 else None

        self._prev_candle = candle

    def _reset(self):
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return ntk_07_range_trader_strategy()
