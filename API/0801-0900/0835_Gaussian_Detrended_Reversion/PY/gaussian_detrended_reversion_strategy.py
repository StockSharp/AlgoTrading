import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, ArnaudLegouxMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class gaussian_detrended_reversion_strategy(Strategy):
    """
    Gaussian detrended reversion strategy.
    The detrended price oscillator is the close minus the PriceLength EMA from PriceLength / 2 + 1 candles ago. It is smoothed with an
    ALMA of SmoothingLength, and the lag line is that value LagLength candles ago. The smoothed oscillator crossing above the lag line
    below zero goes long, crossing below it above zero goes short. A long closes on a cross below the lag line or above zero, a short
    on a cross above the lag line or below zero.
    """

    def __init__(self):
        super(gaussian_detrended_reversion_strategy, self).__init__()
        self._price_length = self.Param("PriceLength", 52).SetGreaterThanZero().SetDisplay("Price Length", "EMA length used to detrend the price", "Indicators")
        self._smoothing_length = self.Param("SmoothingLength", 52).SetGreaterThanZero().SetDisplay("Smoothing Length", "ALMA smoothing length", "Indicators")
        self._lag_length = self.Param("LagLength", 26).SetGreaterThanZero().SetDisplay("Lag Length", "Candles back for the lag line", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._alma = None
        self._ema_history = []
        self._smooth_history = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gaussian_detrended_reversion_strategy, self).OnReseted()
        self._ema_history = []
        self._smooth_history = []

    def OnStarted2(self, time):
        super(gaussian_detrended_reversion_strategy, self).OnStarted2(time)

        self._ema_history = []
        self._smooth_history = []

        ema = ExponentialMovingAverage()
        ema.Length = self._price_length.Value
        self._alma = ArnaudLegouxMovingAverage()
        self._alma.Length = self._smoothing_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema):
        if candle.State != CandleStates.Finished:
            return

        bars_back = self._price_length.Value // 2 + 1
        lag_length = self._lag_length.Value

        self._ema_history.append(ema)
        if len(self._ema_history) > bars_back + 1:
            self._ema_history.pop(0)

        if len(self._ema_history) <= bars_back:
            return

        dpo = candle.ClosePrice - self._ema_history[0]
        alma_input = DecimalIndicatorValue(self._alma, dpo, candle.OpenTime)
        alma_input.IsFinal = True
        alma_value = self._alma.Process(alma_input)

        if not self._alma.IsFormed:
            return

        # The history keeps the current value, the previous one and the lag values for both.
        self._smooth_history.append(alma_value.GetValue[Decimal](None))
        if len(self._smooth_history) > lag_length + 2:
            self._smooth_history.pop(0)

        if len(self._smooth_history) < lag_length + 2:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        count = len(self._smooth_history)
        smooth = self._smooth_history[count - 1]
        prev_smooth = self._smooth_history[count - 2]
        lag = self._smooth_history[count - 1 - lag_length]
        prev_lag = self._smooth_history[count - 2 - lag_length]

        cross_up = prev_smooth <= prev_lag and smooth > lag
        cross_down = prev_smooth >= prev_lag and smooth < lag
        zero_up = prev_smooth <= 0 and smooth > 0
        zero_down = prev_smooth >= 0 and smooth < 0

        if cross_up and smooth < 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and smooth > 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and (cross_down or zero_up):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (cross_up or zero_down):
            self.BuyMarket(abs(self.Position))

    def CreateClone(self):
        return gaussian_detrended_reversion_strategy()
