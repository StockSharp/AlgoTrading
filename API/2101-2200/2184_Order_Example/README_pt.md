# Estratégia de Exemplo de Ordem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento convertida a partir do exemplo MQL5 `OrderExample.mq5`.
Ela entra em operações quando o preço rompe acima de máximas recentes ou abaixo de mínimas recentes.

A estratégia utiliza os indicadores `Highest` e `Lowest` para rastrear os níveis de rompimento em uma janela configurável.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close` rompe acima da máxima mais alta de `Lookback` velas
  - Vendido: `Close` rompe abaixo da mínima mais baixa de `Lookback` velas
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Rompimento oposto
- **Stops**: Não
- **Valores padrão**:
  - `Lookback` = 26
  - `CandleType` = `TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
