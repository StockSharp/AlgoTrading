import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_crossover_ea_strategy(Strategy):
    def __init__(self):
        super(rsi_crossover_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._rsi_buy_level = self.Param("RsiBuyLevel", 30.0)
        self._rsi_sell_level = self.Param("RsiSellLevel", 70.0)
        self._enable_long = self.Param("EnableLong", True)
        self._enable_short = self.Param("EnableShort", True)
        self._close_by_signal = self.Param("CloseBySignal", True)
        self._stop_loss = self.Param("StopLoss", 0.0)
        self._take_profit = self.Param("TakeProfit", 0.0)
        self._trailing_stop = self.Param("TrailingStop", 0.0)

        self._previous_rsi = None
        self._long_stop = None
        self._short_stop = None
        self._long_take_profit = None
        self._short_take_profit = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiBuyLevel(self):
        return self._rsi_buy_level.Value

    @RsiBuyLevel.setter
    def RsiBuyLevel(self, value):
        self._rsi_buy_level.Value = value

    @property
    def RsiSellLevel(self):
        return self._rsi_sell_level.Value

    @RsiSellLevel.setter
    def RsiSellLevel(self, value):
        self._rsi_sell_level.Value = value

    @property
    def EnableLong(self):
        return self._enable_long.Value

    @EnableLong.setter
    def EnableLong(self, value):
        self._enable_long.Value = value

    @property
    def EnableShort(self):
        return self._enable_short.Value

    @EnableShort.setter
    def EnableShort(self, value):
        self._enable_short.Value = value

    @property
    def CloseBySignal(self):
        return self._close_by_signal.Value

    @CloseBySignal.setter
    def CloseBySignal(self, value):
        self._close_by_signal.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def TrailingStop(self):
        return self._trailing_stop.Value

    @TrailingStop.setter
    def TrailingStop(self, value):
        self._trailing_stop.Value = value

    def OnStarted2(self, time):
        super(rsi_crossover_ea_strategy, self).OnStarted2(time)

        self._previous_rsi = None
        self._long_stop = None
        self._short_stop = None
        self._long_take_profit = None
        self._short_take_profit = None

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        self._rsi = rsi

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)

        if not self._rsi.IsFormed:
            self._previous_rsi = rsi_val
            return

        previous = self._previous_rsi
        self._previous_rsi = rsi_val

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self._manage_open_position(candle):
            return

        buy_level = float(self.RsiBuyLevel)
        sell_level = float(self.RsiSellLevel)

        cross_above_buy = previous is not None and previous < buy_level and rsi_val > buy_level
        cross_below_sell = previous is not None and previous > sell_level and rsi_val < sell_level

        if self.CloseBySignal:
            if self.Position > 0 and cross_below_sell:
                self.SellMarket()
                self._reset_protection()
                return
            if self.Position < 0 and cross_above_buy:
                self.BuyMarket()
                self._reset_protection()
                return

        if self.Position != 0:
            return

        if self.EnableShort and cross_below_sell:
            self.SellMarket()
            return

        if self.EnableLong and cross_above_buy:
            self.BuyMarket()

    def _manage_open_position(self, candle):
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        sl = float(self.StopLoss)
        tp = float(self.TakeProfit)
        trail = float(self.TrailingStop)

        if self.Position > 0:
            entry_price = close

            if self._long_stop is None and sl > 0.0:
                self._long_stop = entry_price - sl

            if self._long_take_profit is None and tp > 0.0:
                self._long_take_profit = entry_price + tp

            if trail > 0.0 and close > entry_price:
                candidate = close - trail
                if self._long_stop is None or close - 2.0 * trail > self._long_stop:
                    self._long_stop = candidate

            if self._long_stop is not None and low <= self._long_stop:
                self.SellMarket()
                self._reset_protection()
                return True

            if self._long_take_profit is not None and high >= self._long_take_profit:
                self.SellMarket()
                self._reset_protection()
                return True

        elif self.Position < 0:
            entry_price = close

            if self._short_stop is None and sl > 0.0:
                self._short_stop = entry_price + sl

            if self._short_take_profit is None and tp > 0.0:
                self._short_take_profit = entry_price - tp

            if trail > 0.0 and close < entry_price:
                candidate = close + trail
                if self._short_stop is None or close + 2.0 * trail < self._short_stop:
                    self._short_stop = candidate

            if self._short_stop is not None and high >= self._short_stop:
                self.BuyMarket()
                self._reset_protection()
                return True

            if self._short_take_profit is not None and low <= self._short_take_profit:
                self.BuyMarket()
                self._reset_protection()
                return True
        else:
            self._reset_protection()

        return False

    def _reset_protection(self):
        self._long_stop = None
        self._short_stop = None
        self._long_take_profit = None
        self._short_take_profit = None

    def OnReseted(self):
        super(rsi_crossover_ea_strategy, self).OnReseted()
        self._previous_rsi = None
        self._long_stop = None
        self._short_stop = None
        self._long_take_profit = None
        self._short_take_profit = None

    def CreateClone(self):
        return rsi_crossover_ea_strategy()
