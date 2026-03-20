import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class vr_setka3_strategy(Strategy):
    def __init__(self):
        super(vr_setka3_strategy, self).__init__()

        self._start_offset = self.Param("StartOffset", 100.0)
        self._take_profit = self.Param("TakeProfit", 300.0)
        self._grid_distance = self.Param("GridDistance", 300.0)
        self._step_distance = self.Param("StepDistance", 50.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._buy_avg_price = 0.0
        self._buy_volume = 0.0
        self._sell_avg_price = 0.0
        self._sell_volume = 0.0
        self._buy_count = 0
        self._sell_count = 0
        self._has_buy_pending = False
        self._has_sell_pending = False

    @property
    def StartOffset(self):
        return self._start_offset.Value

    @StartOffset.setter
    def StartOffset(self, value):
        self._start_offset.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def GridDistance(self):
        return self._grid_distance.Value

    @GridDistance.setter
    def GridDistance(self, value):
        self._grid_distance.Value = value

    @property
    def StepDistance(self):
        return self._step_distance.Value

    @StepDistance.setter
    def StepDistance(self, value):
        self._step_distance.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(vr_setka3_strategy, self).OnStarted(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)

        if self._buy_volume > 0.0 and price >= self._buy_avg_price + float(self.TakeProfit):
            self.SellMarket()
            self._reset_state()
            return

        if self._sell_volume > 0.0 and price <= self._sell_avg_price - float(self.TakeProfit):
            self.BuyMarket()
            self._reset_state()
            return

        if self._buy_volume > 0.0 and not self._has_buy_pending:
            level = self._buy_avg_price - (float(self.GridDistance) + float(self.StepDistance) * self._buy_count)
            if level > 0.0:
                self.BuyLimit(level)
                self._has_buy_pending = True
        elif self._sell_volume > 0.0 and not self._has_sell_pending:
            level = self._sell_avg_price + (float(self.GridDistance) + float(self.StepDistance) * self._sell_count)
            self.SellLimit(level)
            self._has_sell_pending = True
        elif self._buy_volume == 0.0 and self._sell_volume == 0.0:
            if not self._has_buy_pending:
                buy_price = price - float(self.StartOffset)
                if buy_price > 0.0:
                    self.BuyLimit(buy_price)
                    self._has_buy_pending = True
            if not self._has_sell_pending:
                sell_price = price + float(self.StartOffset)
                self.SellLimit(sell_price)
                self._has_sell_pending = True

    def OnOwnTradeReceived(self, trade):
        super(vr_setka3_strategy, self).OnOwnTradeReceived(trade)

        trade_price = float(trade.Trade.Price)
        trade_vol = float(trade.Trade.Volume)

        if trade.Order.Side == Sides.Buy:
            self._buy_avg_price = (self._buy_avg_price * self._buy_volume + trade_price * trade_vol) / (self._buy_volume + trade_vol)
            self._buy_volume += trade_vol
            self._buy_count += 1
            self._has_buy_pending = False
        elif trade.Order.Side == Sides.Sell:
            self._sell_avg_price = (self._sell_avg_price * self._sell_volume + trade_price * trade_vol) / (self._sell_volume + trade_vol)
            self._sell_volume += trade_vol
            self._sell_count += 1
            self._has_sell_pending = False

    def _reset_state(self):
        self._buy_avg_price = 0.0
        self._sell_avg_price = 0.0
        self._buy_volume = 0.0
        self._sell_volume = 0.0
        self._buy_count = 0
        self._sell_count = 0
        self._has_buy_pending = False
        self._has_sell_pending = False

    def OnReseted(self):
        super(vr_setka3_strategy, self).OnReseted()
        self._reset_state()

    def CreateClone(self):
        return vr_setka3_strategy()
