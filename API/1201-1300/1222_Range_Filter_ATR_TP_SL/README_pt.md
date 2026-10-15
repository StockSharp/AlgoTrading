# Estratégia de Filtro de Intervalo com ATR TP/SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que entra quando o preço cruza as bandas do filtro de intervalo e sai usando níveis de take-profit e stop-loss baseados em ATR.

## Detalhes

- **Critérios de entrada**: O preço cruza acima da banda superior para comprado, abaixo da banda inferior para vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Take profit ou stop loss baseado em ATR.
- **Stops**: Baseado em ATR, fixo quando a operação é aberta.
- **Valores padrão**:
  - `RangeFilterLength` = 20
  - `RangeFilterMultiplier` = 1.5
  - `AtrLength` = 14
  - `TakeProfitMultiplier` = 1.5
  - `StopLossMultiplier` = 1.5
- **Filtros**: nenhum.
- **Complexidade**: moderado.
- **Período**: configurável.
