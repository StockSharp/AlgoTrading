# Estratégia de Scalping MartinGale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O cruzamento de SMA(3) com SMA(8) aciona entradas com piramidação no estilo martingale. Ordens adicionais são adicionadas a cada barra até que o stop ou take-profit seja atingido.

## Detalhes

- **Critérios de entrada**: `SMA3` acima de `SMA8` para comprados, abaixo para vendidos; novas entradas são adicionadas enquanto o sinal persistir.
- **Comprado/Vendido**: Configurável via `TradingMode`.
- **Critérios de saída**: Preço atinge `TakeProfit` ou `StopLoss` e relação SMA oposta.
- **Stops**: Sim, baseados no valor da SMA lenta.
- **Valores padrão**:
  - `FastLength` = 3
  - `SlowLength` = 8
  - `TakeProfit` = 1.03
  - `StopLoss` = 0.95
  - `TradingMode` = Long
  - `CandleType` = 5 minutes
  - `MaxPyramids` = 5
- **Filtros**:
  - Categoria: Tendência
  - Direção: Configurável
  - Indicadores: SMA
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
