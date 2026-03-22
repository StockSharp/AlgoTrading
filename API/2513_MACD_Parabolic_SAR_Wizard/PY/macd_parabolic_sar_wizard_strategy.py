import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, ParabolicSar
from StockSharp.Algo.Strategies import Strategy

class macd_parabolic_sar_wizard_strategy(Strategy):
    """
    MACD + Parabolic SAR wizard: weighted scoring system for entries/exits.
    """

    def __init__(self):
        super(macd_parabolic_sar_wizard_strategy, self).__init__()
        self._macd_fast = self.Param("MacdFastPeriod", 12).SetDisplay("MACD Fast", "Fast EMA period", "MACD")
        self._macd_slow = self.Param("MacdSlowPeriod", 24).SetDisplay("MACD Slow", "Slow EMA period", "MACD")
        self._macd_signal = self.Param("MacdSignalPeriod", 9).SetDisplay("MACD Signal", "Signal period", "MACD")
        self._macd_weight = self.Param("MacdWeight", 0.9).SetDisplay("MACD Weight", "Weight of MACD in scoring", "Scoring")
        self._sar_weight = self.Param("SarWeight", 0.1).SetDisplay("SAR Weight", "Weight of SAR in scoring", "Scoring")
        self._open_threshold = self.Param("OpenThreshold", 90.0).SetDisplay("Open Threshold", "Score to open", "Scoring")
        self._close_threshold = self.Param("CloseThreshold", 90.0).SetDisplay("Close Threshold", "Score to exit", "Scoring")
        self._sar_step = self.Param("SarStep", 0.02).SetDisplay("SAR Step", "Acceleration factor", "Parabolic SAR")
        self._sar_max = self.Param("SarMax", 0.2).SetDisplay("SAR Max", "Max acceleration", "Parabolic SAR")
        self._stop_loss_points = self.Param("StopLossPoints", 50.0).SetDisplay("Stop Loss", "SL in points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 115.0).SetDisplay("Take Profit", "TP in points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candles", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_parabolic_sar_wizard_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(macd_parabolic_sar_wizard_strategy, self).OnStarted(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value
        sar = ParabolicSar()
        sar.AccelerationStep = self._sar_step.Value
        sar.AccelerationMax = self._sar_max.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, sar, self._process_candle).Start()
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        tp = self._take_profit_points.Value * step if self._take_profit_points.Value > 0 else 0
        sl = self._stop_loss_points.Value * step if self._stop_loss_points.Value > 0 else 0
        if tp > 0 or sl > 0:
            self.StartProtection(
                Unit(tp, UnitTypes.Absolute) if tp > 0 else Unit(),
                Unit(sl, UnitTypes.Absolute) if sl > 0 else Unit(),
                True
            )
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_value, sar_value):
        if candle.State != CandleStates.Finished:
            return
        typed_val = macd_value
        macd_line = typed_val.Macd
        signal_line = typed_val.Signal
        if macd_line is None or signal_line is None:
            return
        macd_f = float(macd_line)
        signal_f = float(signal_line)
        sar_f = float(sar_value)
        close = float(candle.ClosePrice)
        macd_bull = 100.0 if macd_f > signal_f else 0.0
        macd_bear = 100.0 if macd_f < signal_f else 0.0
        sar_bull = 100.0 if close > sar_f else 0.0
        sar_bear = 100.0 if close < sar_f else 0.0
        mw = float(self._macd_weight.Value)
        sw = float(self._sar_weight.Value)
        bull_score = macd_bull * mw + sar_bull * sw
        bear_score = macd_bear * mw + sar_bear * sw
        ot = float(self._open_threshold.Value)
        ct = float(self._close_threshold.Value)
        if self.Position > 0 and bear_score >= ct:
            self.SellMarket()
            return
        if self.Position < 0 and bull_score >= ct:
            self.BuyMarket()
            return
        if self.Position <= 0 and bull_score >= ot:
            self.BuyMarket()
            return
        if self.Position >= 0 and bear_score >= ot:
            self.SellMarket()

    def CreateClone(self):
        return macd_parabolic_sar_wizard_strategy()
