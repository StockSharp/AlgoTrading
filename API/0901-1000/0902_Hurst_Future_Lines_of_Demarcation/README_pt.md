# Estratégia Hurst Future Lines of Demarcation
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia usa uma FLD (Future Line of Demarcation) suavizada e três comprimentos de ciclo (sinal, trading, tendência). Ela entra quando o preço cruza a FLD de sinal em estados de tendência específicos e sai em um cruzamento entre os valores selecionados.

## Detalhes

- **Critérios de entrada**:
  - Comprar quando o preço cruza acima da FLD de sinal enquanto o estado de tendência é igual a 1.
  - Vender quando o preço cruza abaixo da FLD de sinal enquanto o estado de tendência é igual a 6.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Fechar posição quando `CloseTrigger1` cruza `CloseTrigger2` na direção oposta ao trade.
- **Stops**: Não.
- **Valores padrão**:
  - `SmoothFld` = false
  - `FldSmoothing` = 5
  - `SignalCycleLength` = 5
  - `TradeCycleLength` = 20
  - `TrendCycleLength` = 80
  - `CloseTrigger1` = Price
  - `CloseTrigger2` = Trade
