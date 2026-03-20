import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class kositbablo10_strategy(Strategy):
    def __init__(self):
        super(kositbablo10_strategy, self).__init__()
        self._rsi_buy_period = self.Param("RsiBuyPeriod", 5) \
            .SetDisplay("RSI Buy Period", "RSI period for buy signals", "Indicators")
        self._rsi_sell_period = self.Param("RsiSellPeriod", 20) \
            .SetDisplay("RSI Sell Period", "RSI period for sell signals", "Indicators")
        self._ema_long_period = self.Param("EmaLongPeriod", 20) \
            .SetDisplay("EMA Long", "Long EMA period", "Indicators")
        self._ema_short_period = self.Param("EmaShortPeriod", 5) \
            .SetDisplay("EMA Short", "Short EMA period", "Indicators")
        self._stop_loss_pct = self.Param("StopLossPct", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def rsi_buy_period(self):
        return self._rsi_buy_period.Value

    @property
    def rsi_sell_period(self):
        return self._rsi_sell_period.Value

    @property
    def ema_long_period(self):
        return self._ema_long_period.Value

    @property
    def ema_short_period(self):
        return self._ema_short_period.Value

    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(kositbablo10_strategy, self).OnStarted(time)
        rsi_buy = RelativeStrengthIndex()
        rsi_buy.Length = self.rsi_buy_period
        rsi_sell = RelativeStrengthIndex()
        rsi_sell.Length = self.rsi_sell_period
        ema_long = ExponentialMovingAverage()
        ema_long.Length = self.ema_long_period
        ema_short = ExponentialMovingAverage()
        ema_short.Length = self.ema_short_period
        self.StartProtection(
            takeProfit=Unit(2.0, UnitTypes.Percent),
            stopLoss=Unit(float(self.stop_loss_pct), UnitTypes.Percent))
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi_buy, rsi_sell, ema_long, ema_short, self.process_candle).Start()

    def process_candle(self, candle, rsi_buy, rsi_sell, ema_long, ema_short):
        if candle.State != CandleStates.Finished:
            return
        rsi_buy = float(rsi_buy)
        rsi_sell = float(rsi_sell)
        ema_long = float(ema_long)
        ema_short = float(ema_short)
        buy_cond = rsi_buy < 48 and ema_long > ema_short
        sell_cond = rsi_sell > 60 and ema_long > ema_short
        if buy_cond and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif sell_cond and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return kositbablo10_strategy()
