import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class linear_cross_trading_strategy(Strategy):
    """
    Linear Cross Trading strategy.
    Regresses the close on volume over Length candles and predicts the price for the current volume. A long opens when the
    predicted price crosses above its LinearLength WMA while MACD is above its signal and rising. A short opens when MACD is
    below its signal and falling while the low is lower than the previous low. Opposite signals reverse the position.
    """

    def __init__(self):
        super(linear_cross_trading_strategy, self).__init__()
        self._length = self.Param("Length", 21).SetGreaterThanZero().SetDisplay("Length", "Number of candles in the price-on-volume regression", "Indicators")
        self._linear_length = self.Param("LinearLength", 9).SetGreaterThanZero().SetDisplay("Linear Length", "WMA period applied to the predicted price", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._wma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._window = []
        self._prev_predicted = None
        self._prev_wma = None
        self._prev_macd = None
        self._prev_low = None

    def OnReseted(self):
        super(linear_cross_trading_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(linear_cross_trading_strategy, self).OnStarted2(time)

        self._reset_state()

        self._wma = WeightedMovingAverage()
        self._wma.Length = self._linear_length.Value
        macd = MovingAverageConvergenceDivergenceSignal()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._wma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        prev_low = self._prev_low
        self._prev_low = candle.LowPrice

        length = self._length.Value
        self._window.append((candle.ClosePrice, candle.TotalVolume))
        while len(self._window) > length:
            self._window.pop(0)

        predicted = None
        wma = None
        if len(self._window) == length:
            predicted = self._predict(candle.TotalVolume)
            wma_value = process_float(self._wma, predicted, candle.OpenTime, True)
            if self._wma.IsFormed:
                wma = wma_value.GetValue[Decimal](None)

        prev_predicted = self._prev_predicted
        prev_wma = self._prev_wma
        self._prev_predicted = predicted
        self._prev_wma = wma

        if not macd_value.IsFormed or macd_value.Macd is None or macd_value.Signal is None:
            return

        macd = macd_value.Macd
        signal = macd_value.Signal
        prev_macd = self._prev_macd
        self._prev_macd = macd

        if prev_macd is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        cross_up = predicted is not None and wma is not None and prev_predicted is not None and prev_wma is not None \
            and prev_predicted <= prev_wma and predicted > wma
        macd_up = macd > signal and macd > prev_macd
        macd_down = macd < signal and macd < prev_macd
        lower_low = prev_low is not None and candle.LowPrice < prev_low

        if cross_up and macd_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif macd_down and lower_low and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def _predict(self, volume):
        # Least-squares fit close = a + b * volume over the window, evaluated at the current volume.
        n = Decimal(len(self._window))
        mean_volume = sum((v for _, v in self._window), Decimal(0)) / n
        mean_close = sum((c for c, _ in self._window), Decimal(0)) / n

        covariance = Decimal(0)
        variance = Decimal(0)
        for close, vol in self._window:
            dv = vol - mean_volume
            covariance += dv * (close - mean_close)
            variance += dv * dv

        slope = Decimal(0) if variance == Decimal(0) else covariance / variance
        return mean_close + slope * (volume - mean_volume)

    def CreateClone(self):
        return linear_cross_trading_strategy()
