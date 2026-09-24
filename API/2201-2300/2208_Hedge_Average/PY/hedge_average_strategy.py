import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class hedge_average_strategy(Strategy):
    def __init__(self):
        super(hedge_average_strategy, self).__init__()

        self._period1 = self.Param("Period1", 5).SetGreaterThanZero()
        self._period2 = self.Param("Period2", 20).SetGreaterThanZero()
        self._start_hour = self.Param("StartHour", 0).SetRange(0, 23)
        self._end_hour = self.Param("EndHour", 23).SetRange(0, 23)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._take_profit = self.Param("TakeProfit", 0.0).SetNotNegative()
        self._stop_loss = self.Param("StopLoss", 0.0).SetNotNegative()
        self._use_trailing = self.Param("UseTrailing", False)

        self._opens = []
        self._closes = []
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._best_price = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(hedge_average_strategy, self).OnReseted()
        self._opens = []
        self._closes = []
        self._reset_protection()

    def OnStarted2(self, time):
        super(hedge_average_strategy, self).OnStarted2(time)
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._opens.append(float(candle.OpenPrice))
        self._closes.append(float(candle.ClosePrice))

        p1 = int(self._period1.Value)
        p2 = int(self._period2.Value)
        keep = max(p1, p2)
        if len(self._opens) > keep:
            del self._opens[:-keep]
            del self._closes[:-keep]

        if self.Position != 0 and self._apply_protection(candle):
            return

        if len(self._opens) < keep or self.Position != 0 or not self._is_trading_hour(candle.OpenTime.Hour):
            return

        fast_open = sum(self._opens[-p1:]) / p1
        fast_close = sum(self._closes[-p1:]) / p1
        slow_open = sum(self._opens[-p2:]) / p2
        slow_close = sum(self._closes[-p2:]) / p2

        if slow_open > slow_close and fast_open < fast_close:
            self._enter(Sides.Buy, float(candle.ClosePrice))
        elif slow_open < slow_close and fast_open > fast_close:
            self._enter(Sides.Sell, float(candle.ClosePrice))

    def _enter(self, side, price):
        if side == Sides.Buy:
            self.BuyMarket()
        else:
            self.SellMarket()

        sl = float(self._stop_loss.Value)
        tp = float(self._take_profit.Value)
        self._entry_price = price
        self._best_price = price
        self._stop_price = (price - sl if side == Sides.Buy else price + sl) if sl > 0 else None
        self._take_price = (price + tp if side == Sides.Buy else price - tp) if tp > 0 else None

    def _apply_protection(self, candle):
        sl = float(self._stop_loss.Value)

        if self.Position > 0:
            high = float(candle.HighPrice)
            self._best_price = high if self._best_price is None else max(self._best_price, high)
            if bool(self._use_trailing.Value) and sl > 0:
                candidate = self._best_price - sl
                if self._stop_price is None or candidate > self._stop_price:
                    self._stop_price = candidate

            if ((self._stop_price is not None and float(candle.LowPrice) <= self._stop_price) or
                    (self._take_price is not None and high >= self._take_price)):
                self.SellMarket(Math.Abs(self.Position))
                self._reset_protection()
                return True

        elif self.Position < 0:
            low = float(candle.LowPrice)
            self._best_price = low if self._best_price is None else min(self._best_price, low)
            if bool(self._use_trailing.Value) and sl > 0:
                candidate = self._best_price + sl
                if self._stop_price is None or candidate < self._stop_price:
                    self._stop_price = candidate

            if ((self._stop_price is not None and float(candle.HighPrice) >= self._stop_price) or
                    (self._take_price is not None and low <= self._take_price)):
                self.BuyMarket(Math.Abs(self.Position))
                self._reset_protection()
                return True

        return False

    def _is_trading_hour(self, hour):
        start = int(self._start_hour.Value)
        end = int(self._end_hour.Value)
        return start <= hour <= end if start <= end else hour >= start or hour <= end

    def _reset_protection(self):
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._best_price = None

    def CreateClone(self):
        return hedge_average_strategy()
