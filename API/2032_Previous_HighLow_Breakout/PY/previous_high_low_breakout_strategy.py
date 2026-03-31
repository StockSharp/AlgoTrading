import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class previous_high_low_breakout_strategy(Strategy):

    def __init__(self):
        super(previous_high_low_breakout_strategy, self).__init__()

        self._stop_loss = self.Param("StopLoss", 50.0) \
            .SetDisplay("Stop Loss", "Stop loss in price points", "Risk Management")
        self._take_profit = self.Param("TakeProfit", 1000.0) \
            .SetDisplay("Take Profit", "Take profit in price points", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")
        self._cooldown_candles = self.Param("CooldownCandles", 300) \
            .SetDisplay("Cooldown", "Cooldown between trades in candles", "General")

        self._previous_high = 0.0
        self._previous_low = 0.0
        self._is_first_candle = True
        self._cooldown = 0

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CooldownCandles(self):
        return self._cooldown_candles.Value

    @CooldownCandles.setter
    def CooldownCandles(self, value):
        self._cooldown_candles.Value = value

    def OnStarted2(self, time):
        super(previous_high_low_breakout_strategy, self).OnStarted2(time)

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.ProcessCandle) \
            .Start()

        self.StartProtection(
            stopLoss=Unit(self.StopLoss, UnitTypes.Absolute),
            takeProfit=Unit(self.TakeProfit, UnitTypes.Absolute)
        )

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._is_first_candle:
            self._previous_high = float(candle.HighPrice)
            self._previous_low = float(candle.LowPrice)
            self._is_first_candle = False
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._previous_high = float(candle.HighPrice)
            self._previous_low = float(candle.LowPrice)
            return

        price = float(candle.ClosePrice)

        if price > self._previous_high and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown = self.CooldownCandles
        elif price < self._previous_low and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown = self.CooldownCandles

        self._previous_high = float(candle.HighPrice)
        self._previous_low = float(candle.LowPrice)

    def OnReseted(self):
        super(previous_high_low_breakout_strategy, self).OnReseted()
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._is_first_candle = True
        self._cooldown = 0

    def CreateClone(self):
        return previous_high_low_breakout_strategy()
