# Estratégia Multik SMA Exp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia implementa uma abordagem contrária baseada na inclinação de uma média móvel simples (SMA). Foi portada do consultor especialista MetaTrader 5 "Multik_SMA_Exp".

A estratégia monitora os últimos três valores de SMA. Se a SMA estiver caindo durante os dois segmentos completados mais recentes, a estratégia entra em uma posição comprada. Se a SMA estiver subindo durante os dois segmentos, abre uma posição vendida. As posições são fechadas quando a inclinação da SMA se reverte.

## Parâmetros
- **MA Period** – comprimento da média móvel simples. Padrão: 50.
- **Candle Type** – tipo de velas usadas para cálculos. Padrão: período de 1 minuto.

## Regras de negociação
1. Em cada vela fechada, calcular a SMA.
2. Determinar as inclinações:
   - `dsma1 = SMA[n-1] - SMA[n-2]`
   - `dsma2 = SMA[n-2] - SMA[n-3]`
3. Entrada:
   - Se `dsma1 < 0` e `dsma2 < 0` e não há posição comprada, comprar.
   - Se `dsma1 > 0` e `dsma2 > 0` e não há posição vendida, vender.
4. Saída:
   - Se mantendo uma posição comprada e `dsma1 > 0`, fechar a posição comprada.
   - Se mantendo uma posição vendida e `dsma1 < 0`, fechar a posição vendida.

O volume de novas ordens usa o `Volume` da estratégia mais o valor absoluto da posição atual para reverter completamente quando necessário.
