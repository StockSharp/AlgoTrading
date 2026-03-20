import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, CommodityChannelIndex, RelativeStrengthIndex, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class binary_wave_std_dev_strategy(Strategy):
    def __init__(self):
        super(binary_wave_std_dev_strategy, self).__init__()
        self._weight_ma = self.Param("WeightMa", 1.0) \
            .SetDisplay("MA Weight", "Weight for moving average direction", "Weights")
        self._weight_cci = self.Param("WeightCci", 1.0) \
            .SetDisplay("CCI Weight", "Weight for CCI direction", "Weights")
        self._weight_rsi = self.Param("WeightRsi", 1.0) \
            .SetDisplay("RSI Weight", "Weight for RSI", "Weights")
        self._ma_period = self.Param("MaPeriod", 13) \
            .SetDisplay("MA Period", "Moving average period", "Indicators")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "Lookback period for CCI", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Lookback for RSI", "Indicators")
        self._std_dev_period = self.Param("StdDevPeriod", 9) \
            .SetDisplay("StdDev Period", "Length of standard deviation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")

    @property
    def weight_ma(self):
        return self._weight_ma.Value

    @property
    def weight_cci(self):
        return self._weight_cci.Value

    @property
    def weight_rsi(self):
        return self._weight_rsi.Value

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def std_dev_period(self):
        return self._std_dev_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(binary_wave_std_dev_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ma_period
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        std_dev = StandardDeviation()
        std_dev.Length = self.std_dev_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, cci, rsi, std_dev, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ema_value, cci_value, rsi_value, std_dev_value):
        if candle.State != CandleStates.Finished:
            return
        ema_value = float(ema_value)
        cci_value = float(cci_value)
        rsi_value = float(rsi_value)
        close_price = float(candle.ClosePrice)
        w_ma = float(self.weight_ma)
        w_cci = float(self.weight_cci)
        w_rsi = float(self.weight_rsi)
        score = 0.0
        score += w_ma if close_price > ema_value else -w_ma
        score += w_cci if cci_value > 0 else -w_cci
        score += w_rsi if rsi_value > 50 else -w_rsi
        if score > 0 and self.Position <= 0:
            self.BuyMarket()
        elif score < 0 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return binary_wave_std_dev_strategy()
