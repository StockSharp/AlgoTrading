# Estratégia Max Profit Min Loss Options
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina médias móveis rápidas e lentas com RSI, MACD e um filtro de volume. Entra comprado quando as condições de tendência e momentum se alinham e usa stop loss e trailing profit para as saídas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: MA rápida > MA lenta, MACD cruza acima da sinal, RSI > sobrevendido com padrão ascendente, volume acima da média.
  - **Vendido**: MA rápida < MA lenta, MACD cruza abaixo da sinal, RSI < sobrecomprado com padrão descendente, volume acima da média.
- **Saída**: sinal oposto ou stop-loss/trailing profit.
- **Stops**: stop loss percentual e trailing profit.
- **Valores padrão**:
  - Comprimento de MA rápida = 9
  - Comprimento de MA lenta = 21
  - Comprimento de RSI = 14
  - Comprimento de SMA de volume = 20
  - Stop loss = 1%
  - Trailing profit = 4%
- **Indicadores**: MA, RSI, MACD, SMA de volume
- **Período**: velas de 5 minutos por padrão
