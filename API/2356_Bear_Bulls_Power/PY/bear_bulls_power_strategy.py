import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class bear_bulls_power_strategy(Strategy):
    def __init__(self):
        super(bear_bulls_power_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Timeframe of processed candles", "General")
        self._first_length = self.Param("FirstLength", 3) \
            .SetDisplay("Price MA Length", "Length of the first smoothing", "Indicator")
        self._second_length = self.Param("SecondLength", 2) \
            .SetDisplay("Signal MA Length", "Length of the second smoothing", "Indicator")
        self._price_ma = None
        self._signal_ma = None
        self._prev_value = None

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def first_length(self):
        return self._first_length.Value
    @property
    def second_length(self):
        return self._second_length.Value

    def OnReseted(self):
        super(bear_bulls_power_strategy, self).OnReseted()
        self._price_ma = None
        self._signal_ma = None
        self._prev_value = None

    def OnStarted(self, time):
        super(bear_bulls_power_strategy, self).OnStarted(time)
        self._price_ma = SimpleMovingAverage()
        self._price_ma.Length = self.first_length
        self._signal_ma = SimpleMovingAverage()
        self._signal_ma.Length = self.second_length
        self._prev_value = None
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        price_ma_input = DecimalIndicatorValue(self._price_ma, price, candle.OpenTime)
        price_ma_input.IsFinal = True
        price_ma_val = float(self._price_ma.Process(price_ma_input))

        diff = (float(candle.HighPrice) + float(candle.LowPrice) - 2.0 * price_ma_val) / 2.0
        signal_input = DecimalIndicatorValue(self._signal_ma, diff, candle.OpenTime)
        signal_input.IsFinal = True
        signal = float(self._signal_ma.Process(signal_input))

        if not self._price_ma.IsFormed or not self._signal_ma.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_value = signal
            return

        sec = self.Security
        threshold = (float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0) * 10.0

        if self._prev_value is not None:
            if self._prev_value <= -threshold and signal > threshold and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_value >= threshold and signal < -threshold and self.Position >= 0:
                self.SellMarket()

        self._prev_value = signal

    def CreateClone(self):
        return bear_bulls_power_strategy()
