# Estratégia CCI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina cruzamentos do CCI com um filtro MACD e bandas EMA/ATR para operar na direção da tendência.

## Detalhes

- **Dados**: Candles de preço.
- **Entrada**: Comprado quando CCI cruza acima de zero, MACD acima de zero, preço acima de EMA125 e EMA750 mas abaixo da banda ATR superior; vendido quando o oposto.
- **Saída**: Posição fecha no sinal oposto.
- **Instrumentos**: Quaisquer instrumentos.
- **Risco**: Sem stop loss ou take profit.
