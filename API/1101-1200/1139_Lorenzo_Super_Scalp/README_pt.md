# Estratégia Lorenzo SuperScalp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de scalping combina RSI, Bandas de Bollinger e MACD. Compra quando o RSI está abaixo de 45, o preço está próximo da banda inferior e o MACD cruza para cima. Vende quando o RSI está acima de 55, o preço está próximo da banda superior e o MACD cruza para baixo. Um número mínimo de barras entre operações evita a re-entrada rápida.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `RSI < 45` && `Close < LowerBand * 1.02` && `MACD` cruza acima do sinal.
  - **Vendido**: `RSI > 55` && `Close > UpperBand * 0.98` && `MACD` cruza abaixo do sinal.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `RSI Length` = 14
  - `Bollinger Length` = 20
  - `Bollinger Multiplier` = 2
  - `MACD Fast` = 12
  - `MACD Slow` = 26
  - `MACD Signal` = 9
  - `Min Bars` = 15
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Não
  - Complexidade: Moderado
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
