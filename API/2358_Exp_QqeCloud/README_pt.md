# Estratégia Exp QqeCloud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma abordagem de seguimento de tendência que aplica o indicador QQE (Quantitative Qualitative Estimation) a um RSI suavizado.
A estratégia abre posições apenas em um horário de início de sessão predefinido e as fecha quando o sinal oposto ocorre
ou a sessão de negociação termina.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Em `StartHour`:`StartMinute`, a tendência QQE gira para cima.
  - **Vendido**: Em `StartHour`:`StartMinute`, a tendência QQE gira para baixo.
- **Critérios de saída**:
  - Sinal de tendência QQE oposto.
  - O tempo ultrapassa `StopHour`:`StopMinute`.
- **Indicadores**:
  - RSI (período `RsiPeriod`, suavizado por `RsiSmoothing`).
  - Bandas QQE usando multiplicador `QqeFactor`.
- **Stops**: Nenhum por padrão.
- **Valores padrão**:
  - `CandleType` = velas de 1 minuto
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.236
  - `StartHour` = 0, `StartMinute` = 0
  - `StopHour` = 23, `StopMinute` = 59
- **Filtros**:
  - Janela de tempo para entradas e saídas
  - Seguimento de tendência, período único
