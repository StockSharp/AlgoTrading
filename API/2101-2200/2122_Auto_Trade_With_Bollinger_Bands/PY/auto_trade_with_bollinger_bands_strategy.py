import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class auto_trade_with_bollinger_bands_strategy(Strategy):
    def __init__(self):
        super(auto_trade_with_bollinger_bands_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._bb_period = self.Param("BbPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 6) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def bb_period(self):
        return self._bb_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value

    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value

    def OnStarted2(self, time):
        super(auto_trade_with_bollinger_bands_strategy, self).OnStarted2(time)
        bollinger = BollingerBands()
        bollinger.Length = self.bb_period
        bollinger.Width = 2.0
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, bb_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return
        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        middle = float(bb_value.MovingAverage)
        rsi = float(rsi_value)
        price = float(candle.ClosePrice)
        ob = float(self.rsi_overbought)
        os_level = float(self.rsi_oversold)
        # Sell when price above upper band and RSI overbought
        if price > upper and rsi > ob and self.Position >= 0:
            self.SellMarket()
        # Buy when price below lower band and RSI oversold
        elif price < lower and rsi < os_level and self.Position <= 0:
            self.BuyMarket()
        # Exit long when price returns to middle
        elif self.Position > 0 and price >= middle:
            self.SellMarket()
        # Exit short when price returns to middle
        elif self.Position < 0 and price <= middle:
            self.BuyMarket()

    def CreateClone(self):
        return auto_trade_with_bollinger_bands_strategy()
