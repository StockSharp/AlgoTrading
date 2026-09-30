import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class footprint_strategy(Strategy):
    def __init__(self):
        super(footprint_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))
        self._imbalance_percent = self.Param("ImbalancePercent", 25.0).SetNotNegative()
        self._use_daily_trend_filter = self.Param("UseDailyTrendFilter", False)
        self._daily_trend_period = self.Param("DailyTrendPeriod", 50).SetGreaterThanZero()
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative()
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0).SetNotNegative()

        self._daily_trend_bullish = False
        self._daily_trend_ready = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        result = [(self.Security, self.CandleType)]
        if bool(self._use_daily_trend_filter.Value):
            result.append((self.Security, DataType.TimeFrame(TimeSpan.FromDays(1))))
        return result

    def OnReseted(self):
        super(footprint_strategy, self).OnReseted()
        self._daily_trend_bullish = False
        self._daily_trend_ready = False

    def OnStarted2(self, time):
        super(footprint_strategy, self).OnStarted2(time)

        sl = float(self._stop_loss_percent.Value)
        tp = float(self._take_profit_percent.Value)
        if sl > 0 or tp > 0:
            self.StartProtection(
                Unit(tp, UnitTypes.Percent) if tp > 0 else None,
                Unit(sl, UnitTypes.Percent) if sl > 0 else None)

        if bool(self._use_daily_trend_filter.Value):
            sma = SimpleMovingAverage()
            sma.Length = int(self._daily_trend_period.Value)

            def on_daily(candle, sma_value):
                if candle.State != CandleStates.Finished or not sma.IsFormed:
                    return
                self._daily_trend_bullish = float(candle.ClosePrice) > float(sma_value)
                self._daily_trend_ready = True

            self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromDays(1))).Bind(sma, on_daily).Start()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished or self.Position != 0:
            return

        if bool(self._use_daily_trend_filter.Value) and (not self._daily_trend_ready or not self._daily_trend_bullish):
            return

        buy = float(candle.BuyVolume) if candle.BuyVolume is not None else 0.0
        sell = float(candle.SellVolume) if candle.SellVolume is not None else 0.0

        if buy <= 0 and sell <= 0:
            return

        required_buy = sell * (1.0 + float(self._imbalance_percent.Value) / 100.0)

        if buy > required_buy and float(candle.ClosePrice) < float(candle.OpenPrice):
            self.BuyMarket()

    def CreateClone(self):
        return footprint_strategy()
