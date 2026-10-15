# Estratégia de Futuros com Padrão de Vela Engolfo por Tamanho
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera uma vez por dia quando o intervalo de uma vela excede um limiar de ticks dentro de uma janela de tempo selecionada. A direção segue o corpo da vela e a saída é feita via take profit e stop loss.

## Detalhes

- **Critérios de entrada**: Intervalo da vela em ticks dentro da sessão de trading.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Take profit ou stop loss.
- **Stops**: Take Profit e Stop Loss.
- **Valores padrão**:
  - `CandleType` = 1 minute
  - `CandleSizeThresholdTicks` = 25
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 40
  - `StartHour` = 7
  - `StartMinute` = 0
  - `EndHour` = 9
  - `EndMinute` = 15
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
