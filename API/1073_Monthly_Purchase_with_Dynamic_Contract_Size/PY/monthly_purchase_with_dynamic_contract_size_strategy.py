import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class monthly_purchase_with_dynamic_contract_size_strategy(Strategy):
    def __init__(self):
        super(monthly_purchase_with_dynamic_contract_size_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "General")
        self._percent_of_equity = self.Param("PercentOfEquity", 0.03) \
            .SetDisplay("Percent of Equity", "Percentage of equity per purchase", "Strategy")
        self._buy_day = self.Param("BuyDay", 1) \
            .SetDisplay("Buy Day", "Day of month to buy", "Strategy")
        self._last_buy_month = -1
        self._last_buy_year = -1

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(monthly_purchase_with_dynamic_contract_size_strategy, self).OnReseted()
        self._last_buy_month = -1
        self._last_buy_year = -1

    def OnStarted(self, time):
        super(monthly_purchase_with_dynamic_contract_size_strategy, self).OnStarted(time)
        self._last_buy_month = -1
        self._last_buy_year = -1
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        if close <= 0.0:
            return
        current_day = candle.OpenTime.Day
        current_month = candle.OpenTime.Month
        current_year = candle.OpenTime.Year
        buy_day = self._buy_day.Value
        if current_day >= buy_day and (self._last_buy_month != current_month or self._last_buy_year != current_year):
            pct = float(self._percent_of_equity.Value)
            effective_equity = close
            contracts = int(effective_equity * pct / close)
            if contracts <= 0:
                contracts = 1
            self.BuyMarket()
            self._last_buy_month = current_month
            self._last_buy_year = current_year

    def CreateClone(self):
        return monthly_purchase_with_dynamic_contract_size_strategy()
