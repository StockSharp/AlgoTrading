import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class extreme_strength_reversal_strategy(Strategy):
    def __init__(self):
        super(extreme_strength_reversal_strategy, self).__init__()

        self._risk_percent = self.Param("RiskPercent", 1) \
            .SetDisplay("Risk Percent", "Risk per trade as percentage of equity.", "Risk Management")
        self._stop_loss_pips = self.Param("StopLossPips", 150) \
            .SetDisplay("Risk Percent", "Risk per trade as percentage of equity.", "Risk Management")
        self._take_profit_pips = self.Param("TakeProfitPips", 300) \
            .SetDisplay("Risk Percent", "Risk per trade as percentage of equity.", "Risk Management")
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Risk Percent", "Risk per trade as percentage of equity.", "Risk Management")
        self._bollinger_deviation = self.Param("BollingerDeviation", 1.5) \
            .SetDisplay("Risk Percent", "Risk per trade as percentage of equity.", "Risk Management")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Risk Percent", "Risk per trade as percentage of equity.", "Risk Management")
        self._rsi_overbought = self.Param("RsiOverbought", 65) \
            .SetDisplay("Risk Percent", "Risk per trade as percentage of equity.", "Risk Management")
        self._rsi_oversold = self.Param("RsiOversold", 35) \
            .SetDisplay("Risk Percent", "Risk per trade as percentage of equity.", "Risk Management")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Risk Percent", "Risk per trade as percentage of equity.", "Risk Management")

        self._bollinger = None
        self._rsi = None
        self._stop_loss_price = None
        self._take_profit_price = None
        self._entry_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(extreme_strength_reversal_strategy, self).OnReseted()
        self._bollinger = None
        self._rsi = None
        self._stop_loss_price = None
        self._take_profit_price = None
        self._entry_price = None

    def OnStarted(self, time):
        super(extreme_strength_reversal_strategy, self).OnStarted(time)

        self.__bollinger = BollingerBands()
        self.__bollinger.Length = self.bollinger_period
        self.__bollinger.Width = self.bollinger_deviation
        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self.__bollinger, self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return extreme_strength_reversal_strategy()
