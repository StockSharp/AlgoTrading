import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class jupiter_m_strategy(Strategy):
    def __init__(self):
        super(jupiter_m_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 50) \
            .SetDisplay("CCI Period", "Period for CCI indicator", "Indicators")
        self._buy_level = self.Param("BuyLevel", -100.0) \
            .SetDisplay("Buy Level", "CCI level to buy (cross above)", "Trading")
        self._sell_level = self.Param("SellLevel", 100.0) \
            .SetDisplay("Sell Level", "CCI level to sell (cross below)", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._prev_cci = None

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def buy_level(self):
        return self._buy_level.Value

    @property
    def sell_level(self):
        return self._sell_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(jupiter_m_strategy, self).OnReseted()
        self._prev_cci = None

    def OnStarted(self, time):
        super(jupiter_m_strategy, self).OnStarted(time)
        self._prev_cci = None
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, cci):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        cci = float(cci)
        if self._prev_cci is None:
            self._prev_cci = cci
            return
        buy_level = float(self.buy_level)
        sell_level = float(self.sell_level)
        if self._prev_cci <= buy_level and cci > buy_level and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_cci >= sell_level and cci < sell_level and self.Position >= 0:
            self.SellMarket()
        self._prev_cci = cci

    def CreateClone(self):
        return jupiter_m_strategy()
