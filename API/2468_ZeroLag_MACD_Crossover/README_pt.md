# Estratégia de Cruzamento ZeroLag MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o algoritmo **ZeroLagEA-AIP** do MetaTrader 5. Usa um MACD de zero lag construído a partir de duas médias móveis exponenciais de zero lag. O sistema abre uma posição vendida quando o valor do MACD aumenta em comparação com a barra anterior e abre uma posição comprada quando o MACD diminui. Se aparecer um sinal oposto enquanto há uma posição aberta, a posição atual é fechada e uma nova é aberta na barra seguinte.

## Lógica

1. Duas EMAs de zero lag com períodos configuráveis são calculadas.
2. Sua diferença multiplicada por 10 forma o valor do MACD de zero lag.
3. Uma operação é executada apenas quando a direção do MACD muda entre duas barras consecutivas (opcional).
4. O trading só é permitido entre as horas de início e fim configuradas. Todas as posições são fechadas à força fora desta janela ou no dia da semana e hora especificados.

## Parâmetros

- **Volume** – volume da ordem.
- **Fast EMA** – período da EMA rápida de zero lag.
- **Slow EMA** – período da EMA lenta de zero lag.
- **Use Fresh Signal** – se habilitado, opera apenas em uma nova mudança de direção do MACD.
- **Start Hour / End Hour** – limites da sessão de trading em UTC.
- **Kill Day / Kill Hour** – dia da semana e hora em que todas as posições são fechadas.
- **Candle Type** – dados de vela usados para cálculos.

## Notas

A estratégia usa a API de alto nível do StockSharp com `SubscribeCandles` e `Bind` para receber valores de indicadores. As posições são fechadas usando ordens a mercado.
