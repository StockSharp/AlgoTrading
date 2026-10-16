import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates
from StockSharp.Algo.Strategies import Strategy


class exp_cycle_period_strategy(Strategy):
    """Ehlers' CyclePeriod, not a fast/slow EMA crossover.

    Core recursion is based on Ehlers Cycle Period (CyclePeriod.mq5):
    https://www.mql5.com/en/articles/288
    Price input is median of finished candle high/low. SignalBar compares a
    formed cycle-period reading against its N-bar-old reading.
    """

    def __init__(self):
        super(exp_cycle_period_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(6))).SetDisplay(
            "Candle Type", "Candle timeframe", "General")
        self._alpha = self.Param("Alpha", 0.07).SetDisplay("Alpha", "Ehlers cycle filter coefficient", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1).SetDisplay("Signal Bar", "Cycle period comparison lag", "Indicator")
        self._take_profit = self.Param("TakeProfit", 2000.0).SetDisplay("Take Profit", "Absolute price-distance target", "Risk")
        self._stop_loss = self.Param("StopLoss", 1000.0).SetDisplay("Stop Loss", "Absolute price-distance stop", "Risk")
        self._buy_open = self.Param("BuyPosOpen", True).SetDisplay("Buy Open", "Allow long entries", "Logic")
        self._sell_open = self.Param("SellPosOpen", True).SetDisplay("Sell Open", "Allow short entries", "Logic")
        self._buy_close = self.Param("BuyPosClose", True).SetDisplay("Buy Close", "Allow long closes", "Logic")
        self._sell_close = self.Param("SellPosClose", True).SetDisplay("Sell Close", "Allow short closes", "Logic")
        self._reset_state()

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value
    @property
    def Alpha(self): return self._alpha.Value
    @Alpha.setter
    def Alpha(self, value): self._alpha.Value = value
    @property
    def SignalBar(self): return self._signal_bar.Value
    @SignalBar.setter
    def SignalBar(self, value): self._signal_bar.Value = value
    @property
    def TakeProfit(self): return self._take_profit.Value
    @TakeProfit.setter
    def TakeProfit(self, value): self._take_profit.Value = value
    @property
    def StopLoss(self): return self._stop_loss.Value
    @StopLoss.setter
    def StopLoss(self, value): self._stop_loss.Value = value
    @property
    def BuyPosOpen(self): return self._buy_open.Value
    @BuyPosOpen.setter
    def BuyPosOpen(self, value): self._buy_open.Value = value
    @property
    def SellPosOpen(self): return self._sell_open.Value
    @SellPosOpen.setter
    def SellPosOpen(self, value): self._sell_open.Value = value
    @property
    def BuyPosClose(self): return self._buy_close.Value
    @BuyPosClose.setter
    def BuyPosClose(self, value): self._buy_close.Value = value
    @property
    def SellPosClose(self): return self._sell_close.Value
    @SellPosClose.setter
    def SellPosClose(self, value): self._sell_close.Value = value

    def _reset_state(self):
        self._price = []
        self._smooth = []
        self._cycle = []
        self._q = []
        self._i = []
        self._phase = []
        self._inst = []
        self._period = []
        self._valid_phase_count = 0
        self._pending_order = None

    def OnReseted(self):
        super(exp_cycle_period_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(exp_cycle_period_strategy, self).OnStarted2(time)
        self._reset_state()
        self.StartProtection(
            takeProfit=Unit(self.TakeProfit, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLoss, UnitTypes.Absolute))
        self.SubscribeCandles(self.CandleType).Bind(self.ProcessCandle).Start()

    def _compute_period(self, price):
        alpha = float(self.Alpha)
        n = len(self._price)
        self._price.append(float(price))
        if n >= 3:
            smooth = (self._price[n] + 2*self._price[n-1] + 2*self._price[n-2] + self._price[n-3]) / 6.0
        else:
            smooth = self._price[n]
        self._smooth.append(smooth)
        if n < 2:
            cycle = 0.0
        elif n < 7:
            cycle = (self._price[n] - 2*self._price[n-1] + self._price[n-2]) / 4.0
        else:
            cycle = ((1-0.5*alpha)**2 * (smooth - 2*self._smooth[n-1] + self._smooth[n-2])
                     + 2*(1-alpha)*self._cycle[n-1] - (1-alpha)**2*self._cycle[n-2])
        self._cycle.append(cycle)
        prev_inst = self._inst[-1] if self._inst else 0.0
        q = ((0.0962*cycle + 0.5769*self._cycle[n-2]
              - 0.5769*self._cycle[n-4] - 0.0962*self._cycle[n-6]) * (0.5+0.08*prev_inst)) if n >= 6 else 0.0
        i = self._cycle[n-3] if n >= 3 else 0.0
        delta = 0.0
        if n >= 7 and q != 0.0 and self._q[-1] != 0.0:
            previous_q, previous_i = self._q[-1], self._i[-1]
            den = 1.0 + i*previous_i/(q*previous_q)
            if den != 0.0:
                delta = ((i/q) - (previous_i/previous_q)) / den
                self._valid_phase_count += 1
        delta = max(0.1, min(0.9, delta))
        self._q.append(q); self._i.append(i); self._phase.append(delta)
        if n >= 7:
            recent = sorted(self._phase[-5:])
            median = recent[2]
            dominant = (6.28318 / median + 0.5) if median else 15.0
            inst = 0.33*dominant + 0.67*prev_inst
            previous_period = self._period[-1] if self._period else 0.0
            period = 0.15*inst + 0.85*previous_period
        else:
            inst = period = 0.0
        self._inst.append(inst)
        self._period.append(period)
        return period

    def _has_pending(self):
        return self._pending_order is not None and self._pending_order.State not in (
            OrderStates.Done, OrderStates.Failed)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        price = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        period = self._compute_period(price)
        lag = int(self.SignalBar)
        if (lag < 1 or len(self._period) < max(15, lag+8) or self._valid_phase_count < 5 or
                self._has_pending() or not self.IsFormedAndOnlineAndAllowTrading()):
            return
        prior = self._period[-1-lag]
        if period > prior:
            if self.Position < 0:
                if self.SellPosClose:
                    volume = abs(self.Position) + (self.Volume if self.BuyPosOpen else 0)
                    self._pending_order = self.BuyMarket(volume)
            elif self.Position == 0 and self.BuyPosOpen:
                self._pending_order = self.BuyMarket(self.Volume)
        elif period < prior:
            if self.Position > 0:
                if self.BuyPosClose:
                    volume = abs(self.Position) + (self.Volume if self.SellPosOpen else 0)
                    self._pending_order = self.SellMarket(volume)
            elif self.Position == 0 and self.SellPosOpen:
                self._pending_order = self.SellMarket(self.Volume)

    def CreateClone(self):
        return exp_cycle_period_strategy()
