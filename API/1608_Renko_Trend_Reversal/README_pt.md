# Estratégia de Reversão de Tendência Renko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Reversão de Tendência Renko opera quando a abertura Renko cruza o fechamento Renko. Stop-loss e take-profit podem ser ativados. Usa blocos Renko baseados em ATR.

## Detalhes

- **Critérios de entrada**: cruzamento de abertura/fechamento Renko com janela de tempo
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop loss ou take profit opcionais
- **Stops**: Opcional
- **Valores padrão**:
  - `RenkoAtrLength` = 10
  - `StopLossPct` = 10
  - `TakeProfitPct` = 50
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR
  - Stops: Opcional
  - Complexidade: Básico
  - Período: Renko
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
