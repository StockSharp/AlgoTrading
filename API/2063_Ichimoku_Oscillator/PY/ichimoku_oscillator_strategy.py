import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Ichimoku, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class ichimoku_oscillator_strategy(Strategy):

    def __init__(self):
        super(ichimoku_oscillator_strategy, self).__init__()

        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetDisplay("Tenkan Period", "Period for Tenkan-sen line", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetDisplay("Kijun Period", "Period for Kijun-sen line", "Ichimoku")
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetDisplay("Senkou Span B Period", "Period for Senkou Span B", "Ichimoku")
        self._smoothing_period = self.Param("SmoothingPeriod", 7) \
            .SetDisplay("Smoothing Period", "Period for smoothing EMA", "Oscillator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculation", "Main")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss in percent", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 4.0) \
            .SetDisplay("Take Profit %", "Take profit in percent", "Risk")

        self._ema = None
        self._prev_value = None
        self._prev_prev_value = None

    @property
    def TenkanPeriod(self):
        return self._tenkan_period.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkan_period.Value = value

    @property
    def KijunPeriod(self):
        return self._kijun_period.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijun_period.Value = value

    @property
    def SenkouSpanBPeriod(self):
        return self._senkou_span_b_period.Value

    @SenkouSpanBPeriod.setter
    def SenkouSpanBPeriod(self, value):
        self._senkou_span_b_period.Value = value

    @property
    def SmoothingPeriod(self):
        return self._smoothing_period.Value

    @SmoothingPeriod.setter
    def SmoothingPeriod(self, value):
        self._smoothing_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def TakeProfitPercent(self):
        return self._take_profit_percent.Value

    @TakeProfitPercent.setter
    def TakeProfitPercent(self, value):
        self._take_profit_percent.Value = value

    def OnStarted2(self, time):
        super(ichimoku_oscillator_strategy, self).OnStarted2(time)

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self.TenkanPeriod
        ichimoku.Kijun.Length = self.KijunPeriod
        ichimoku.SenkouB.Length = self.SenkouSpanBPeriod

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.SmoothingPeriod

        self.Indicators.Add(self._ema)

        self._prev_value = None
        self._prev_prev_value = None

        self.SubscribeCandles(self.CandleType) \
            .BindEx(ichimoku, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(float(self.TakeProfitPercent), UnitTypes.Percent),
            stopLoss=Unit(float(self.StopLossPercent), UnitTypes.Percent),
            useMarketOrders=True
        )

    def ProcessCandle(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        if not ichimoku_value.IsFormed:
            return

        chikou = ichimoku_value.Chinkou
        span_b = ichimoku_value.SenkouB
        tenkan = ichimoku_value.Tenkan
        kijun = ichimoku_value.Kijun

        if chikou is None or span_b is None or tenkan is None or kijun is None:
            return

        osc = (chikou - span_b) - (tenkan - kijun)

        t = candle.OpenTime
        ema_input = DecimalIndicatorValue(self._ema, osc, t)
        ema_input.IsFinal = True
        ema_result = self._ema.Process(ema_input)
        if not ema_result.IsFormed:
            return

        current = float(ema_result)

        if self._prev_value is not None and self._prev_prev_value is not None:
            prev = self._prev_value
            prev_prev = self._prev_prev_value

            rising = prev > prev_prev
            falling = prev < prev_prev

            if rising and current >= prev and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif falling and current <= prev and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_prev_value = self._prev_value
        self._prev_value = current

    def OnReseted(self):
        super(ichimoku_oscillator_strategy, self).OnReseted()
        self._ema = None
        self._prev_value = None
        self._prev_prev_value = None

    def CreateClone(self):
        return ichimoku_oscillator_strategy()
