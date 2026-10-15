# Estratégia RSI + MACD Somente Comprado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia entra comprada quando o RSI cruza acima da linha central com confirmação altista do MACD, ou quando o MACD cruza acima da sua linha de sinal enquanto o RSI permanece acima da linha central. As saídas ocorrem quando o RSI cai abaixo da linha central ou o MACD cruza abaixo do sinal com um histograma não positivo. Um filtro de tendência EMA opcional e o contexto de sobrevendido podem refinar as entradas.

## Detalhes

- **Critérios de entrada**: RSI cruza acima da linha central com MACD altista ou MACD cruza acima do sinal com RSI acima da linha central
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: RSI cruza abaixo da linha central ou MACD cruza abaixo do sinal com histograma ≤ 0
- **Stops**: Take profit e stop loss percentuais opcionais
- **Valores padrão**:
  - `RsiLength` = 14
  - `RsiOversold` = 30
  - `RsiMidline` = 50
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `OversoldWindowBars` = 10
  - `EmaLength` = 200
  - `TakeProfitPercent` = 11.5
  - `StopLossPercent` = 2.5
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: RSI, MACD, EMA
  - Stops: Sim (opcional)
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
