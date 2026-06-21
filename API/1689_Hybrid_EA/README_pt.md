# Estratégia Hybrid EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Hybrid EA utiliza o Índice de Vigor Relativo (RVI) e sua linha de sinal.
Abre uma posição comprada quando o RVI sobe acima do sinal por uma diferença especificada e abre uma posição vendida quando cai abaixo pelo mesmo valor. As posições são protegidas por níveis fixos de take profit e stop loss medidos em pontos de preço.

## Detalhes

- **Critérios de entrada**: RVI menos sinal ultrapassa o limiar
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: cruzamento oposto do limiar ou take profit/stop loss
- **Stops**: Sim, distância fixa em pontos
- **Valores padrão**:
  - `Volume` = 1
  - `RviLength` = 10
  - `SignalLength` = 4
  - `DifferenceThreshold` = 0.05
  - `TakeProfit` = 18
  - `StopLoss` = 9
  - `CandleType` = 5 minute candles
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RVI, SMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
