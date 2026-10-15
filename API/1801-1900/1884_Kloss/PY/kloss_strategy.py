import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WeightedMovingAverage, CommodityChannelIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class kloss_strategy(Strategy):
    def __init__(self):
        super(kloss_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Length of weighted MA", "Indicators") \
            .SetOptimize(5, 50, 5)
        self._cci_period = self.Param("CciPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "Length of CCI", "Indicators") \
            .SetOptimize(5, 30, 5)
        self._cci_level = self.Param("CciLevel", 50.0) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Level", "Distance from zero to trigger signal", "Indicators") \
            .SetOptimize(50.0, 200.0, 10.0)
        self._stoch_level = self.Param("StochLevel", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Level", "Distance from 50 to trigger", "Indicators") \
            .SetOptimize(5.0, 40.0, 5.0)
        self._stop_loss = self.Param("StopLoss", 550.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss", "Stop loss in price steps", "Risk")
        self._take_profit_param = self.Param("TakeProfit", 550.0) \
            .SetNotNegative() \
            .SetDisplay("Take Profit", "Take profit in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candles for calculations", "General")
        self._cooldown_bars = self.Param("CooldownBars", 3) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")

        self._previous_signal = 0
        self._cooldown_remaining = 0

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def cci_level(self):
        return self._cci_level.Value

    @property
    def stoch_level(self):
        return self._stoch_level.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def take_profit(self):
        return self._take_profit_param.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(kloss_strategy, self).OnReseted()
        self._previous_signal = 0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(kloss_strategy, self).OnStarted2(time)
        ma = WeightedMovingAverage()
        ma.Length = self.ma_period
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        stoch = StochasticOscillator()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ma, cci, stoch, self.process_candle).Start()
        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(1, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ma_value, cci_value, stoch_value):
        if candle.State != CandleStates.Finished:
            return
        if not ma_value.IsFinal or not cci_value.IsFinal or not stoch_value.IsFinal:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        ma = float(ma_value)
        cci = float(cci_value)

        stoch_k = stoch_value.K
        stoch_d = stoch_value.D
        if stoch_k is None or stoch_d is None:
            return
        k = float(stoch_k)
        d = float(stoch_d)

        price = float(candle.ClosePrice)
        cci_lvl = float(self.cci_level)
        stoch_lvl = float(self.stoch_level)

        buy_signal = cci < -cci_lvl and k < 50.0 - stoch_lvl and d < 50.0 - stoch_lvl and price > ma
        sell_signal = cci > cci_lvl and k > 50.0 + stoch_lvl and d > 50.0 + stoch_lvl and price < ma

        if buy_signal:
            current_signal = 1
        elif sell_signal:
            current_signal = -1
        else:
            current_signal = 0

        if self._cooldown_remaining == 0 and self.Position == 0:
            if current_signal > 0 and self._previous_signal <= 0:
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif current_signal < 0 and self._previous_signal >= 0:
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars

        if current_signal != 0:
            self._previous_signal = current_signal

    def CreateClone(self):
        return kloss_strategy()
