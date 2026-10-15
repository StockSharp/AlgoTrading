# Estratégia de Retração EMA WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia seguidora de tendência que combina um filtro de tendência EMA com extremos do Williams %R. Aguarda uma retração no Williams %R antes de permitir outra operação e pode piramidizar até um número definido de posições.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Williams %R cai abaixo de -100 e então ocorre uma retração acima de `WPR Retracement`. Tendência de alta opcional confirmada pela EMA.
  - **Vendido**: Williams %R sobe acima de 0 e então retrai abaixo de `-WPR Retracement`. Tendência de baixa opcional confirmada pela EMA.
- **Comprado/Vendido**: Ambas as direções com piramidização.
- **Critérios de saída**:
  - Williams %R sai da zona extrema.
  - Saída opcional após `Max Unprofit Bars` sem lucro.
  - Stop loss, take profit e stop trailing opcional gerenciados pelo módulo de proteção.
- **Stops**: Stop loss e take profit fixos com stop trailing opcional.
- **Valores padrão**:
  - `Use EMA Trend` = true
  - `Bars In Trend` = 1
  - `EMA Trend` = 144
  - `WPR Period` = 46
  - `WPR Retracement` = 30
  - `Use WPR Exit` = true
  - `Order Volume` = 0.1
  - `Max Trades` = 2
  - `Stop Loss` = 50
  - `Take Profit` = 200
  - `Use Trailing` = false
  - `Trailing Stop` = 10
  - `Use Unprofit Exit` = false
  - `Max Unprofit Bars` = 5
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, Williams %R
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
