# Estratégia de Reversão de Tendência Renko V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Reversão de Tendência Renko V2 opera quando a abertura Renko cruza o fechamento Renko. Utiliza blocos Renko baseados em ATR e aplica níveis de stop-loss e take-profit. As posições vendidas podem ser desativadas.

## Detalhes

- **Critérios de entrada**: cruzamento de abertura/fechamento Renko com janela de tempo
- **Comprado/Vendido**: Ambos (vendido opcional)
- **Critérios de saída**: stop loss ou take profit
- **Stops**: Sim
- **Valores padrão**:
  - `RenkoAtrLength` = 10
  - `StopLossPct` = 3
  - `TakeProfitPct` = 20
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Renko
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
