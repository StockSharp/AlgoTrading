import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class donchian_cci_strategy(Strategy):
    """
    Strategy based on Donchian Channels and CCI indicators.
    Buys when price touches upper Donchian band with CCI > 0.
    Sells when price touches lower Donchian band with CCI < 0.
    Exits when price crosses middle band.
    """

    def __init__(self):
        super(donchian_cci_strategy, self).__init__()
        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetDisplay("Donchian Period", "Period for Donchian Channel", "Indicators")
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "Period for CCI indicator", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop-Loss %", "Stop-loss percentage from entry price", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchian_cci_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(donchian_cci_strategy, self).OnStarted(time)

        donchian = DonchianChannels()
        donchian.Length = self._donchian_period.Value
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, cci, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, donchian_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        upper = donchian_value.UpperBand
        lower = donchian_value.LowerBand
        middle = donchian_value.Middle
        if upper is None or lower is None or middle is None:
            return

        upper = float(upper)
        lower = float(lower)
        middle = float(middle)
        cci_dec = float(cci_value)
        price = float(candle.ClosePrice)

        if self._cooldown > 0:
            self._cooldown -= 1

        if self._cooldown == 0 and price >= upper and cci_dec > 0 and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 50
        elif self._cooldown == 0 and price <= lower and cci_dec < 0 and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 50
        elif self.Position > 0 and price < middle:
            self.SellMarket()
            self._cooldown = 50
        elif self.Position < 0 and price > middle:
            self.BuyMarket()
            self._cooldown = 50

    def CreateClone(self):
        return donchian_cci_strategy()
