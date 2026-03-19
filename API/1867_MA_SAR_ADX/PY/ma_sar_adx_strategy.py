import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ParabolicSar, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class ma_sar_adx_strategy(Strategy):
    """
    SMA + Parabolic SAR + RSI strategy.
    Buys when price > SMA and > SAR and RSI >= long level.
    """

    def __init__(self):
        super(ma_sar_adx_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 100).SetDisplay("MA Period", "SMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "RSI period", "Indicators")
        self._sar_step = self.Param("SarStep", 0.02).SetDisplay("SAR Step", "SAR acceleration", "Indicators")
        self._sar_max = self.Param("SarMax", 0.1).SetDisplay("SAR Max", "SAR max acceleration", "Indicators")
        self._rsi_long = self.Param("RsiLongLevel", 52.0).SetDisplay("RSI Long", "Min RSI for long", "Filters")
        self._rsi_short = self.Param("RsiShortLevel", 48.0).SetDisplay("RSI Short", "Max RSI for short", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 3).SetDisplay("Cooldown", "Bars after position change", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Timeframe", "General")
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_sar_adx_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(ma_sar_adx_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        sar = ParabolicSar()
        sar.Acceleration = self._sar_step.Value
        sar.AccelerationMax = self._sar_max.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, sar, rsi, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val, sar_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if not ma_val.IsFinal or not sar_val.IsFinal or not rsi_val.IsFinal:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        price = float(candle.ClosePrice)
        ma = float(ma_val.ToDecimal())
        sar = float(sar_val.ToDecimal())
        rsi = float(rsi_val.ToDecimal())
        long_signal = price > ma and price > sar and rsi >= self._rsi_long.Value
        short_signal = price < ma and price < sar and rsi <= self._rsi_short.Value
        long_exit = price < sar or price < ma
        short_exit = price > sar or price > ma
        if self.Position == 0 and self._cooldown_remaining == 0:
            if long_signal:
                self.BuyMarket()
                self._cooldown_remaining = self._cooldown_bars.Value
            elif short_signal:
                self.SellMarket()
                self._cooldown_remaining = self._cooldown_bars.Value
        elif self.Position > 0 and long_exit:
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        elif self.Position < 0 and short_exit:
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value

    def CreateClone(self):
        return ma_sar_adx_strategy()
