# Estratégia de Tendência FATL MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um sistema seguidor de tendência baseado no indicador **FATL MACD**. A FATL (Fast Adaptive Trend Line) é subtraída do preço para produzir um oscilador similar ao MACD, que é então suavizado por uma média móvel adaptativa. Valores positivos indicam momentum de alta; valores negativos indicam momentum de baixa.

O algoritmo analisa a inclinação deste oscilador em cada candle concluído:

- Quando o valor anterior é inferior ao valor anterior a ele, o oscilador virou para cima. Se o valor atual subir ainda mais, a estratégia abre uma posição comprada e fecha quaisquer posições vendidas.
- Quando o valor anterior é superior ao valor anterior a ele, o oscilador virou para baixo. Se o valor atual continuar caindo, a estratégia abre uma posição vendida e fecha quaisquer posições compradas.

Todos os parâmetros principais são configuráveis:

- **Fast EMA** – período da média móvel rápida do MACD (padrão 12).
- **Slow EMA** – período da média móvel lenta do MACD (padrão 26).
- **Signal EMA** – período da linha de sinal do MACD (padrão 9).
- **Candle Type** – série de candles utilizada para o cálculo do indicador.

As posições são abertas com ordens a mercado e são fechadas quando aparece um sinal oposto.
