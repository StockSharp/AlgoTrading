# Estratégia de Histograma Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma migração StockSharp do consultor MQL original `Exp_Stochastic_Histogram`.
Usa o oscilador Stochastic para produzir sinais de negociação contrários em dois modos:

- **Levels** – aparece um sinal quando %K sai das áreas de sobrecompra ou sobrevenda definidas por `HighLevel` e `LowLevel`.
- **Cross** – aparece um sinal quando %K cruza a linha %D. A operação é aberta na direção oposta ao cruzamento.

Sempre que um novo sinal é recebido, a estratégia fecha a posição existente e abre uma nova na direção necessária.

## Parâmetros

- `KPeriod` – período principal de %K.
- `DPeriod` – período de suavização de %D.
- `Slowing` – suavização adicional de %K.
- `HighLevel` – limiar superior para o modo Levels.
- `LowLevel` – limiar inferior para o modo Levels.
- `Mode` – Levels ou Cross.
- `CandleType` – período de velas utilizado para os cálculos.

## Como funciona

Para cada vela concluída, o oscilador Stochastic é atualizado e avaliado. No modo **Levels**, uma operação comprada é aberta quando %K regressa abaixo do nível alto e uma operação vendida quando %K sobe acima do nível baixo. No modo **Cross**, uma operação comprada é aberta em cruzamentos descendentes de %K abaixo de %D, enquanto cruzamentos ascendentes desencadeiam operações vendidas. A estratégia tem no máximo uma posição aberta de cada vez.
