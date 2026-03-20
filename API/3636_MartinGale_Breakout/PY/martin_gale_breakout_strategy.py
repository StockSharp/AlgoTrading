import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class martin_gale_breakout_strategy(Strategy):
    def __init__(self):
        super(martin_gale_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._required_history = self.Param("RequiredHistory", 10)
        self._breakout_factor = self.Param("BreakoutFactor", 2.5)
        self._take_profit_pct = self.Param("TakeProfitPct", 0.5)
        self._stop_loss_pct = self.Param("StopLossPct", 0.3)
        self._recovery_multiplier = self.Param("RecoveryMultiplier", 1.5)

        self._ranges = []
        self._entry_price = 0.0
        self._entry_side = 0
        self._recovering = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RequiredHistory(self):
        return self._required_history.Value

    @RequiredHistory.setter
    def RequiredHistory(self, value):
        self._required_history.Value = value

    @property
    def BreakoutFactor(self):
        return self._breakout_factor.Value

    @BreakoutFactor.setter
    def BreakoutFactor(self, value):
        self._breakout_factor.Value = value

    @property
    def TakeProfitPct(self):
        return self._take_profit_pct.Value

    @TakeProfitPct.setter
    def TakeProfitPct(self, value):
        self._take_profit_pct.Value = value

    @property
    def StopLossPct(self):
        return self._stop_loss_pct.Value

    @StopLossPct.setter
    def StopLossPct(self, value):
        self._stop_loss_pct.Value = value

    @property
    def RecoveryMultiplier(self):
        return self._recovery_multiplier.Value

    @RecoveryMultiplier.setter
    def RecoveryMultiplier(self, value):
        self._recovery_multiplier.Value = value

    def OnReseted(self):
        super(martin_gale_breakout_strategy, self).OnReseted()
        self._ranges = []
        self._entry_price = 0.0
        self._entry_side = 0
        self._recovering = False

    def OnStarted(self, time):
        super(martin_gale_breakout_strategy, self).OnStarted(time)
        self._ranges = []
        self._entry_price = 0.0
        self._entry_side = 0
        self._recovering = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _add_range(self, r):
        self._ranges.append(r)
        req = self.RequiredHistory
        while len(self._ranges) > req:
            self._ranges.pop(0)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        r = float(candle.HighPrice) - float(candle.LowPrice)

        # Check exit
        if self.Position != 0 and self._entry_price > 0:
            tp_pct = float(self.TakeProfitPct) * float(self.RecoveryMultiplier) if self._recovering else float(self.TakeProfitPct)
            sl_pct = float(self.StopLossPct)

            if self._entry_side == 1:
                pnl = (close - self._entry_price) / self._entry_price * 100.0
                if pnl >= tp_pct or pnl <= -sl_pct:
                    was_loss = pnl < 0
                    self.SellMarket()
                    self._entry_price = 0.0
                    self._entry_side = 0
                    self._recovering = was_loss
                    self._add_range(r)
                    return
            elif self._entry_side == -1:
                pnl = (self._entry_price - close) / self._entry_price * 100.0
                if pnl >= tp_pct or pnl <= -sl_pct:
                    was_loss = pnl < 0
                    self.BuyMarket()
                    self._entry_price = 0.0
                    self._entry_side = 0
                    self._recovering = was_loss
                    self._add_range(r)
                    return

        # Entry - only when flat
        req = self.RequiredHistory
        if self.Position == 0 and len(self._ranges) >= req:
            total = sum(self._ranges)
            avg_range = total / len(self._ranges)

            if avg_range > 0 and r > avg_range * float(self.BreakoutFactor):
                body = float(candle.ClosePrice) - float(candle.OpenPrice)

                if body > 0 and body > r * 0.4:
                    self.BuyMarket()
                    self._entry_price = close
                    self._entry_side = 1
                elif body < 0 and abs(body) > r * 0.4:
                    self.SellMarket()
                    self._entry_price = close
                    self._entry_side = -1

        self._add_range(r)

    def CreateClone(self):
        return martin_gale_breakout_strategy()
