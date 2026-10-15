# Estratégia Syndicate Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma tradução para StockSharp do script original do MetaTrader **Syndicate_Trader_v_1_04.mq4** da pasta `MQL/12351`.

Opera com base em um cruzamento entre médias móveis exponenciais rápida e lenta com confirmação de pico de volume. Filtros de sessão opcionais restringem o trading a horas específicas. Níveis simples de take-profit e stop-loss gerenciam o risco.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA rápida cruza acima da EMA lenta e o volume supera a média móvel de volume multiplicada por um fator configurável.
  - **Vendido**: EMA rápida cruza abaixo da EMA lenta com a mesma confirmação de volume.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Cruzamento oposto.
  - Stop-loss ou take-profit atingido.
  - Fora da janela de sessão permitida.
- **Stops**: Stop-loss e take-profit fixos em pontos de preço.
- **Filtros**:
  - Filtro de pico de volume.
  - Filtro de tempo de sessão opcional.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `FastEmaLength` | Período da EMA rápida. |
| `SlowEmaLength` | Período da EMA lenta. |
| `VolumeMaLength` | Período para calcular a média do volume. |
| `VolumeMultiplier` | Multiplicador aplicado ao volume médio para definir um pico. |
| `TakeProfitPoints` | Take-profit em pontos de preço. |
| `StopLossPoints` | Stop-loss em pontos de preço. |
| `UseSessionFilter` | Ativar ou desativar o filtro de sessão. |
| `SessionStartHour/SessionStartMinute` | Hora de início da sessão de trading. |
| `SessionEndHour/SessionEndMinute` | Hora de fim da sessão de trading. |
| `CandleType` | Tipo de vela e período. |
