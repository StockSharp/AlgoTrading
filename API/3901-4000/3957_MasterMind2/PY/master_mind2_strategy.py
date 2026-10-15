import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import StochasticOscillator, WilliamsR

class master_mind2_strategy(Strategy):
    def __init__(self):
        super(master_mind2_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Trade Volume", "Trade volume in contracts", "General")
        self._stochastic_period = self.Param("StochasticPeriod", 100) \
            .SetDisplay("Stochastic Period", "Period for the Stochastic Oscillator", "Indicators")
        self._stochastic_k = self.Param("StochasticK", 3) \
            .SetDisplay("Stochastic %K", "Smoothing length for %K", "Indicators")
        self._stochastic_d = self.Param("StochasticD", 3) \
            .SetDisplay("Stochastic %D", "Smoothing length for %D", "Indicators")
        self._williams_period = self.Param("WilliamsPeriod", 100) \
            .SetDisplay("Williams %R Period", "Lookback for Williams %R", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 2000.0) \
            .SetDisplay("Stop Loss", "Stop loss distance in price points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 0.0) \
            .SetDisplay("Take Profit", "Take profit distance in price points", "Risk")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 0.0) \
            .SetDisplay("Trailing Stop", "Trailing stop distance in price points", "Risk")
        self._trailing_step_points = self.Param("TrailingStepPoints", 1.0) \
            .SetDisplay("Trailing Step", "Minimum improvement to trail stop", "Risk")
        self._break_even_points = self.Param("BreakEvenPoints", 0.0) \
            .SetDisplay("Break Even", "Distance to move stop to break-even", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candle type used for calculations", "General")

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @property
    def StochasticPeriod(self):
        return self._stochastic_period.Value

    @property
    def StochasticK(self):
        return self._stochastic_k.Value

    @property
    def StochasticD(self):
        return self._stochastic_d.Value

    @property
    def WilliamsPeriod(self):
        return self._williams_period.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @property
    def TrailingStepPoints(self):
        return self._trailing_step_points.Value

    @property
    def BreakEvenPoints(self):
        return self._break_even_points.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(master_mind2_strategy, self).OnStarted2(time)

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.StochasticPeriod
        stochastic.D.Length = self.StochasticD

        williams = WilliamsR()
        williams.Length = self.WilliamsPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(stochastic, williams, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, stochastic_value, williams_value):
        if candle.State != CandleStates.Finished:
            return

        if not stochastic_value.IsFinal or not williams_value.IsFinal:
            return

        stoch = stochastic_value
        signal = stoch.D
        if signal is None:
            return

        signal = float(signal)
        wpr = float(williams_value)

        ps = self.Security.PriceStep if self.Security is not None else None
        step = float(ps) if ps is not None else 1.0
        if step <= 0:
            step = 1.0

        self._manage_long_position(candle, step)
        self._manage_short_position(candle, step)

        if signal < 5.0 and wpr < -95.0:
            self._handle_buy_signal(candle, step)
        elif signal > 95.0 and wpr > -5.0:
            self._handle_sell_signal(candle, step)

    def _manage_long_position(self, candle, step):
        if self.Position <= 0:
            return

        be = float(self.BreakEvenPoints)
        if be > 0 and float(candle.ClosePrice) - self._entry_price >= be * step:
            if self._stop_price == 0 or self._stop_price < self._entry_price:
                self._stop_price = self._entry_price

        ts = float(self.TrailingStopPoints)
        if ts > 0:
            candidate_stop = float(candle.ClosePrice) - ts * step
            tst = float(self.TrailingStepPoints)
            if self._stop_price == 0 or candidate_stop - self._stop_price >= tst * step:
                self._stop_price = candidate_stop

        stop_hit = self._stop_price > 0 and float(candle.LowPrice) <= self._stop_price
        target_hit = self._take_profit_price > 0 and float(candle.HighPrice) >= self._take_profit_price
        if stop_hit or target_hit:
            self.SellMarket(self.Position)
            self._reset_stops()

    def _manage_short_position(self, candle, step):
        if self.Position >= 0:
            return

        be = float(self.BreakEvenPoints)
        if be > 0 and self._entry_price - float(candle.ClosePrice) >= be * step:
            if self._stop_price == 0 or self._stop_price > self._entry_price:
                self._stop_price = self._entry_price

        ts = float(self.TrailingStopPoints)
        if ts > 0:
            candidate_stop = float(candle.ClosePrice) + ts * step
            tst = float(self.TrailingStepPoints)
            if self._stop_price == 0 or self._stop_price - candidate_stop >= tst * step:
                self._stop_price = candidate_stop

        stop_hit = self._stop_price > 0 and float(candle.HighPrice) >= self._stop_price
        target_hit = self._take_profit_price > 0 and float(candle.LowPrice) <= self._take_profit_price
        if stop_hit or target_hit:
            self.BuyMarket(abs(self.Position))
            self._reset_stops()

    def _handle_buy_signal(self, candle, step):
        if self.Position < 0:
            self.BuyMarket(abs(self.Position))
            self._reset_stops()

        tv = float(self.TradeVolume)
        if self.Position > 0 or tv <= 0:
            return

        self.BuyMarket(tv)
        self._entry_price = float(candle.ClosePrice)
        sl = float(self.StopLossPoints)
        tp = float(self.TakeProfitPoints)
        self._stop_price = self._entry_price - sl * step if sl > 0 else 0.0
        self._take_profit_price = self._entry_price + tp * step if tp > 0 else 0.0

    def _handle_sell_signal(self, candle, step):
        if self.Position > 0:
            self.SellMarket(self.Position)
            self._reset_stops()

        tv = float(self.TradeVolume)
        if self.Position < 0 or tv <= 0:
            return

        self.SellMarket(tv)
        self._entry_price = float(candle.ClosePrice)
        sl = float(self.StopLossPoints)
        tp = float(self.TakeProfitPoints)
        self._stop_price = self._entry_price + sl * step if sl > 0 else 0.0
        self._take_profit_price = self._entry_price - tp * step if tp > 0 else 0.0

    def _reset_stops(self):
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    def OnReseted(self):
        super(master_mind2_strategy, self).OnReseted()
        self._reset_stops()

    def CreateClone(self):
        return master_mind2_strategy()
