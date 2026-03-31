import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class vwap_with_behavioral_bias_filter_strategy(Strategy):
    """
    VWAP with Behavioral Bias Filter strategy.
    """

    def __init__(self):
        super(vwap_with_behavioral_bias_filter_strategy, self).__init__()

        self._bias_threshold = self.Param("BiasThreshold", 0.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Bias Threshold", "Threshold for behavioral bias", "Behavioral Settings")

        self._bias_window_size = self.Param("BiasWindowSize", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bias Window Size", "Window size for behavioral bias calculation", "Behavioral Settings")

        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (%)", "Stop Loss percentage from entry price", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._vwap = None
        self._current_bias_score = 0.0
        self._recent_price_movements = []
        self._is_long = False
        self._is_short = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(vwap_with_behavioral_bias_filter_strategy, self).OnReseted()
        self._is_long = False
        self._is_short = False
        self._current_bias_score = 0.0
        self._recent_price_movements = []
        self._vwap = None

    def OnStarted2(self, time):
        super(vwap_with_behavioral_bias_filter_strategy, self).OnStarted2(time)

        self._vwap = VolumeWeightedMovingAverage()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._vwap, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._vwap)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(0),
            Unit(float(self._stop_loss.Value), UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, vwap_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self.UpdateBehavioralBias(candle)

        price = float(candle.ClosePrice)
        vwap = float(vwap_value)
        price_below_vwap = price < vwap
        price_above_vwap = price > vwap

        threshold = float(self._bias_threshold.Value)

        if price_below_vwap and self._current_bias_score < -threshold and not self._is_long and self.Position <= 0:
            self.BuyMarket(self.Volume)
            self._is_long = True
            self._is_short = False
        elif price_above_vwap and self._current_bias_score > threshold and not self._is_short and self.Position >= 0:
            self.SellMarket(self.Volume)
            self._is_short = True
            self._is_long = False

        if self._is_long and price_above_vwap and self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
            self._is_long = False
        elif self._is_short and price_below_vwap and self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
            self._is_short = False

    def UpdateBehavioralBias(self, candle):
        price_change = 0.0
        if candle.OpenPrice != 0:
            price_change = float((candle.ClosePrice - candle.OpenPrice) / candle.OpenPrice * 100)

        self._recent_price_movements.append(price_change)

        window = int(self._bias_window_size.Value)
        while len(self._recent_price_movements) > window:
            self._recent_price_movements.pop(0)

        if len(self._recent_price_movements) < 5:
            self._current_bias_score = 0.0
            return

        movements = self._recent_price_movements

        recent_movement = 0.0
        start = max(0, len(movements) - 5)
        for i in range(start, len(movements)):
            recent_movement += movements[i]

        total = 0.0
        total_sq = 0.0
        for m in movements:
            total += m
            total_sq += m * m

        avg = total / len(movements)
        variance = (total_sq / len(movements)) - (avg * avg)
        volatility = Math.Sqrt(max(0.0, variance))

        previous_move = 0.0
        consecutive_same = 0
        max_consecutive = 0
        for m in movements:
            if previous_move != 0 and Math.Sign(m) == Math.Sign(previous_move):
                consecutive_same += 1
                max_consecutive = max(max_consecutive, consecutive_same)
            else:
                consecutive_same = 0
            previous_move = m

        body_size = float(abs(candle.ClosePrice - candle.OpenPrice))
        total_size = float(candle.HighPrice - candle.LowPrice)
        body_ratio = body_size / total_size if total_size > 0 else 0.0

        self._current_bias_score = 0.0
        self._current_bias_score += min(0.5, max(-0.5, recent_movement / 2.0))
        self._current_bias_score += Math.Sign(recent_movement) * min(0.3, volatility / 10.0)
        self._current_bias_score += Math.Sign(recent_movement) * min(0.2, max_consecutive / 10.0)

        if candle.ClosePrice > candle.OpenPrice:
            self._current_bias_score += body_ratio * 0.2
        else:
            self._current_bias_score -= body_ratio * 0.2

        self._current_bias_score = max(-1.0, min(1.0, self._current_bias_score))

    def CreateClone(self):
        return vwap_with_behavioral_bias_filter_strategy()
