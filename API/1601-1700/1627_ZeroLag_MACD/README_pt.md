# Estratégia de Cruzamento ZeroLag MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base em um cruzamento entre a linha MACD e sua linha de sinal. Foi convertida do assessor especialista MetaTrader **ZeroLagEA-AIP v0.0.4**. A estratégia opera apenas durante as horas de sessão configuradas e pode opcionalmente exigir que o cruzamento aconteça na barra atual.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A linha MACD cruza acima da linha de sinal.
  - **Vendido**: A linha MACD cruza abaixo da linha de sinal.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Cruzamento oposto ou saída forçada no dia e hora especificados.
- **Stops**: Nenhum.
- **Filtros**:
  - Horas de sessão definidas por `StartHour` e `EndHour`.
  - Requisito opcional de cruzamento recente (`UseFreshSignal`).

## Parâmetros

- `FastEmaLength` = 2
- `SlowEmaLength` = 34
- `SignalEmaLength` = 2
- `UseFreshSignal` = true
- `Volume` = 2
- `StartHour` = 9
- `EndHour` = 15
- `KillDay` = 5
- `KillHour` = 21
- `CandleType` = velas de 1 minuto
