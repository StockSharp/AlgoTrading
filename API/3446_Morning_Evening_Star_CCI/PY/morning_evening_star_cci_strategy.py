import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class morning_evening_star_cci_strategy(Strategy):
    """
    Morning/Evening Star + CCI: buy on morning star with negative CCI, sell on evening star with positive CCI.
    """

    def __init__(self):
        super(morning_evening_star_cci_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 14).SetDisplay("CCI Period", "CCI period", "Indicators")
        self._cci_level = self.Param("CciLevel", 0.0).SetDisplay("CCI Level", "CCI threshold", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_candle = None
        self._prev_prev_candle = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(morning_evening_star_cci_strategy, self).OnReseted()
        self._prev_candle = None
        self._prev_prev_candle = None

    def OnStarted(self, time):
        super(morning_evening_star_cci_strategy, self).OnStarted(time)
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, cci_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        cci = float(cci_val)
        cci_level = float(self._cci_level.Value)
        if self._prev_candle is not None and self._prev_prev_candle is not None:
            prev_body = abs(float(self._prev_candle.ClosePrice) - float(self._prev_candle.OpenPrice))
            prev_range = float(self._prev_candle.HighPrice) - float(self._prev_candle.LowPrice)
            is_small_body = prev_range > 0 and prev_body < prev_range * 0.3
            pp_open = float(self._prev_prev_candle.OpenPrice)
            pp_close = float(self._prev_prev_candle.ClosePrice)
            c_open = float(candle.OpenPrice)
            c_close = float(candle.ClosePrice)
            first_bearish = pp_open > pp_close
            curr_bullish = c_close > c_open
            is_morning = first_bearish and is_small_body and curr_bullish and c_close > pp_open * 0.5 + pp_close * 0.5
            first_bullish = pp_close > pp_open
            curr_bearish = c_open > c_close
            is_evening = first_bullish and is_small_body and curr_bearish and c_close < pp_open * 0.5 + pp_close * 0.5
            if is_morning and cci < -cci_level and self.Position <= 0:
                self.BuyMarket()
            elif is_evening and cci > cci_level and self.Position >= 0:
                self.SellMarket()
        self._prev_prev_candle = self._prev_candle
        self._prev_candle = candle

    def CreateClone(self):
        return morning_evening_star_cci_strategy()
