import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Strategies import Strategy


class exp_leading_strategy(Strategy):

    def __init__(self):
        super(exp_leading_strategy, self).__init__()

        self._alpha1 = self.Param("Alpha1", 0.25) \
            .SetDisplay("Alpha1", "Alpha1 coefficient", "Indicator")
        self._alpha2 = self.Param("Alpha2", 0.33) \
            .SetDisplay("Alpha2", "Alpha2 coefficient", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle data type", "General")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Stop loss in price", "Protection")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Take profit in price", "Protection")
        self._cooldown_bars = self.Param("CooldownBars", 1) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Protection")

        self._is_initialized = False
        self._has_prev2 = False
        self._price_prev = 0.0
        self._lead_prev = 0.0
        self._net_lead_prev = 0.0
        self._ema_prev = 0.0
        self._prev_net_lead = 0.0
        self._prev_ema = 0.0
        self._prev2_net_lead = 0.0
        self._prev2_ema = 0.0
        self._bars_since_trade = 0

    @property
    def Alpha1(self):
        return self._alpha1.Value

    @Alpha1.setter
    def Alpha1(self, value):
        self._alpha1.Value = value

    @property
    def Alpha2(self):
        return self._alpha2.Value

    @Alpha2.setter
    def Alpha2(self, value):
        self._alpha2.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

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
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    def OnStarted2(self, time):
        super(exp_leading_strategy, self).OnStarted2(time)

        self.StartProtection(
            Unit(self.TakeProfit, UnitTypes.Absolute),
            Unit(self.StopLoss, UnitTypes.Absolute))

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        price = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        a1 = float(self.Alpha1)
        a2 = float(self.Alpha2)

        if not self._is_initialized:
            self._price_prev = price
            self._lead_prev = price
            self._net_lead_prev = price
            self._ema_prev = price
            self._prev_net_lead = price
            self._prev_ema = price
            self._is_initialized = True
            return

        lead = 2.0 * price + (a1 - 2.0) * self._price_prev + (1.0 - a1) * self._lead_prev
        net_lead = a2 * lead + (1.0 - a2) * self._net_lead_prev
        ema = 0.5 * price + 0.5 * self._ema_prev

        if self._has_prev2:
            buy_signal = self._prev2_net_lead > self._prev2_ema and self._prev_net_lead < self._prev_ema
            sell_signal = self._prev2_net_lead < self._prev2_ema and self._prev_net_lead > self._prev_ema

            if self._bars_since_trade >= self.CooldownBars:
                pos = self.Position
                if buy_signal and pos <= 0:
                    self.BuyMarket(self.Volume + abs(pos))
                    self._bars_since_trade = 0
                elif sell_signal and pos >= 0:
                    self.SellMarket(self.Volume + abs(pos))
                    self._bars_since_trade = 0
        else:
            self._has_prev2 = True

        self._prev2_net_lead = self._prev_net_lead
        self._prev2_ema = self._prev_ema
        self._prev_net_lead = net_lead
        self._prev_ema = ema
        self._price_prev = price
        self._lead_prev = lead
        self._net_lead_prev = net_lead
        self._ema_prev = ema

    def OnReseted(self):
        super(exp_leading_strategy, self).OnReseted()
        self._is_initialized = False
        self._has_prev2 = False
        self._price_prev = 0.0
        self._lead_prev = 0.0
        self._net_lead_prev = 0.0
        self._ema_prev = 0.0
        self._prev_net_lead = 0.0
        self._prev_ema = 0.0
        self._prev2_net_lead = 0.0
        self._prev2_ema = 0.0
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return exp_leading_strategy()
