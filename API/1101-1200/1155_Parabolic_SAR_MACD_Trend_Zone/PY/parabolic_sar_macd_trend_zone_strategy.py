import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class parabolic_sar_macd_trend_zone_strategy(Strategy):
    """
    Parabolic SAR with MACD confirmation.
    A close crossing above the Parabolic SAR while the MACD line is above its signal line goes long, a close crossing below the SAR
    while MACD is below its signal goes short, reversing an opposite position. A position is closed on the opposite price/SAR
    crossover or on the opposite MACD/signal crossover.
    """

    def __init__(self):
        super(parabolic_sar_macd_trend_zone_strategy, self).__init__()
        self._sar_start = self.Param("SarStart", 0.02).SetGreaterThanZero().SetDisplay("SAR Start", "Initial SAR acceleration factor", "Indicators")
        self._sar_increment = self.Param("SarIncrement", 0.02).SetGreaterThanZero().SetDisplay("SAR Increment", "SAR acceleration factor increment", "Indicators")
        self._sar_max = self.Param("SarMax", 0.2).SetGreaterThanZero().SetDisplay("SAR Max", "Maximum SAR acceleration factor", "Indicators")
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast EMA period of MACD", "Indicators")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow EMA period of MACD", "Indicators")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal line period of MACD", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_sar = None
        self._prev_macd = None
        self._prev_signal = None

    def OnReseted(self):
        super(parabolic_sar_macd_trend_zone_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(parabolic_sar_macd_trend_zone_strategy, self).OnStarted2(time)

        self._reset_state()

        sar = ParabolicSar()
        sar.Acceleration = Decimal(self._sar_start.Value)
        sar.AccelerationStep = Decimal(self._sar_increment.Value)
        sar.AccelerationMax = Decimal(self._sar_max.Value)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sar, macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, sar_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not sar_value.IsFormed or not macd_value.IsFormed:
            return

        macd_line = macd_value.Macd
        signal_line = macd_value.Signal
        if macd_line is None or signal_line is None:
            return

        sar = sar_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        prev_close = self._prev_close
        prev_sar = self._prev_sar
        prev_macd = self._prev_macd
        prev_signal = self._prev_signal

        self._prev_close = close
        self._prev_sar = sar
        self._prev_macd = macd_line
        self._prev_signal = signal_line

        if prev_close is None or prev_sar is None or prev_macd is None or prev_signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cross_up = prev_close <= prev_sar and close > sar
        cross_down = prev_close >= prev_sar and close < sar
        macd_cross_up = prev_macd <= prev_signal and macd_line > signal_line
        macd_cross_down = prev_macd >= prev_signal and macd_line < signal_line

        if cross_up and macd_line > signal_line and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and macd_line < signal_line and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and (cross_down or macd_cross_down):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (cross_up or macd_cross_up):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return parabolic_sar_macd_trend_zone_strategy()
