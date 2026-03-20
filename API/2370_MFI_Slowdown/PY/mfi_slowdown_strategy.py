import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy


class mfi_slowdown_strategy(Strategy):
    def __init__(self):
        super(mfi_slowdown_strategy, self).__init__()
        self._mfi_period = self.Param("MfiPeriod", 2) \
            .SetDisplay("MFI Period", "Period for the MFI indicator", "Indicator")
        self._upper_threshold = self.Param("UpperThreshold", 90.0) \
            .SetDisplay("Upper Threshold", "MFI upper level", "Signal")
        self._lower_threshold = self.Param("LowerThreshold", 10.0) \
            .SetDisplay("Lower Threshold", "MFI lower level", "Signal")
        self._seek_slowdown = self.Param("SeekSlowdown", True) \
            .SetDisplay("Seek Slowdown", "Require MFI to slow down", "Signal")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Open Long", "Allow opening long positions", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Close Long", "Allow closing long positions", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Open Short", "Allow opening short positions", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Close Short", "Allow closing short positions", "Trading")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetDisplay("Take Profit %", "Take-profit percentage", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(6))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_mfi = None

    @property
    def mfi_period(self):
        return self._mfi_period.Value

    @property
    def upper_threshold(self):
        return self._upper_threshold.Value

    @property
    def lower_threshold(self):
        return self._lower_threshold.Value

    @property
    def seek_slowdown(self):
        return self._seek_slowdown.Value

    @property
    def buy_pos_open(self):
        return self._buy_pos_open.Value

    @property
    def buy_pos_close(self):
        return self._buy_pos_close.Value

    @property
    def sell_pos_open(self):
        return self._sell_pos_open.Value

    @property
    def sell_pos_close(self):
        return self._sell_pos_close.Value

    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mfi_slowdown_strategy, self).OnReseted()
        self._prev_mfi = None

    def OnStarted(self, time):
        super(mfi_slowdown_strategy, self).OnStarted(time)
        self._prev_mfi = None
        mfi = MoneyFlowIndex()
        mfi.Length = int(self.mfi_period)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(mfi, self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(float(self.take_profit_percent), UnitTypes.Percent),
            stopLoss=Unit(float(self.stop_loss_percent), UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, mfi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, mfi_value):
        if candle.State != CandleStates.Finished:
            return
        mfi_value = float(mfi_value)
        slowdown = self._prev_mfi is not None and abs(mfi_value - self._prev_mfi) < 1.0
        ut = float(self.upper_threshold)
        lt = float(self.lower_threshold)
        up_signal = mfi_value >= ut and (not self.seek_slowdown or slowdown)
        down_signal = mfi_value <= lt and (not self.seek_slowdown or slowdown)
        if up_signal:
            if self.sell_pos_close and self.Position < 0:
                self.BuyMarket()
            if self.buy_pos_open and self.Position <= 0:
                self.BuyMarket()
        elif down_signal:
            if self.buy_pos_close and self.Position > 0:
                self.SellMarket()
            if self.sell_pos_open and self.Position >= 0:
                self.SellMarket()
        self._prev_mfi = mfi_value

    def CreateClone(self):
        return mfi_slowdown_strategy()
