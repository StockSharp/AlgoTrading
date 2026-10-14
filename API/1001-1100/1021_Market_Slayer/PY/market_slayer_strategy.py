import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class market_slayer_strategy(Strategy):
    """
    Market Slayer strategy.
    The SSL channel on TrendCandleType is built from WMAs of highs and lows over ConfirmationTrendValue candles: a close above the
    high WMA makes the trend bullish, a close below the low WMA makes it bearish, otherwise the previous state is kept. On CandleType
    the short WMA crossing above the long WMA in a bullish trend goes long and crossing below in a bearish trend goes short. A position
    closes when the trend turns opposite. Optional take profit and stop loss are distances in price steps.
    """

    def __init__(self):
        super(market_slayer_strategy, self).__init__()
        self._short_length = self.Param("ShortLength", 10).SetGreaterThanZero().SetDisplay("Short Length", "Short WMA length", "Indicators")
        self._long_length = self.Param("LongLength", 20).SetGreaterThanZero().SetDisplay("Long Length", "Long WMA length", "Indicators")
        self._confirmation_trend_value = self.Param("ConfirmationTrendValue", 2).SetGreaterThanZero().SetDisplay("Trend Length", "SSL WMA length on the trend timeframe", "Trend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Signal candle type", "General")
        self._trend_candle_type = self.Param("TrendCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(240))).SetDisplay("Trend Candle Type", "Higher timeframe for the SSL trend", "Trend")
        self._take_profit_enabled = self.Param("TakeProfitEnabled", False).SetDisplay("Use Take Profit", "Enable take profit", "Risk")
        self._take_profit_value = self.Param("TakeProfitValue", 20.0).SetNotNegative().SetDisplay("Take Profit", "Take profit distance in price steps", "Risk")
        self._stop_loss_enabled = self.Param("StopLossEnabled", False).SetDisplay("Use Stop Loss", "Enable stop loss", "Risk")
        self._stop_loss_value = self.Param("StopLossValue", 50.0).SetNotNegative().SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk")
        self._trend_high = None
        self._trend_low = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def trend_candle_type(self):
        return self._trend_candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, self.trend_candle_type)]

    def _reset_state(self):
        self._trend_hlv = 0
        self._prev_short = None
        self._prev_long = None

    def OnReseted(self):
        super(market_slayer_strategy, self).OnReseted()
        self._trend_high = None
        self._trend_low = None
        self._reset_state()

    def OnStarted2(self, time):
        super(market_slayer_strategy, self).OnStarted2(time)

        self._reset_state()

        short_wma = WeightedMovingAverage()
        short_wma.Length = self._short_length.Value
        long_wma = WeightedMovingAverage()
        long_wma.Length = self._long_length.Value
        self._trend_high = WeightedMovingAverage()
        self._trend_high.Length = self._confirmation_trend_value.Value
        self._trend_low = WeightedMovingAverage()
        self._trend_low.Length = self._confirmation_trend_value.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(short_wma, long_wma, self._process_candle).Start()

        self.SubscribeCandles(self.trend_candle_type).Bind(self._process_trend_candle).Start()

        use_tp = self._take_profit_enabled.Value
        use_sl = self._stop_loss_enabled.Value
        if use_tp or use_sl:
            step = Decimal(1)
            if self.Security is not None and self.Security.PriceStep is not None:
                step = self.Security.PriceStep
            tp = Unit(Decimal(self._take_profit_value.Value) * step, UnitTypes.Absolute) if use_tp else Unit()
            sl = Unit(Decimal(self._stop_loss_value.Value) * step, UnitTypes.Absolute) if use_sl else Unit()
            self.StartProtection(tp, sl, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_wma)
            self.DrawIndicator(area, long_wma)
            self.DrawOwnTrades(area)

    def _process_trend_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high_value = process_value(self._trend_high, candle.HighPrice, candle.OpenTime, True)
        low_value = process_value(self._trend_low, candle.LowPrice, candle.OpenTime, True)

        if not self._trend_high.IsFormed or not self._trend_low.IsFormed:
            return

        high = high_value.GetValue[Decimal](None)
        low = low_value.GetValue[Decimal](None)

        if candle.ClosePrice > high:
            self._trend_hlv = 1
        elif candle.ClosePrice < low:
            self._trend_hlv = -1

    def _process_candle(self, candle, short_value, long_value):
        if candle.State != CandleStates.Finished:
            return

        ps = self._prev_short
        pl = self._prev_long
        self._prev_short = short_value
        self._prev_long = long_value

        if ps is None or pl is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        bullish = self._trend_hlv > 0
        bearish = self._trend_hlv < 0

        if self.Position > 0 and bearish:
            self.SellMarket(self.Position)
            return

        if self.Position < 0 and bullish:
            self.BuyMarket(-self.Position)
            return

        if ps <= pl and short_value > long_value and bullish and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif ps >= pl and short_value < long_value and bearish and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return market_slayer_strategy()
