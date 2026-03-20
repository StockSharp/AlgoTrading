import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class rnn_probability_strategy(Strategy):
    def __init__(self):
        super(rnn_probability_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1.0) \
            .SetDisplay("Trade Volume", "Lot size used for each market entry", "General")
        self._rsi_period = self.Param("RsiPeriod", 9) \
            .SetDisplay("RSI Period", "Length of the RSI indicator feeding the neural network", "Indicator")
        self._stop_loss_take_profit_pips = self.Param("StopLossTakeProfitPips", 100.0) \
            .SetDisplay("Stop Loss and Take Profit pips", "Distance used for both SL and TP levels", "Risk")
        self._weight0 = self.Param("Weight0", 6.0) \
            .SetDisplay("Weight 0", "Probability weight when all RSI inputs are low", "Model")
        self._weight1 = self.Param("Weight1", 96.0) \
            .SetDisplay("Weight 1", "Probability weight for low low high branch", "Model")
        self._weight2 = self.Param("Weight2", 90.0) \
            .SetDisplay("Weight 2", "Probability weight for low high low branch", "Model")
        self._weight3 = self.Param("Weight3", 35.0) \
            .SetDisplay("Weight 3", "Probability weight for low high high branch", "Model")
        self._weight4 = self.Param("Weight4", 64.0) \
            .SetDisplay("Weight 4", "Probability weight for high low low branch", "Model")
        self._weight5 = self.Param("Weight5", 83.0) \
            .SetDisplay("Weight 5", "Probability weight for high low high branch", "Model")
        self._weight6 = self.Param("Weight6", 66.0) \
            .SetDisplay("Weight 6", "Probability weight for high high low branch", "Model")
        self._weight7 = self.Param("Weight7", 50.0) \
            .SetDisplay("Weight 7", "Probability weight for high high high branch", "Model")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Primary timeframe used for signal generation", "General")

        self._rsi = None
        self._rsi_history = []
        self._pip_size = 0.0

    @property
    def trade_volume(self):
        return self._trade_volume.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def stop_loss_take_profit_pips(self):
        return self._stop_loss_take_profit_pips.Value

    @property
    def weight0(self):
        return self._weight0.Value

    @property
    def weight1(self):
        return self._weight1.Value

    @property
    def weight2(self):
        return self._weight2.Value

    @property
    def weight3(self):
        return self._weight3.Value

    @property
    def weight4(self):
        return self._weight4.Value

    @property
    def weight5(self):
        return self._weight5.Value

    @property
    def weight6(self):
        return self._weight6.Value

    @property
    def weight7(self):
        return self._weight7.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rnn_probability_strategy, self).OnReseted()
        self._rsi = None
        self._rsi_history = []
        self._pip_size = 0.0

    def OnStarted(self, time):
        super(rnn_probability_strategy, self).OnStarted(time)

        self.Volume = self.trade_volume
        self._pip_size = self._calculate_pip_size()

        sl_tp_pips = float(self.stop_loss_take_profit_pips)
        if sl_tp_pips > 0 and self._pip_size > 0:
            distance = Unit(sl_tp_pips * self._pip_size, UnitTypes.Absolute)
            self.StartProtection(
                takeProfit=distance,
                stopLoss=distance,
                isStopTrailing=False,
                useMarketOrders=True)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._rsi is None:
            return

        rsi_period = self.rsi_period
        if rsi_period <= 0:
            return

        price = float(candle.OpenPrice)
        rsi_ind_value = self._rsi.Process(
            DecimalIndicatorValue(self._rsi, price, candle.OpenTime))

        if not self._rsi.IsFormed or rsi_ind_value.IsEmpty:
            return

        rsi_value = float(rsi_ind_value)

        self._rsi_history.append(rsi_value)
        max_size = max(2 * rsi_period + 5, rsi_period + 1)
        if len(self._rsi_history) > max_size:
            self._rsi_history = self._rsi_history[-max_size:]

        if not self.IsOnline:
            return

        last_index = len(self._rsi_history) - 1
        delayed_index = last_index - rsi_period
        delayed_twice_index = last_index - 2 * rsi_period

        if delayed_index < 0 or delayed_twice_index < 0:
            return

        p1 = self._rsi_history[last_index] / 100.0
        p2 = self._rsi_history[delayed_index] / 100.0
        p3 = self._rsi_history[delayed_twice_index] / 100.0

        probability = self._calculate_probability(p1, p2, p3)
        signal = probability * 2.0 - 1.0

        tv = float(self.trade_volume)
        if tv <= 0:
            return

        if self.Position != 0:
            return

        if signal < 0:
            self.BuyMarket(tv)
        else:
            self.SellMarket(tv)

    def _calculate_probability(self, p1, p2, p3):
        pn1 = 1.0 - p1
        pn2 = 1.0 - p2
        pn3 = 1.0 - p3

        w0 = float(self.weight0)
        w1 = float(self.weight1)
        w2 = float(self.weight2)
        w3 = float(self.weight3)
        w4 = float(self.weight4)
        w5 = float(self.weight5)
        w6 = float(self.weight6)
        w7 = float(self.weight7)

        probability = (
            pn1 * (pn2 * (pn3 * w0 + p3 * w1) +
                   p2 * (pn3 * w2 + p3 * w3)) +
            p1 * (pn2 * (pn3 * w4 + p3 * w5) +
                  p2 * (pn3 * w6 + p3 * w7))
        )

        return probability / 100.0

    def _calculate_pip_size(self):
        sec = self.Security
        if sec is None:
            return 0.0
        step = sec.PriceStep
        if step is None or float(step) <= 0:
            return 0.0
        step_val = float(step)
        decimals = self._get_decimal_places(step_val)
        if decimals == 3 or decimals == 5:
            return step_val * 10.0
        return step_val

    def _get_decimal_places(self, value):
        value = abs(value)
        decimals = 0
        while value != int(value) and decimals < 10:
            value *= 10.0
            decimals += 1
        return decimals

    def CreateClone(self):
        return rnn_probability_strategy()
