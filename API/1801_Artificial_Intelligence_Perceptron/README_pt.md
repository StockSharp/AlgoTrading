# Estratégia de Inteligência Artificial Perceptron
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa os valores do Accelerator Oscillator (AC) como entradas de um perceptron linear simples. Quatro leituras de AC espaçadas sete barras são ponderadas por coeficientes definidos pelo usuário. Uma saída positiva do perceptron abre uma posição comprada, e uma saída negativa abre uma posição vendida.

A estratégia sempre aplica um stop-loss. Se um sinal oposto aparecer depois que o lucro ultrapassar o dobro do stop-loss, a posição é revertida com volume aumentado. Caso contrário, o stop-loss é movido para o ponto de equilíbrio.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Saída do perceptron > 0.
  - **Vendido**: Saída do perceptron < 0.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinal oposto com lucro > 2 * StopLoss → reversão.
  - Sinal oposto com lucro menor → stop movido para a entrada.
  - Stop-loss atingido.
- **Stops**: Stop-loss fixo em pontos.
- **Filtros**: Nenhum.

## Parâmetros
- `StopLoss` – distância do stop-loss em pontos (padrão 850).
- `Shift` – deslocamento de barra para valores do indicador (padrão 1).
- `X1`, `X2`, `X3`, `X4` – pesos do perceptron.
- `CandleType` – período de velas.
