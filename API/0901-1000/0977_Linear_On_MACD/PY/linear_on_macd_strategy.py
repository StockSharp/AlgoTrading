import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, LinearReg
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class linear_on_macd_strategy(Strategy):
    """
    Linear On MACD strategy.
    One MACD runs on the close and another on candle volume, and a Lookback linear regression projects the price. A long opens when
    both MACDs are above their signal lines and the regression price lies between the candle open and close; a short opens when both
    are below their signals under the same regression condition. With RiskHigh enabled one MACD agreeing is enough. Opposite signals
    reverse the position.
    """

    def __init__(self):
        super(linear_on_macd_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12).SetGreaterThanZero().SetDisplay("Fast Length", "Fast EMA period of both MACDs", "MACD")
        self._slow_length = self.Param("SlowLength", 26).SetGreaterThanZero().SetDisplay("Slow Length", "Slow EMA period of both MACDs", "MACD")
        self._signal_length = self.Param("SignalLength", 9).SetGreaterThanZero().SetDisplay("Signal Length", "Signal line period of both MACDs", "MACD")
        self._lookback = self.Param("Lookback", 21).SetGreaterThanZero().SetDisplay("Lookback", "Linear regression lookback", "Regression")
        self._risk_high = self.Param("RiskHigh", False).SetDisplay("Risk High", "Accept a signal when only one MACD agrees", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_macd = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _create_macd(self):
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_length.Value
        macd.Macd.LongMa.Length = self._slow_length.Value
        macd.SignalMa.Length = self._signal_length.Value
        return macd

    def OnStarted2(self, time):
        super(linear_on_macd_strategy, self).OnStarted2(time)

        price_macd = self._create_macd()
        self._volume_macd = self._create_macd()
        regression = LinearReg()
        regression.Length = self._lookback.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(price_macd, regression, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, regression)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, price_macd)

    def _process_candle(self, candle, price_macd_value, regression_value):
        if candle.State != CandleStates.Finished:
            return

        volume_macd_value = process_float(self._volume_macd, candle.TotalVolume, candle.OpenTime, True)

        if not price_macd_value.IsFormed or not regression_value.IsFormed or not volume_macd_value.IsFormed:
            return

        price_macd = price_macd_value.Macd
        price_signal = price_macd_value.Signal
        volume_macd = volume_macd_value.Macd
        volume_signal = volume_macd_value.Signal
        if price_macd is None or price_signal is None or volume_macd is None or volume_signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        predicted = regression_value.GetValue[Decimal](None)
        body_low = min(candle.OpenPrice, candle.ClosePrice)
        body_high = max(candle.OpenPrice, candle.ClosePrice)
        in_body = predicted >= body_low and predicted <= body_high

        price_up = price_macd > price_signal
        volume_up = volume_macd > volume_signal
        price_down = price_macd < price_signal
        volume_down = volume_macd < volume_signal

        if self._risk_high.Value:
            bullish = price_up or volume_up
            bearish = price_down or volume_down
        else:
            bullish = price_up and volume_up
            bearish = price_down and volume_down

        if in_body and bullish and not bearish and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif in_body and bearish and not bullish and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return linear_on_macd_strategy()
