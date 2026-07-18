# Estratégia de Previsão EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador EMA Prediction que gera sinais quando as médias móveis exponenciais rápida e lenta se cruzam em uma vela que confirma a direção.

A estratégia abre posições compradas quando a EMA rápida cruza acima da EMA lenta durante uma vela de alta e fecha qualquer posição vendida. Abre posições vendidas quando a EMA rápida cruza abaixo da EMA lenta durante uma vela de baixa e fecha qualquer posição comprada.

## Detalhes

- **Critérios de entrada**:
  - Comprado: EMA rápida cruza acima da EMA lenta e a vela é de alta.
  - Vendido: EMA rápida cruza abaixo da EMA lenta e a vela é de baixa.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Take profit e stop loss fixos
- **Valores padrão**:
  - `CandleType` = velas de 6 horas
  - `FastPeriod` = 1
  - `SlowPeriod` = 2
  - `StopLossTicks` = 1000
  - `TakeProfitTicks` = 2000
- **Filtros**:
  - Categoria: Cruzamento de médias móveis
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Take profit e stop loss
  - Complexidade: Básico
  - Período: 6 horas
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
