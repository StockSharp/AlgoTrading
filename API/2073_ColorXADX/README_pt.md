# ColorXADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no cruzamento das linhas +DI e -DI confirmado pela força do ADX.

O sistema monitoriza os indicadores de Movimento Direcional. Quando +DI cruza acima de -DI com o Índice Médio Direcional excedendo um limiar definido, entra numa posição comprada e sai de qualquer vendida existente. Por outro lado, um cruzamento baixista (-DI acima de +DI) com ADX forte abre uma posição vendida e fecha as compradas. Níveis de stop-loss e take-profit são aplicados para gerir o risco.

## Detalhes

- **Critérios de entrada**: Cruzamento +DI/-DI com ADX acima do limiar.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou níveis de stop.
- **Stops**: Sim, stop-loss e take-profit fixos.
- **Valores padrão**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 30m
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ADX, DMI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Swing (4h)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
