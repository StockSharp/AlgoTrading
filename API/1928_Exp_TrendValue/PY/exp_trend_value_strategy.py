import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class exp_trend_value_strategy(Strategy):

    def __init__(self):
        super(exp_trend_value_strategy, self).__init__()

        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Allow Long Exit", "Allow closing long positions", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Allow Short Exit", "Allow closing short positions", "Trading")
        self._stop_loss_pips = self.Param("StopLossPips", 1000) \
            .SetDisplay("Stop Loss", "Stop loss in points", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 2000) \
            .SetDisplay("Take Profit", "Take profit in points", "Risk")
        self._ma_period = self.Param("MaPeriod", 13) \
            .SetDisplay("MA Period", "Weighted moving average period", "Indicator")
        self._shift_percent = self.Param("ShiftPercent", 0.05) \
            .SetDisplay("Shift Percent", "Percentage offset for bands", "Indicator")
        self._atr_period = self.Param("AtrPeriod", 15) \
            .SetDisplay("ATR Period", "Range average period", "Indicator")
        self._atr_sensitivity = self.Param("AtrSensitivity", 0.6) \
            .SetDisplay("ATR Sensitivity", "Multiplier for range shift", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")

        self._wma_high = WeightedMovingAverage()
        self._wma_low = WeightedMovingAverage()
        self._range_average = SimpleMovingAverage()
        self._prev_high_band = 0.0
        self._prev_low_band = 0.0
        self._prev_trend = 0
        self._initialized = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    @property
    def BuyPosOpen(self):
        return self._buy_pos_open.Value

    @BuyPosOpen.setter
    def BuyPosOpen(self, value):
        self._buy_pos_open.Value = value

    @property
    def SellPosOpen(self):
        return self._sell_pos_open.Value

    @SellPosOpen.setter
    def SellPosOpen(self, value):
        self._sell_pos_open.Value = value

    @property
    def BuyPosClose(self):
        return self._buy_pos_close.Value

    @BuyPosClose.setter
    def BuyPosClose(self, value):
        self._buy_pos_close.Value = value

    @property
    def SellPosClose(self):
        return self._sell_pos_close.Value

    @SellPosClose.setter
    def SellPosClose(self, value):
        self._sell_pos_close.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @TakeProfitPips.setter
    def TakeProfitPips(self, value):
        self._take_profit_pips.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def ShiftPercent(self):
        return self._shift_percent.Value

    @ShiftPercent.setter
    def ShiftPercent(self, value):
        self._shift_percent.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrSensitivity(self):
        return self._atr_sensitivity.Value

    @AtrSensitivity.setter
    def AtrSensitivity(self, value):
        self._atr_sensitivity.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(exp_trend_value_strategy, self).OnStarted(time)

        self._wma_high.Length = self.MaPeriod
        self._wma_low.Length = self.MaPeriod
        self._range_average.Length = self.AtrPeriod
        close_trigger = SimpleMovingAverage()
        close_trigger.Length = 1

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(close_trigger, self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, close_value):
        if candle.State != CandleStates.Finished:
            return

        hi_inp = DecimalIndicatorValue(self._wma_high, candle.HighPrice, candle.OpenTime)
        hi_inp.IsFinal = True
        high_ma = float(self._wma_high.Process(hi_inp))
        lo_inp = DecimalIndicatorValue(self._wma_low, candle.LowPrice, candle.OpenTime)
        lo_inp.IsFinal = True
        low_ma = float(self._wma_low.Process(lo_inp))

        if not self._wma_high.IsFormed or not self._wma_low.IsFormed:
            return

        range_val = candle.HighPrice - candle.LowPrice
        rng_inp = DecimalIndicatorValue(self._range_average, range_val, candle.OpenTime)
        rng_inp.IsFinal = True
        range_avg = float(self._range_average.Process(rng_inp))

        if not self._range_average.IsFormed:
            return

        cv = float(close_value)
        percent_offset = cv * float(self.ShiftPercent) / 100.0
        range_offset = range_avg * float(self.AtrSensitivity) * 0.25
        high_band = high_ma - range_offset + percent_offset
        low_band = low_ma + range_offset - percent_offset

        if not self._initialized:
            self._prev_high_band = high_band
            self._prev_low_band = low_band
            self._initialized = True
            return

        center_line = (high_band + low_band) / 2.0
        close_price = float(candle.ClosePrice)
        trend = 1 if close_price >= center_line else -1
        up_signal = trend > 0 and self._prev_trend <= 0
        down_signal = trend < 0 and self._prev_trend >= 0

        self._prev_high_band = high_band
        self._prev_low_band = low_band
        self._prev_trend = trend

        if up_signal and self.Position == 0:
            self.BuyMarket()
        elif down_signal and self.Position == 0:
            self.SellMarket()

    def OnReseted(self):
        super(exp_trend_value_strategy, self).OnReseted()
        self._wma_high.Reset()
        self._wma_low.Reset()
        self._range_average.Reset()
        self._prev_high_band = 0.0
        self._prev_low_band = 0.0
        self._prev_trend = 0
        self._initialized = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    def CreateClone(self):
        return exp_trend_value_strategy()
