import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class hth_trader_strategy(Strategy):
    def __init__(self):
        super(hth_trader_strategy, self).__init__()

        self._trade_enabled = self.Param("TradeEnabled", True)
        self._use_profit_target = self.Param("UseProfitTarget", True)
        self._use_loss_limit = self.Param("UseLossLimit", True)
        self._profit_target_pips = self.Param("ProfitTargetPips", 80)
        self._loss_limit_pips = self.Param("LossLimitPips", 40)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._prev_close1 = 0.0
        self._prev_close2 = 0.0
        self._entry_price = 0.0
        self._price_step = 0.0

    @property
    def TradeEnabled(self):
        return self._trade_enabled.Value

    @TradeEnabled.setter
    def TradeEnabled(self, value):
        self._trade_enabled.Value = value

    @property
    def UseProfitTarget(self):
        return self._use_profit_target.Value

    @UseProfitTarget.setter
    def UseProfitTarget(self, value):
        self._use_profit_target.Value = value

    @property
    def UseLossLimit(self):
        return self._use_loss_limit.Value

    @UseLossLimit.setter
    def UseLossLimit(self, value):
        self._use_loss_limit.Value = value

    @property
    def ProfitTargetPips(self):
        return self._profit_target_pips.Value

    @ProfitTargetPips.setter
    def ProfitTargetPips(self, value):
        self._profit_target_pips.Value = value

    @property
    def LossLimitPips(self):
        return self._loss_limit_pips.Value

    @LossLimitPips.setter
    def LossLimitPips(self, value):
        self._loss_limit_pips.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(hth_trader_strategy, self).OnStarted2(time)

        self._price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0001
        if self._price_step <= 0.0:
            self._price_step = 0.0001

        self._prev_close1 = 0.0
        self._prev_close2 = 0.0
        self._entry_price = 0.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.TradeEnabled:
            return

        close = float(candle.ClosePrice)

        # Check exit conditions
        if self.Position != 0 and self._entry_price > 0.0:
            if self.Position > 0:
                price_diff = close - self._entry_price
            else:
                price_diff = self._entry_price - close

            pips_diff = price_diff / self._price_step

            if self.UseProfitTarget and pips_diff >= int(self.ProfitTargetPips):
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                self._entry_price = 0.0
                return

            if self.UseLossLimit and pips_diff <= -int(self.LossLimitPips):
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                self._entry_price = 0.0
                return

        # Entry logic
        if self.Position == 0 and self._prev_close1 > 0.0 and self._prev_close2 > 0.0:
            deviation = (100.0 * self._prev_close1 / self._prev_close2) - 100.0

            if deviation > 0.1:
                self.BuyMarket()
                self._entry_price = close
            elif deviation < -0.1:
                self.SellMarket()
                self._entry_price = close

        self._prev_close2 = self._prev_close1
        self._prev_close1 = close

    def OnReseted(self):
        super(hth_trader_strategy, self).OnReseted()
        self._prev_close1 = 0.0
        self._prev_close2 = 0.0
        self._entry_price = 0.0
        self._price_step = 0.0

    def CreateClone(self):
        return hth_trader_strategy()
