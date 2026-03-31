import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class hans123_trader_strategy(Strategy):

    def __init__(self):
        super(hans123_trader_strategy, self).__init__()
        self._order_volume = self.Param("OrderVolume", 0.1)
        self._range_length = self.Param("RangeLength", 40)
        self._stop_loss_pips = self.Param("StopLossPips", 50)
        self._take_profit_pips = self.Param("TakeProfitPips", 50)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 10)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5)
        self._start_hour = self.Param("StartHour", 0)
        self._end_hour = self.Param("EndHour", 24)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(3)))

        self._highest = None
        self._lowest = None
        self._entry_price = 0.0
        self._pip_size = 0.0
        self._highest_since_entry = 0.0
        self._lowest_since_entry = 0.0

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def RangeLength(self):
        return self._range_length.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @property
    def EndHour(self):
        return self._end_hour.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(hans123_trader_strategy, self).OnStarted2(time)

        self._pip_size = self._calculate_pip_size()

        self._highest = Highest()
        self._highest.Length = self.RangeLength
        self._lowest = Lowest()
        self._lowest.Length = self.RangeLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._highest, self._lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._highest)
            self.DrawIndicator(area, self._lowest)
            self.DrawOwnTrades(area)

    def OnOwnTradeReceived(self, trade):
        super(hans123_trader_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Trade is None:
            return
        pos = float(self.Position)
        if pos != 0 and self._entry_price == 0.0:
            self._entry_price = float(trade.Trade.Price)
            self._highest_since_entry = float(trade.Trade.Price)
            self._lowest_since_entry = float(trade.Trade.Price)
        if pos == 0:
            self._entry_price = 0.0
            self._highest_since_entry = 0.0
            self._lowest_since_entry = 0.0

    def _process_candle(self, candle, highest, lowest):
        if candle.State != CandleStates.Finished:
            return

        self._check_protection(candle)

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        if not self._is_within_trading_window(candle.OpenTime):
            return

        highest_val = float(highest)
        lowest_val = float(lowest)

        if float(self.OrderVolume) <= 0 or highest_val <= lowest_val:
            return

        pos = float(self.Position)
        if pos > 0 and float(candle.HighPrice) > self._highest_since_entry:
            self._highest_since_entry = float(candle.HighPrice)
        if pos < 0 and (self._lowest_since_entry == 0 or float(candle.LowPrice) < self._lowest_since_entry):
            self._lowest_since_entry = float(candle.LowPrice)

        if float(self.Position) == 0:
            self._entry_price = 0.0
            self._highest_since_entry = 0.0
            self._lowest_since_entry = 0.0
            close = float(candle.ClosePrice)
            if float(candle.HighPrice) >= highest_val:
                self.BuyMarket(float(self.OrderVolume))
                self._entry_price = close
                self._highest_since_entry = close
                self._lowest_since_entry = close
            elif float(candle.LowPrice) <= lowest_val:
                self.SellMarket(float(self.OrderVolume))
                self._entry_price = close
                self._highest_since_entry = close
                self._lowest_since_entry = close

    def _check_protection(self, candle):
        pos = float(self.Position)
        if pos == 0 or self._entry_price == 0.0:
            return

        stop_dist = self.StopLossPips * self._pip_size if self.StopLossPips > 0 else 0.0
        take_dist = self.TakeProfitPips * self._pip_size if self.TakeProfitPips > 0 else 0.0
        trail_dist = self.TrailingStopPips * self._pip_size if self.TrailingStopPips > 0 else 0.0
        activation = (self.TrailingStopPips + self.TrailingStepPips) * self._pip_size

        if pos > 0:
            if stop_dist > 0 and float(candle.LowPrice) <= self._entry_price - stop_dist:
                self.SellMarket(abs(pos))
                return
            if take_dist > 0 and float(candle.HighPrice) >= self._entry_price + take_dist:
                self.SellMarket(abs(pos))
                return
            if trail_dist > 0 and self._highest_since_entry - self._entry_price > activation:
                trail_stop = self._highest_since_entry - trail_dist
                if float(candle.LowPrice) <= trail_stop:
                    self.SellMarket(abs(pos))
                    return
        elif pos < 0:
            if stop_dist > 0 and float(candle.HighPrice) >= self._entry_price + stop_dist:
                self.BuyMarket(abs(pos))
                return
            if take_dist > 0 and float(candle.LowPrice) <= self._entry_price - take_dist:
                self.BuyMarket(abs(pos))
                return
            if trail_dist > 0 and self._lowest_since_entry > 0 and self._entry_price - self._lowest_since_entry > activation:
                trail_stop = self._lowest_since_entry + trail_dist
                if float(candle.HighPrice) >= trail_stop:
                    self.BuyMarket(abs(pos))
                    return

    def _calculate_pip_size(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        return step

    def _is_within_trading_window(self, time):
        return time.Hour >= self.StartHour and time.Hour < self.EndHour

    def OnReseted(self):
        super(hans123_trader_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._entry_price = 0.0
        self._pip_size = 0.0
        self._highest_since_entry = 0.0
        self._lowest_since_entry = 0.0

    def CreateClone(self):
        return hans123_trader_strategy()
