import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class color_x_adx_strategy(Strategy):

    def __init__(self):
        super(color_x_adx_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 20.0) \
            .SetDisplay("ADX Threshold", "Minimum ADX level for trades", "Indicators")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_plus_di = None
        self._prev_minus_di = None

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def AdxThreshold(self):
        return self._adx_threshold.Value

    @AdxThreshold.setter
    def AdxThreshold(self, value):
        self._adx_threshold.Value = value

    @property
    def StopLossPct(self):
        return self._stop_loss_pct.Value

    @StopLossPct.setter
    def StopLossPct(self, value):
        self._stop_loss_pct.Value = value

    @property
    def TakeProfitPct(self):
        return self._take_profit_pct.Value

    @TakeProfitPct.setter
    def TakeProfitPct(self, value):
        self._take_profit_pct.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(color_x_adx_strategy, self).OnStarted(time)

        self._prev_plus_di = None
        self._prev_minus_di = None

        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod

        self.SubscribeCandles(self.CandleType) \
            .BindEx(adx, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(float(self.TakeProfitPct), UnitTypes.Percent),
            stopLoss=Unit(float(self.StopLossPct), UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        if not adx_value.IsFormed:
            return

        plus_di = adx_value.Dx.Plus
        minus_di = adx_value.Dx.Minus
        adx_main = adx_value.MovingAverage

        if plus_di is None or minus_di is None or adx_main is None:
            return

        plus_di_f = float(plus_di)
        minus_di_f = float(minus_di)
        adx_main_f = float(adx_main)
        threshold = float(self.AdxThreshold)

        if self._prev_plus_di is None or self._prev_minus_di is None:
            self._prev_plus_di = plus_di_f
            self._prev_minus_di = minus_di_f
            return

        if (plus_di_f > minus_di_f and self._prev_plus_di <= self._prev_minus_di and
                adx_main_f > threshold and self.Position <= 0):
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif (minus_di_f > plus_di_f and self._prev_minus_di <= self._prev_plus_di and
              adx_main_f > threshold and self.Position >= 0):
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_plus_di = plus_di_f
        self._prev_minus_di = minus_di_f

    def OnReseted(self):
        super(color_x_adx_strategy, self).OnReseted()
        self._prev_plus_di = None
        self._prev_minus_di = None

    def CreateClone(self):
        return color_x_adx_strategy()
