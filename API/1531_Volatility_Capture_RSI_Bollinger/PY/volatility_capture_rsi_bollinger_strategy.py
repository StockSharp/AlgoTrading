import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class volatility_capture_rsi_bollinger_strategy(Strategy):
    def __init__(self):
        super(volatility_capture_rsi_bollinger_strategy, self).__init__()
        self._sma_length = self.Param("SmaLength", 50) \
            .SetDisplay("SMA Length", "Bollinger SMA period", "Indicators")
        self._bb_width = self.Param("BbWidth", 2.7) \
            .SetDisplay("BB Width", "Bollinger band width", "Indicators")
        self._rsi_length = self.Param("RsiLength", 10) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_buy = self.Param("RsiBuy", 55) \
            .SetDisplay("RSI Buy", "RSI above for buy signal", "Levels")
        self._rsi_sell = self.Param("RsiSell", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("RSI Sell", "RSI below for sell signal", "Levels")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown = 0

    @property
    def sma_length(self):
        return self._sma_length.Value

    @property
    def bb_width(self):
        return self._bb_width.Value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def rsi_buy(self):
        return self._rsi_buy.Value

    @property
    def rsi_sell(self):
        return self._rsi_sell.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volatility_capture_rsi_bollinger_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(volatility_capture_rsi_bollinger_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.sma_length
        std_dev = StandardDeviation()
        std_dev.Length = self.sma_length
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, std_dev, rsi, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_val, std_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            return
        lower = sma_val - self.bb_width * std_val
        upper = sma_val + self.bb_width * std_val
        # Buy when price near lower band (oversold)
        if candle.ClosePrice <= lower and rsi_val < self.rsi_sell and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 50
        # Sell when price near upper band (overbought)
        elif candle.ClosePrice >= upper and rsi_val > self.rsi_buy and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 50

    def CreateClone(self):
        return volatility_capture_rsi_bollinger_strategy()
