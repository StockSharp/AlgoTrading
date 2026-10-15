# Estratégia Color Zero Lag MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa uma média móvel de atraso zero (ZLMA) para detectar reversões de tendência. Abre posições compradas quando a ZLMA vira para cima e abre posições vendidas quando a ZLMA vira para baixo. As posições existentes são fechadas quando a inclinação do indicador se reverte.

## Parâmetros

- **Length**: Período da média móvel de atraso zero.
- **Candle Type**: Período dos candles usados pela estratégia.
- **Open Buy**: Ativar abertura de posições compradas.
- **Open Sell**: Ativar abertura de posições vendidas.
- **Close Buy**: Fechar posições compradas quando a ZLMA vira para baixo.
- **Close Sell**: Fechar posições vendidas quando a ZLMA vira para cima.

## Lógica

1. Subscrever candles do período selecionado.
2. Calcular a média móvel de atraso zero.
3. Rastrear os dois últimos valores da ZLMA para determinar a direção da inclinação.
4. Se a inclinação mudar de descendente para ascendente, fechar posições vendidas e abrir uma posição comprada.
5. Se a inclinação mudar de ascendente para descendente, fechar posições compradas e abrir uma posição vendida.

Esta abordagem simples segue a mudança de cor da média móvel de atraso zero para capturar possíveis reversões de tendência.
