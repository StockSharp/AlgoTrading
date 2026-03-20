import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class bykov_trend_re_open_strategy(Strategy):
    def __init__(self):
        super(bykov_trend_re_open_strategy, self).__init__()

        self._risk = self.Param("Risk", 3)
        self._ssp = self.Param("Ssp", 9)
        self._price_step_param = self.Param("PriceStep", 300.0)
        self._max_positions = self.Param("MaxPositions", 10)
        self._stop_loss = self.Param("StopLoss", 1000.0)
        self._take_profit = self.Param("TakeProfit", 2000.0)
        self._enable_long_open = self.Param("EnableLongOpen", True)
        self._enable_short_open = self.Param("EnableShortOpen", True)
        self._enable_long_close = self.Param("EnableLongClose", True)
        self._enable_short_close = self.Param("EnableShortClose", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._last_buy_price = 0.0
        self._last_sell_price = 0.0
        self._buy_count = 0
        self._sell_count = 0
        self._trend_up = False

    @property
    def Risk(self):
        return self._risk.Value

    @Risk.setter
    def Risk(self, value):
        self._risk.Value = value

    @property
    def Ssp(self):
        return self._ssp.Value

    @Ssp.setter
    def Ssp(self, value):
        self._ssp.Value = value

    @property
    def PriceStepParam(self):
        return self._price_step_param.Value

    @PriceStepParam.setter
    def PriceStepParam(self, value):
        self._price_step_param.Value = value

    @property
    def MaxPositions(self):
        return self._max_positions.Value

    @MaxPositions.setter
    def MaxPositions(self, value):
        self._max_positions.Value = value

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
    def EnableLongOpen(self):
        return self._enable_long_open.Value

    @EnableLongOpen.setter
    def EnableLongOpen(self, value):
        self._enable_long_open.Value = value

    @property
    def EnableShortOpen(self):
        return self._enable_short_open.Value

    @EnableShortOpen.setter
    def EnableShortOpen(self, value):
        self._enable_short_open.Value = value

    @property
    def EnableLongClose(self):
        return self._enable_long_close.Value

    @EnableLongClose.setter
    def EnableLongClose(self, value):
        self._enable_long_close.Value = value

    @property
    def EnableShortClose(self):
        return self._enable_short_close.Value

    @EnableShortClose.setter
    def EnableShortClose(self, value):
        self._enable_short_close.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(bykov_trend_re_open_strategy, self).OnStarted(time)

        self._last_buy_price = 0.0
        self._last_sell_price = 0.0
        self._buy_count = 0
        self._sell_count = 0
        self._trend_up = False

        wpr = WilliamsR()
        wpr.Length = self.Ssp
        atr = AverageTrueRange()
        atr.Length = 15

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(wpr, atr, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, wpr_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        wpr = float(wpr_value)
        close = float(candle.ClosePrice)

        k = 33 - int(self.Risk)
        new_trend = self._trend_up
        if wpr < -100.0 + k:
            new_trend = False
        if wpr > -k:
            new_trend = True

        buy_signal = not self._trend_up and new_trend
        sell_signal = self._trend_up and not new_trend

        price_step = float(self.PriceStepParam)
        sl = float(self.StopLoss)
        tp = float(self.TakeProfit)
        max_pos = int(self.MaxPositions)

        if buy_signal:
            if self.EnableShortClose and self.Position < 0:
                self.BuyMarket()
                self._reset_state()
            if self.EnableLongOpen and self.Position <= 0:
                self.BuyMarket()
                self._last_buy_price = close
                self._buy_count = 1
        elif sell_signal:
            if self.EnableLongClose and self.Position > 0:
                self.SellMarket()
                self._reset_state()
            if self.EnableShortOpen and self.Position >= 0:
                self.SellMarket()
                self._last_sell_price = close
                self._sell_count = 1

        if self.Position > 0:
            self._check_rebuy(close, price_step, max_pos, sl, tp)
        elif self.Position < 0:
            self._check_resell(close, price_step, max_pos, sl, tp)

        self._trend_up = new_trend

    def _check_rebuy(self, price, price_step, max_pos, sl, tp):
        if self._buy_count < max_pos and price - self._last_buy_price >= price_step:
            self.BuyMarket()
            self._last_buy_price = price
            self._buy_count += 1
        self._check_stops(price, True, sl, tp)

    def _check_resell(self, price, price_step, max_pos, sl, tp):
        if self._sell_count < max_pos and self._last_sell_price - price >= price_step:
            self.SellMarket()
            self._last_sell_price = price
            self._sell_count += 1
        self._check_stops(price, False, sl, tp)

    def _check_stops(self, price, is_long, sl, tp):
        entry = self._last_buy_price if is_long else self._last_sell_price

        if sl > 0.0:
            stop_price = entry - sl if is_long else entry + sl
            if (is_long and price <= stop_price) or (not is_long and price >= stop_price):
                if is_long:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                self._reset_state()
                return

        if tp > 0.0:
            target = entry + tp if is_long else entry - tp
            if (is_long and price >= target) or (not is_long and price <= target):
                if is_long:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                self._reset_state()

    def _reset_state(self):
        self._last_buy_price = 0.0
        self._last_sell_price = 0.0
        self._buy_count = 0
        self._sell_count = 0

    def OnReseted(self):
        super(bykov_trend_re_open_strategy, self).OnReseted()
        self._last_buy_price = 0.0
        self._last_sell_price = 0.0
        self._buy_count = 0
        self._sell_count = 0
        self._trend_up = False

    def CreateClone(self):
        return bykov_trend_re_open_strategy()
