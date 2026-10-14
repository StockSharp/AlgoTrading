import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class parabolic_sar_early_buy_ma_based_exit_strategy(Strategy):
    """
    Parabolic SAR early buy with moving average exit.
    A close crossing above the Parabolic SAR goes long and a close crossing below it goes short, reversing an opposite position.
    A long is also closed early when the SAR is above the close and the close is below the MaPeriod simple moving average.
    """

    def __init__(self):
        super(parabolic_sar_early_buy_ma_based_exit_strategy, self).__init__()
        self._acceleration = self.Param("Acceleration", 0.02).SetGreaterThanZero().SetDisplay("Acceleration", "Initial SAR acceleration factor", "Indicators")
        self._acceleration_step = self.Param("AccelerationStep", 0.02).SetGreaterThanZero().SetDisplay("Acceleration Step", "SAR acceleration factor increment", "Indicators")
        self._max_acceleration = self.Param("MaxAcceleration", 0.2).SetGreaterThanZero().SetDisplay("Max Acceleration", "Maximum SAR acceleration factor", "Indicators")
        self._ma_period = self.Param("MaPeriod", 11).SetGreaterThanZero().SetDisplay("MA Period", "Period of the exit moving average", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_close = None
        self._prev_sar = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(parabolic_sar_early_buy_ma_based_exit_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_sar = None

    def OnStarted2(self, time):
        super(parabolic_sar_early_buy_ma_based_exit_strategy, self).OnStarted2(time)

        self._prev_close = None
        self._prev_sar = None

        sar = ParabolicSar()
        sar.Acceleration = Decimal(self._acceleration.Value)
        sar.AccelerationStep = Decimal(self._acceleration_step.Value)
        sar.AccelerationMax = Decimal(self._max_acceleration.Value)
        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sar, ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sar, ma):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        prev_close = self._prev_close
        prev_sar = self._prev_sar
        self._prev_close = close
        self._prev_sar = sar

        if prev_close is None or prev_sar is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cross_up = prev_close <= prev_sar and close > sar
        cross_down = prev_close >= prev_sar and close < sar

        if cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and sar > close and close < ma:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return parabolic_sar_early_buy_ma_based_exit_strategy()
