import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class cci_vwap_strategy(Strategy):
    """
    CCI + VWAP strategy.
    Enters long when CCI < -100 and price < VWAP.
    Enters short when CCI > 100 and price > VWAP.
    """

    def __init__(self):
        super(cci_vwap_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI period", "CCI indicator period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 60) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle type", "Type of candles to use", "General")

        self._cooldown = 0
        self._vwap_date = None
        self._vwap_cum_pv = 0.0
        self._vwap_cum_vol = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cci_vwap_strategy, self).OnReseted()
        self._cooldown = 0
        self._vwap_date = None
        self._vwap_cum_pv = 0.0
        self._vwap_cum_vol = 0.0

    def OnStarted2(self, time):
        super(cci_vwap_strategy, self).OnStarted2(time)

        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value
        dummy_ema = ExponentialMovingAverage()
        dummy_ema.Length = 10

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, dummy_ema, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def on_process(self, candle, cci_val, dummy_val):
        if candle.State != CandleStates.Finished:
            return

        date = candle.ServerTime.Date
        if self._vwap_date != date:
            self._vwap_date = date
            self._vwap_cum_pv = 0.0
            self._vwap_cum_vol = 0.0

        self._vwap_cum_pv += float(candle.ClosePrice) * float(candle.TotalVolume)
        self._vwap_cum_vol += float(candle.TotalVolume)
        if self._vwap_cum_vol <= 0:
            return

        current_vwap = self._vwap_cum_pv / self._vwap_cum_vol
        if current_vwap == 0:
            return

        if self._cooldown > 0:
            self._cooldown -= 1

        close = float(candle.ClosePrice)

        if self._cooldown == 0 and cci_val < -100 and close < current_vwap and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self._cooldown == 0 and cci_val > 100 and close > current_vwap and self.Position >= 0:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self.Position > 0 and close > current_vwap:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self.Position < 0 and close < current_vwap:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value

    def CreateClone(self):
        return cci_vwap_strategy()
