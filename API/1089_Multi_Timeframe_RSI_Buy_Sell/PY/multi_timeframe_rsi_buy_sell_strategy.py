import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class multi_timeframe_rsi_buy_sell_strategy(Strategy):
    def __init__(self):
        super(multi_timeframe_rsi_buy_sell_strategy, self).__init__()

        self._rsi1_enabled = self.Param("Rsi1Enabled", True) \
            .SetDisplay("RSI1 Enabled", "Use first RSI", "RSI1")
        self._rsi2_enabled = self.Param("Rsi2Enabled", True) \
            .SetDisplay("RSI2 Enabled", "Use second RSI", "RSI2")
        self._rsi3_enabled = self.Param("Rsi3Enabled", True) \
            .SetDisplay("RSI3 Enabled", "Use third RSI", "RSI3")

        self._rsi1_length = self.Param("Rsi1Length", 14) \
            .SetDisplay("RSI1 Length", "Period for first RSI", "RSI1")
        self._rsi2_length = self.Param("Rsi2Length", 14) \
            .SetDisplay("RSI2 Length", "Period for second RSI", "RSI2")
        self._rsi3_length = self.Param("Rsi3Length", 14) \
            .SetDisplay("RSI3 Length", "Period for third RSI", "RSI3")

        self._rsi1_candle_type = self.Param("Rsi1CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("RSI1 Timeframe", "Timeframe for first RSI", "RSI1")
        self._rsi2_candle_type = self.Param("Rsi2CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("RSI2 Timeframe", "Timeframe for second RSI", "RSI2")
        self._rsi3_candle_type = self.Param("Rsi3CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("RSI3 Timeframe", "Timeframe for third RSI", "RSI3")

        self._buy_threshold = self.Param("BuyThreshold", 30.0) \
            .SetDisplay("Buy Threshold", "RSI level to enter long", "Strategy")
        self._sell_threshold = self.Param("SellThreshold", 70.0) \
            .SetDisplay("Sell Threshold", "RSI level to enter short", "Strategy")
        self._cooldown_period = self.Param("CooldownPeriod", 5) \
            .SetDisplay("Cooldown", "Bars to wait between signals", "Strategy")

        self._rsi1 = None
        self._rsi2 = None
        self._rsi3 = None
        self._buy_cooldown = 0
        self._sell_cooldown = 0

    @property
    def buy_threshold(self):
        return self._buy_threshold.Value

    @property
    def sell_threshold(self):
        return self._sell_threshold.Value

    @property
    def cooldown_period(self):
        return self._cooldown_period.Value

    def OnReseted(self):
        super(multi_timeframe_rsi_buy_sell_strategy, self).OnReseted()
        self._rsi1 = None
        self._rsi2 = None
        self._rsi3 = None
        self._buy_cooldown = 0
        self._sell_cooldown = 0

    def OnStarted(self, time):
        super(multi_timeframe_rsi_buy_sell_strategy, self).OnStarted(time)
        cd = int(self.cooldown_period)
        self._buy_cooldown = cd
        self._sell_cooldown = cd

        if self._rsi1_enabled.Value:
            rsi1 = RelativeStrengthIndex()
            rsi1.Length = self._rsi1_length.Value
            sub1 = self.SubscribeCandles(self._rsi1_candle_type.Value)
            sub1.Bind(rsi1, self._process_rsi1).Start()

        if self._rsi2_enabled.Value:
            rsi2 = RelativeStrengthIndex()
            rsi2.Length = self._rsi2_length.Value
            sub2 = self.SubscribeCandles(self._rsi2_candle_type.Value)
            sub2.Bind(rsi2, self._process_rsi2).Start()

        if self._rsi3_enabled.Value:
            rsi3 = RelativeStrengthIndex()
            rsi3.Length = self._rsi3_length.Value
            sub3 = self.SubscribeCandles(self._rsi3_candle_type.Value)
            sub3.Bind(rsi3, self._process_rsi3).Start()

    def _process_rsi1(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        self._rsi1 = float(value)
        self._try_trade(candle)

    def _process_rsi2(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        self._rsi2 = float(value)

    def _process_rsi3(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        self._rsi3 = float(value)

    def _try_trade(self, candle):
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if (self._rsi1_enabled.Value and self._rsi1 is None) or \
           (self._rsi2_enabled.Value and self._rsi2 is None) or \
           (self._rsi3_enabled.Value and self._rsi3 is None):
            return

        cd = int(self.cooldown_period)
        if self._buy_cooldown < cd:
            self._buy_cooldown += 1
        if self._sell_cooldown < cd:
            self._sell_cooldown += 1

        bt = float(self.buy_threshold)
        st = float(self.sell_threshold)

        buy_ok = (not self._rsi1_enabled.Value or self._rsi1 < bt) and \
                 (not self._rsi2_enabled.Value or self._rsi2 < bt) and \
                 (not self._rsi3_enabled.Value or self._rsi3 < bt)

        sell_ok = (not self._rsi1_enabled.Value or self._rsi1 > st) and \
                  (not self._rsi2_enabled.Value or self._rsi2 > st) and \
                  (not self._rsi3_enabled.Value or self._rsi3 > st)

        if buy_ok and self._buy_cooldown >= cd:
            self.BuyMarket()
            self._sell_cooldown = cd
            self._buy_cooldown = 0
        elif sell_ok and self._sell_cooldown >= cd:
            self.SellMarket()
            self._sell_cooldown = 0
            self._buy_cooldown = cd

    def CreateClone(self):
        return multi_timeframe_rsi_buy_sell_strategy()
