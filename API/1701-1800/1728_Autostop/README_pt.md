# Estratégia Autostop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia utilitária que define automaticamente take profit e stop loss para posições abertas.
Não gera sinais de negociação. Quaisquer posições abertas externamente são protegidas usando distâncias fixas.

## Detalhes

- **Critérios de entrada**: Nenhum, as ordens são gerenciadas fora da estratégia.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Apenas ordens de proteção.
- **Stops**: Usa StartProtection para colocar take profit e stop loss fixos.
- **Valores padrão**:
  - `MonitorTakeProfit` = true
  - `MonitorStopLoss` = true
  - `TakeProfitTicks` = 30
  - `StopLossTicks` = 30
- **Filtros**:
  - Categoria: Gerenciamento de risco
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Fixo
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
