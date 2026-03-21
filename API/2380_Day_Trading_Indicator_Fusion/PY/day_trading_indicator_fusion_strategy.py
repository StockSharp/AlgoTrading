import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (
    MovingAverageConvergenceDivergenceSignal,
    StochasticOscillator,
    ParabolicSar,
)
from StockSharp.Algo.Strategies import Strategy


class day_trading_indicator_fusion_strategy(Strategy):
    def __init__(self):
        super(day_trading_indicator_fusion_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1.0)
        self._take_profit = self.Param("TakeProfit", 50.0)
        self._trailing_stop = self.Param("TrailingStop", 25.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._prev_sar = 0.0

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @TradeVolume.setter
    def TradeVolume(self, value):
        self._trade_volume.Value = value

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

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(day_trading_indicator_fusion_strategy, self).OnStarted(time)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = 12
        macd.Macd.LongMa.Length = 26
        macd.SignalMa.Length = 9

        stochastic = StochasticOscillator()
        stochastic.K.Length = 5
        stochastic.D.Length = 3

        parabolic_sar = ParabolicSar()
        parabolic_sar.Acceleration = 0.02
        parabolic_sar.AccelerationMax = 0.2

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, stochastic, parabolic_sar, self.ProcessCandle).Start()

        pip = float(self.Security.PriceStep) if self.Security is not None else 1.0
        tp = Unit(float(self.TakeProfit) * pip, UnitTypes.Absolute) if self.TakeProfit > 0 else None
        sl = Unit(float(self.TrailingStop) * pip, UnitTypes.Absolute) if self.TrailingStop > 0 else None
        self.StartProtection(tp, sl, self.TrailingStop > 0)

    def ProcessCandle(self, candle, macd_value, stoch_value, sar_value):
        if candle.State != CandleStates.Finished:
            return

        macd_typed = macd_value
        macd_v = float(macd_typed.Macd) if macd_typed.Macd is not None else None
        macd_signal = float(macd_typed.Signal) if macd_typed.Signal is not None else None
        if macd_v is None or macd_signal is None:
            return

        stoch_typed = stoch_value
        stoch_k = float(stoch_typed.K) if stoch_typed.K is not None else None
        stoch_d = float(stoch_typed.D) if stoch_typed.D is not None else None
        if stoch_k is None or stoch_d is None:
            return

        sar = float(sar_value)

        is_buying = sar <= float(candle.ClosePrice) and self._prev_sar > sar and macd_v < macd_signal and stoch_k < 35.0
        is_selling = sar >= float(candle.ClosePrice) and self._prev_sar < sar and macd_v > macd_signal and stoch_k > 60.0

        self._prev_sar = sar

        if is_buying and self.Position <= 0:
            self.BuyMarket()
        elif is_selling and self.Position >= 0:
            self.SellMarket()

    def OnReseted(self):
        super(day_trading_indicator_fusion_strategy, self).OnReseted()
        self._prev_sar = 0.0

    def CreateClone(self):
        return day_trading_indicator_fusion_strategy()
