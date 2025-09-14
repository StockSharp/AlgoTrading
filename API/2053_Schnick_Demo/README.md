# Schnick Demo

This sample demonstrates how to train and test a simple classifier within a StockSharp strategy. The goal is to identify a fictitious creature called **Schnick** based on seven numeric attributes:

1. Height
2. Weight
3. Number of legs
4. Number of eyes
5. Length of arms
6. Average speed
7. Frequency of call

The strategy follows these steps:

1. **Generate training data** – create random samples and label them using strict rules that define what a Schnick is.
2. **Train classifier** – a simple nearest-neighbor model learns from the generated data.
3. **Insert errors** – random noise is added to demonstrate how the classifier handles imperfect data.
4. **Evaluate accuracy** – the trained models are tested on new samples and their accuracy is logged.

The example is educational and does not perform any market operations.
