# Estratégia de Padrão de 3 Barras MFS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia detecta uma sequência de reversão altista de três barras dentro de uma tendência de baixa. Procura uma grande barra verde de "ignição", um pequeno recuo vermelho e uma barra de confirmação altista que fecha acima da máxima do recuo. O filtro de tendência exige SMA longa > SMA média > SMA curta e o fechamento de ignição abaixo da SMA curta.

Quando o padrão aparece, a estratégia abre uma posição comprada, colocando o stop-loss na mínima da barra de ignição e um take-profit em um múltiplo de risco-recompensa configurável.

## Detalhes

- **Critérios de entrada**: Barra de ignição, recuo e confirmação em uma tendência de baixa.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Stop-loss na mínima de ignição ou take-profit no múltiplo de risco-recompensa.
- **Stops**: Sim, ordens de stop e alvo.
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `SmaShortLength` = 20
  - `SmaMedLength` = 50
  - `SmaLongLength` = 200
  - `IgniteMultiplier` = 3
  - `MaxPullbackSize` = 0.33
  - `MinConfirmationSize` = 0.33
  - `RiskReward` = 2
- **Filtros**:
  - Categoria: Padrão
  - Direção: Comprado
  - Indicadores: Candlestick, Moving Average
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
