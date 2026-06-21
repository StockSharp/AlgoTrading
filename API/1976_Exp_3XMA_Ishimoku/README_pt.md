# Estratégia Exp 3XMA Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão do especialista MQL `exp_3xma_ishimoku`. Ela usa o indicador Ichimoku com períodos reduzidos e age de forma contrária às rupturas de nuvem.

A linha Kijun é comparada com os limites da nuvem Ichimoku. Quando Kijun cai de acima da nuvem para dentro dela, a estratégia fecha posições vendidas e abre uma posição comprada se a compra for permitida. Quando Kijun sobe de abaixo da nuvem para dentro dela, as posições compradas são fechadas e uma posição vendida pode ser aberta.

O período padrão para análise são velas de 4 horas.

## Parâmetros
- **Tenkan Period** – comprimento da linha Tenkan-sen.
- **Kijun Period** – comprimento da linha Kijun-sen.
- **Senkou Span B Period** – período do segundo trecho Senkou.
- **Allow Buy** – habilitar a abertura de posições compradas.
- **Allow Sell** – habilitar a abertura de posições vendidas.
- **Candle Type** – série de candles utilizada para o cálculo do indicador.

## Como funciona
1. Assina a série de candles selecionada e vincula o indicador Ichimoku.
2. Processa apenas candles finalizados.
3. Detecta quando a linha Kijun cruza as bordas da nuvem.
4. Fecha posições opostas e abre uma nova na direção do sinal, se permitido.

## Aviso
Este exemplo destina-se a fins educacionais e não constitui aconselhamento financeiro. Use por sua própria conta e risco.
