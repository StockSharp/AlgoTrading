import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class hancock_rsi_volume_strategy(Strategy):
    """
    Hancock RSI Volume strategy.
    Each candle's volume is split into bullish and bearish volume: with UseWicks by where the close sits inside the high-low range,
    otherwise wholly by the candle colour. The RSI is 100 * bullish / (bullish + bearish) of the Wilder averages of both over
    RsiLength candles. The trend turns up when the RSI rises by more than Threshold from the previous candle and down when it falls
    by more than Threshold; a switch to up buys and a switch to down sells, reversing an opposite position.
    """

    def __init__(self):
        super(hancock_rsi_volume_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "Indicators")
        self._threshold = self.Param("Threshold", 0.1).SetNotNegative().SetDisplay("Threshold", "Minimum RSI change that switches the trend", "Indicators")
        self._use_wicks = self.Param("UseWicks", True).SetDisplay("Use Wicks", "Split the volume by the close position inside the candle range", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._bull_avg = 0.0
        self._bear_avg = 0.0
        self._samples = 0
        self._prev_rsi = None
        self._trend = 0

    def OnReseted(self):
        super(hancock_rsi_volume_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(hancock_rsi_volume_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        volume = float(candle.TotalVolume)
        open_price = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self._use_wicks.Value:
            rng = high - low
            if rng > 0:
                bull = volume * (close - low) / rng
                bear = volume - bull
            else:
                bull = volume / 2.0
                bear = volume / 2.0
        elif close > open_price:
            bull = volume
            bear = 0.0
        elif close < open_price:
            bull = 0.0
            bear = volume
        else:
            bull = volume / 2.0
            bear = volume / 2.0

        # Wilder averages seeded with the simple mean of the first RsiLength candles.
        length = self._rsi_length.Value
        if self._samples < length:
            self._samples += 1
            self._bull_avg += (bull - self._bull_avg) / self._samples
            self._bear_avg += (bear - self._bear_avg) / self._samples
            if self._samples < length:
                return
        else:
            self._bull_avg = (self._bull_avg * (length - 1) + bull) / length
            self._bear_avg = (self._bear_avg * (length - 1) + bear) / length

        total = self._bull_avg + self._bear_avg
        if total <= 0:
            return

        rsi = 100.0 * self._bull_avg / total
        prev_rsi = self._prev_rsi
        self._prev_rsi = rsi

        if prev_rsi is None:
            return

        prev_trend = self._trend
        change = rsi - prev_rsi
        threshold = float(self._threshold.Value)

        if change > threshold:
            self._trend = 1
        elif change < -threshold:
            self._trend = -1

        if self._trend == prev_trend:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._trend > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif self._trend < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return hancock_rsi_volume_strategy()
