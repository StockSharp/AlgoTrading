import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class exp_ma_rounding_channel_strategy(Strategy):

    def __init__(self):
        super(exp_ma_rounding_channel_strategy, self).__init__()

        self._ma_length = self.Param("MaLength", 12) \
            .SetDisplay("MA Period", "Length of moving average", "Indicator")

        self._atr_period = self.Param("AtrPeriod", 12) \
            .SetDisplay("ATR Period", "ATR period for channel width", "Indicator")

        self._atr_factor = self.Param("AtrFactor", 2.0) \
            .SetDisplay("ATR Factor", "Multiplier for ATR channel", "Indicator")

        self._round_step = self.Param("RoundStep", 50.0) \
            .SetDisplay("Round Step", "Rounding step for the moving average", "Indicator")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for calculation", "General")

        self._atr = None
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_close = 0.0

    @property
    def MaLength(self):
        return self._ma_length.Value

    @MaLength.setter
    def MaLength(self, value):
        self._ma_length.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrFactor(self):
        return self._atr_factor.Value

    @AtrFactor.setter
    def AtrFactor(self, value):
        self._atr_factor.Value = value

    @property
    def RoundStep(self):
        return self._round_step.Value

    @RoundStep.setter
    def RoundStep(self, value):
        self._round_step.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(exp_ma_rounding_channel_strategy, self).OnStarted(time)

        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0

        ema = ExponentialMovingAverage()
        ema.Length = self.MaLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(ema, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(3, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        atr_result = self._atr.Process(candle)
        if not atr_result.IsFormed:
            return

        atr_value = float(atr_result)

        step = float(self.RoundStep)
        ma_val = float(ma_value)
        if step > 0:
            rounded_ma = round(ma_val / step) * step
        else:
            rounded_ma = ma_val

        upper = rounded_ma + atr_value * float(self.AtrFactor)
        lower = rounded_ma - atr_value * float(self.AtrFactor)

        if self._prev_close != 0.0:
            close_price = float(candle.ClosePrice)
            break_up = self._prev_close <= self._prev_upper and close_price > upper
            break_down = self._prev_close >= self._prev_lower and close_price < lower

            if break_up and self.Position == 0:
                self.BuyMarket()
            elif break_down and self.Position == 0:
                self.SellMarket()

        self._prev_upper = upper
        self._prev_lower = lower
        self._prev_close = float(candle.ClosePrice)

    def OnReseted(self):
        super(exp_ma_rounding_channel_strategy, self).OnReseted()
        self._atr = None
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_close = 0.0

    def CreateClone(self):
        return exp_ma_rounding_channel_strategy()
