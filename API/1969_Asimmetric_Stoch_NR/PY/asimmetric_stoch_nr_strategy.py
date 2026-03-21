import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class asimmetric_stoch_nr_strategy(Strategy):

    def __init__(self):
        super(asimmetric_stoch_nr_strategy, self).__init__()

        self._k_period_short = self.Param("KPeriodShort", 5) \
            .SetDisplay("Short %K period", "Fast %K period for asymmetric calculation", "Indicator")
        self._k_period_long = self.Param("KPeriodLong", 12) \
            .SetDisplay("Long %K period", "Slow %K period for asymmetric calculation", "Indicator")
        self._d_period = self.Param("DPeriod", 7) \
            .SetDisplay("%D period", "Smoothing period for signal line", "Indicator")
        self._slowing = self.Param("Slowing", 3) \
            .SetDisplay("Slowing", "Smoothing of %K line", "Indicator")
        self._overbought = self.Param("Overbought", 80.0) \
            .SetDisplay("Overbought", "Overbought level", "Indicator")
        self._oversold = self.Param("Oversold", 20.0) \
            .SetDisplay("Oversold", "Oversold level", "Indicator")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Take profit in price units", "Risk")
        self._buy_open = self.Param("BuyOpen", True) \
            .SetDisplay("Allow Buy", "Allow opening long positions", "General")
        self._sell_open = self.Param("SellOpen", True) \
            .SetDisplay("Allow Sell", "Allow opening short positions", "General")
        self._buy_close = self.Param("BuyClose", True) \
            .SetDisplay("Close Long", "Allow closing long positions", "General")
        self._sell_close = self.Param("SellClose", True) \
            .SetDisplay("Close Short", "Allow closing short positions", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for indicator calculation", "General")

        self._prev_k = 0.0
        self._prev_d = 0.0
        self._is_initialized = False

    @property
    def KPeriodShort(self):
        return self._k_period_short.Value

    @KPeriodShort.setter
    def KPeriodShort(self, value):
        self._k_period_short.Value = value

    @property
    def KPeriodLong(self):
        return self._k_period_long.Value

    @KPeriodLong.setter
    def KPeriodLong(self, value):
        self._k_period_long.Value = value

    @property
    def DPeriod(self):
        return self._d_period.Value

    @DPeriod.setter
    def DPeriod(self, value):
        self._d_period.Value = value

    @property
    def Slowing(self):
        return self._slowing.Value

    @Slowing.setter
    def Slowing(self, value):
        self._slowing.Value = value

    @property
    def Overbought(self):
        return self._overbought.Value

    @Overbought.setter
    def Overbought(self, value):
        self._overbought.Value = value

    @property
    def Oversold(self):
        return self._oversold.Value

    @Oversold.setter
    def Oversold(self, value):
        self._oversold.Value = value

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
    def BuyOpen(self):
        return self._buy_open.Value

    @BuyOpen.setter
    def BuyOpen(self, value):
        self._buy_open.Value = value

    @property
    def SellOpen(self):
        return self._sell_open.Value

    @SellOpen.setter
    def SellOpen(self, value):
        self._sell_open.Value = value

    @property
    def BuyClose(self):
        return self._buy_close.Value

    @BuyClose.setter
    def BuyClose(self, value):
        self._buy_close.Value = value

    @property
    def SellClose(self):
        return self._sell_close.Value

    @SellClose.setter
    def SellClose(self, value):
        self._sell_close.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(asimmetric_stoch_nr_strategy, self).OnStarted(time)

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.KPeriodLong
        stochastic.D.Length = self.DPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .BindEx(stochastic, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            Unit(self.TakeProfit, UnitTypes.Absolute),
            Unit(self.StopLoss, UnitTypes.Absolute))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        k_raw = stoch_value.K
        d_raw = stoch_value.D
        if k_raw is None or d_raw is None:
            return

        k = float(k_raw)
        d = float(d_raw)

        if not self._is_initialized:
            self._prev_k = k
            self._prev_d = d
            self._is_initialized = True
            return

        cross_up = self._prev_k < self._prev_d and k > d
        cross_down = self._prev_k > self._prev_d and k < d
        is_oversold = k <= float(self.Oversold) or d <= float(self.Oversold)
        is_overbought = k >= float(self.Overbought) or d >= float(self.Overbought)

        if cross_up and is_oversold:
            if self.BuyOpen and self.Position <= 0:
                volume = self.Volume + abs(self.Position) if self.Position < 0 else self.Volume
                self.BuyMarket(volume)
            elif self.SellClose and self.Position < 0:
                self.BuyMarket(abs(self.Position))
        elif cross_down and is_overbought:
            if self.SellOpen and self.Position >= 0:
                volume = self.Volume + self.Position if self.Position > 0 else self.Volume
                self.SellMarket(volume)
            elif self.BuyClose and self.Position > 0:
                self.SellMarket(abs(self.Position))

        self._prev_k = k
        self._prev_d = d

    def OnReseted(self):
        super(asimmetric_stoch_nr_strategy, self).OnReseted()
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._is_initialized = False

    def CreateClone(self):
        return asimmetric_stoch_nr_strategy()
