import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class cci_ma_v15_strategy(Strategy):
    """
    CCI MA v1.5 strategy. Uses primary CCI with SMA signal line and secondary CCI for exits.
    """

    def __init__(self):
        super(cci_ma_v15_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 14).SetDisplay("CCI Period", "Length of the primary CCI", "CCI")
        self._signal_cci_period = self.Param("SignalCciPeriod", 14).SetDisplay("Exit CCI Period", "Length of the secondary CCI", "CCI")
        self._ma_period = self.Param("MaPeriod", 9).SetDisplay("CCI MA Period", "SMA length applied to the CCI", "CCI")
        self._stop_loss_points = self.Param("StopLossPoints", 500.0).SetDisplay("Stop Loss", "Protective stop distance in absolute points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 500.0).SetDisplay("Take Profit", "Profit target distance in absolute points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Market data series", "General")

        self._cci_history = []
        self._prev_cci_ma = None
        self._prev_cci = None
        self._prev_signal_cci = None

    @property
    def candle_type(self):
        return self._candle_type.Value
    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(cci_ma_v15_strategy, self).OnReseted()
        self._cci_history = []
        self._prev_cci_ma = None
        self._prev_cci = None
        self._prev_signal_cci = None

    def OnStarted(self, time):
        super(cci_ma_v15_strategy, self).OnStarted(time)

        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value
        signal_cci = CommodityChannelIndex()
        signal_cci.Length = self._signal_cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, signal_cci, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            ind_area = self.CreateChartArea()
            if ind_area is not None:
                self.DrawIndicator(ind_area, cci)
                self.DrawIndicator(ind_area, signal_cci)

        tp = Unit(self._take_profit_points.Value, UnitTypes.Absolute) if self._take_profit_points.Value > 0 else None
        sl = Unit(self._stop_loss_points.Value, UnitTypes.Absolute) if self._stop_loss_points.Value > 0 else None
        if tp is not None or sl is not None:
            self.StartProtection(tp, sl)

    def on_process(self, candle, cci_value, signal_cci_value):
        if candle.State != CandleStates.Finished:
            return

        self._cci_history.append(float(cci_value))
        ma_period = self._ma_period.Value
        if len(self._cci_history) > ma_period:
            self._cci_history.pop(0)

        cci_ma = None
        if len(self._cci_history) >= ma_period:
            cci_ma = sum(self._cci_history) / len(self._cci_history)

        if cci_ma is None or self._prev_cci is None or self._prev_cci_ma is None or self._prev_signal_cci is None:
            self._prev_cci = float(cci_value)
            self._prev_cci_ma = cci_ma
            self._prev_signal_cci = float(signal_cci_value)
            return

        # Exit logic
        if self.Position > 0 and self._prev_signal_cci > 100 and float(signal_cci_value) <= 100:
            self.SellMarket()
        elif self.Position < 0 and self._prev_signal_cci < -100 and float(signal_cci_value) >= -100:
            self.BuyMarket()

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_cci = float(cci_value)
            self._prev_cci_ma = cci_ma
            self._prev_signal_cci = float(signal_cci_value)
            return

        # Entry logic
        if self._prev_cci < self._prev_cci_ma and float(cci_value) > cci_ma and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_cci > self._prev_cci_ma and float(cci_value) < cci_ma and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_cci = float(cci_value)
        self._prev_cci_ma = cci_ma
        self._prev_signal_cci = float(signal_cci_value)

    def CreateClone(self):
        return cci_ma_v15_strategy()
