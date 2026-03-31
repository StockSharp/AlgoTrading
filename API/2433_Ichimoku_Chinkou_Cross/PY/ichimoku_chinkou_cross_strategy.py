import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ichimoku_chinkou_cross_strategy(Strategy):
    def __init__(self):
        super(ichimoku_chinkou_cross_strategy, self).__init__()

        self._tenkan_period = self.Param("TenkanPeriod", 9)
        self._kijun_period = self.Param("KijunPeriod", 26)
        self._senkou_span_period = self.Param("SenkouSpanPeriod", 52)
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._rsi_buy_level = self.Param("RsiBuyLevel", 55.0)
        self._rsi_sell_level = self.Param("RsiSellLevel", 45.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._closes = []
        self._highs = []
        self._lows = []

    @property
    def TenkanPeriod(self):
        return self._tenkan_period.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkan_period.Value = value

    @property
    def KijunPeriod(self):
        return self._kijun_period.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijun_period.Value = value

    @property
    def SenkouSpanPeriod(self):
        return self._senkou_span_period.Value

    @SenkouSpanPeriod.setter
    def SenkouSpanPeriod(self, value):
        self._senkou_span_period.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiBuyLevel(self):
        return self._rsi_buy_level.Value

    @RsiBuyLevel.setter
    def RsiBuyLevel(self, value):
        self._rsi_buy_level.Value = value

    @property
    def RsiSellLevel(self):
        return self._rsi_sell_level.Value

    @RsiSellLevel.setter
    def RsiSellLevel(self, value):
        self._rsi_sell_level.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _get_midpoint(self, period):
        start = len(self._highs) - period
        highest = self._highs[start]
        lowest = self._lows[start]
        for i in range(start + 1, len(self._highs)):
            if self._highs[i] > highest:
                highest = self._highs[i]
            if self._lows[i] < lowest:
                lowest = self._lows[i]
        return (highest + lowest) / 2.0

    def OnStarted2(self, time):
        super(ichimoku_chinkou_cross_strategy, self).OnStarted2(time)

        self._closes = []
        self._highs = []
        self._lows = []

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        self._closes.append(float(candle.ClosePrice))
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))

        kijun_p = int(self.KijunPeriod)
        senkou_p = int(self.SenkouSpanPeriod)
        tenkan_p = int(self.TenkanPeriod)
        max_count = max(senkou_p, kijun_p) + kijun_p + 2

        if len(self._closes) > max_count:
            self._closes.pop(0)
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._closes) <= kijun_p or len(self._closes) < max(tenkan_p, kijun_p):
            return

        tenkan = self._get_midpoint(tenkan_p)
        kijun = self._get_midpoint(kijun_p)

        last_idx = len(self._closes) - 1
        prev_idx = last_idx - 1
        lag_idx = last_idx - kijun_p
        prev_lag_idx = prev_idx - kijun_p

        if prev_lag_idx < 0:
            return

        close = self._closes[last_idx]
        prev_close = self._closes[prev_idx]
        lag_close = self._closes[lag_idx]
        prev_lag_close = self._closes[prev_lag_idx]

        chinkou_cross_up = close > lag_close and prev_close <= prev_lag_close
        chinkou_cross_down = close < lag_close and prev_close >= prev_lag_close

        rsi_val = float(rsi_value)

        if chinkou_cross_up and tenkan > kijun and rsi_val >= float(self.RsiBuyLevel) and self.Position <= 0:
            self.BuyMarket()
        elif chinkou_cross_down and tenkan < kijun and rsi_val <= float(self.RsiSellLevel) and self.Position >= 0:
            self.SellMarket()

    def OnReseted(self):
        super(ichimoku_chinkou_cross_strategy, self).OnReseted()
        self._closes = []
        self._highs = []
        self._lows = []

    def CreateClone(self):
        return ichimoku_chinkou_cross_strategy()
