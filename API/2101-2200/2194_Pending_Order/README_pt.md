# Estratégia de Ordem Pendente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que coloca quatro ordens pendentes ao redor do bid e ask atuais durante horas especificadas. Mantém continuamente ordens buy limit, sell limit, buy stop e sell stop a uma distância configurável do preço de mercado. Cada ordem pendente utiliza offsets fixos de stop-loss e take-profit.

## Detalhes

- **Critérios de entrada**: Colocar ordens pendentes a `Distance` ticks do bid/ask atual dentro das horas permitidas.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Take-profit ou stop-loss relativo ao preço de entrada.
- **Stops**: Sim.
- **Valores padrão**:
  - `StartHour` = 6
  - `EndHour` = 20
  - `TakeProfit` = 20
  - `StopLoss` = 100
  - `Distance` = 15
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Range
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
