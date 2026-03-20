import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_ma_strategy(Strategy):
    def __init__(self):
        super(rsi_ma_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14)
        self._oversold_activation_level = self.Param("OversoldActivationLevel", 40)
        self._oversold_extreme_level = self.Param("OversoldExtremeLevel", 30)
        self._overbought_activation_level = self.Param("OverboughtActivationLevel", 60)
        self._overbought_extreme_level = self.Param("OverboughtExtremeLevel", 70)
        self._stop_loss_pips = self.Param("StopLossPips", 399)
        self._take_profit_pips = self.Param("TakeProfitPips", 999)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 299)
        self._use_stop_loss = self.Param("UseStopLoss", True)
        self._use_take_profit = self.Param("UseTakeProfit", True)
        self._use_trailing_stop = self.Param("UseTrailingStop", True)
        self._use_money_management = self.Param("UseMoneyManagement", False)
        self._risk_percent = self.Param("RiskPercent", 10)
        self._trade_volume = self.Param("TradeVolume", 0.1)
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2)

        self._rsi = None
        self._ema = None
        self._previous_indicator_value = None
        self._stop_loss_price = None
        self._take_profit_price = None
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_ma_strategy, self).OnReseted()
        self._rsi = None
        self._ema = None
        self._previous_indicator_value = None
        self._stop_loss_price = None
        self._take_profit_price = None
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(rsi_ma_strategy, self).OnStarted(time)
        self.StartProtection(None, None)

        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period
        self.__ema = ExponentialMovingAverage()
        self.__ema.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return rsi_ma_strategy()
