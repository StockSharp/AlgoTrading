import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Math, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class matrix_machine_learning_strategy(Strategy):
    """
    Hopfield neural network trained on binary price direction sequences.
    """

    def __init__(self):
        super(matrix_machine_learning_strategy, self).__init__()
        self._max_iterations = self.Param("MaxIterations", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Iterations", "Maximum number of Hopfield iterations executed per forecast.", "Machine Learning")
        self._accuracy = self.Param("Accuracy", 0.00001).SetDisplay("Accuracy", "Desired accuracy when checking convergence of neuron states.", "Machine Learning")
        self._history_depth = self.Param("HistoryDepth", 120) \
            .SetGreaterThanZero() \
            .SetDisplay("History Depth", "Total amount of closes stored for the Hopfield network.", "Machine Learning") \
            .SetOptimize(80, 200, 10)
        self._forward_depth = self.Param("ForwardDepth", 60) \
            .SetGreaterThanZero() \
            .SetDisplay("Forward Depth", "Amount of closes kept for out-of-sample validation.", "Machine Learning") \
            .SetOptimize(30, 120, 10)
        self._predictor_length = self.Param("PredictorLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Predictor Length", "Length of binary vector passed to the network input.", "Machine Learning") \
            .SetOptimize(10, 40, 2)
        self._forecast_length = self.Param("ForecastLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Forecast Length", "Length of the binary output vector produced by the network.", "Machine Learning") \
            .SetOptimize(5, 20, 1)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))).SetDisplay("Candle Type", "Type of candles requested from the market data source.", "Data")
        self._enable_debug_log = self.Param("EnableDebugLog", False).SetDisplay("Debug Log", "Write detailed neural network diagnostics to the log.", "Machine Learning")

        self._closes = []
        self._weights = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(matrix_machine_learning_strategy, self).OnReseted()
        self._closes = []
        self._weights = None

    def OnStarted2(self, time):
        super(matrix_machine_learning_strategy, self).OnStarted2(time)
        self.StartProtection(None, None)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._closes.append(candle.ClosePrice)
        hd = self._history_depth.Value
        if len(self._closes) > hd:
            self._closes.pop(0)

        pl = self._predictor_length.Value
        fl = self._forecast_length.Value
        if len(self._closes) < pl + fl + 1:
            return

        if len(self._closes) < self._forward_depth.Value + 2:
            return

        closes = list(self._closes)
        self._train_network(closes)

        forecast = self._forecast(closes)
        if forecast is None or len(forecast) == 0:
            return

        direction = forecast[0]
        if direction > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif direction < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def _train_network(self, closes):
        history_count = len(closes)
        forward_count = min(self._forward_depth.Value, history_count - 1)
        training_count = history_count - forward_count
        predictor = self._predictor_length.Value
        response = self._forecast_length.Value

        if training_count <= predictor + response:
            return

        training_data = self._build_binary_diff(closes, 0, training_count)
        if len(training_data) < predictor + response:
            return

        weights = self._train_weights(training_data, predictor, response)
        if weights is None:
            return

        self._weights = weights
        self._evaluate_weights(training_data, "Backtest evaluation")

        forward_data = self._build_binary_diff(closes, training_count - 1, forward_count + 1)
        if len(forward_data) >= predictor + response:
            self._evaluate_weights(forward_data, "Forward evaluation")

    def _forecast(self, closes):
        if self._weights is None:
            return None

        pattern = self._build_current_pattern(closes)
        if pattern is None:
            return None

        forecast = self._run_weights(self._weights, pattern)
        if self._enable_debug_log.Value:
            self.LogInfo("Online pattern: {0}".format(self._format_vector(pattern)))
            self.LogInfo("Forecast: {0}".format(self._format_vector(forecast)))

        return forecast

    @staticmethod
    def _build_binary_diff(closes, start_index, length):
        if length <= 1 or start_index < 0:
            return []

        if start_index + length > len(closes):
            length = len(closes) - start_index

        result_length = length - 1
        if result_length <= 0:
            return []

        result = []
        for i in range(result_length):
            first = closes[start_index + i]
            second = closes[start_index + i + 1]
            result.append(1.0 if second - first >= 0 else -1.0)

        return result

    def _build_current_pattern(self, closes):
        predictor = self._predictor_length.Value
        required = predictor + 1
        if len(closes) < required:
            return None

        start_index = len(closes) - required
        pattern = []
        for i in range(predictor):
            first = closes[start_index + i]
            second = closes[start_index + i + 1]
            pattern.append(1.0 if second - first >= 0 else -1.0)

        return pattern

    @staticmethod
    def _train_weights(data, predictor, response):
        sample = predictor + response
        if len(data) < sample:
            return None

        count = len(data) - sample + 1
        weights = [[0.0 for _ in range(response)] for _ in range(predictor)]

        for index in range(count):
            for row in range(predictor):
                input_value = data[index + row]
                for column in range(response):
                    output_value = data[index + predictor + column]
                    weights[row][column] += input_value * output_value

        return weights

    def _evaluate_weights(self, data, title):
        if self._weights is None:
            return

        predictor = len(self._weights)
        response = len(self._weights[0]) if predictor > 0 else 0
        sample = predictor + response
        if response == 0 or len(data) < sample:
            return

        count = len(data) - sample + 1
        if count <= 0:
            return

        positive = 0
        negative = 0
        total = 0.0

        for index in range(count):
            input_values = data[index:index + predictor]
            target = data[index + predictor:index + predictor + response]
            forecast = self._run_weights(self._weights, input_values)
            match = sum(forecast[i] * target[i] for i in range(response))

            if match > 0:
                positive += 1
            elif match < 0:
                negative += 1

            total += match
            if self._enable_debug_log.Value:
                self.LogInfo("Sample {0}: forecast={1} target={2} match={3:.3f}".format(index, self._format_vector(forecast), self._format_vector(target), match))

        average = total / count
        accuracy = (average + response) / (2.0 * response) * 100.0
        self.LogInfo("{0}: count={1} positive={2} negative={3} accuracy={4:.2f}%".format(title, count, positive, negative, accuracy))

    def _run_weights(self, weights, input_values):
        predictor = len(weights)
        response = len(weights[0]) if predictor > 0 else 0
        forecast = [0.0 for _ in range(response)]
        if len(input_values) != predictor:
            return forecast

        a = list(input_values)
        b = [0.0 for _ in range(response)]

        for _ in range(self._max_iterations.Value):
            previous_a = list(a)
            previous_b = list(b)

            for column in range(response):
                value = 0.0
                for row in range(predictor):
                    value += a[row] * weights[row][column]
                b[column] = Math.Tanh(value)

            for row in range(predictor):
                value = 0.0
                for column in range(response):
                    value += b[column] * weights[row][column]
                a[row] = Math.Tanh(value)

            diff_a = max([abs(a[i] - previous_a[i]) for i in range(predictor)] or [0.0])
            diff_b = max([abs(b[i] - previous_b[i]) for i in range(response)] or [0.0])
            if diff_a < self._accuracy.Value and diff_b < self._accuracy.Value:
                break

        for i in range(response):
            forecast[i] = 1.0 if b[i] >= 0 else -1.0

        return forecast

    @staticmethod
    def _format_vector(values):
        return "[" + ",".join("{0:.3f}".format(value) for value in values) + "]"

    def CreateClone(self):
        return matrix_machine_learning_strategy()
