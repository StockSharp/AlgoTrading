# Estratégia Binary Wave
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Binary Wave combina vários indicadores técnicos clássicos em uma única "onda" binária. Cada indicador contribui com +1 ou -1 dependendo de seu estado de alta ou baixa. A soma ponderada de todos os sinais forma a onda final usada para as decisões de negociação.

## Parâmetros

- **Mode** – algoritmo de entrada: `Breakdown` reage ao cruzamento do zero; `Twist` reage a mudanças de direção da onda.
- **Candle Type** – período das velas para todos os cálculos.
- **Indicator Periods** – comprimentos para MA, MACD (rápido, lento, sinal), CCI, Momentum, RSI e ADX.
- **Weights** – contribuição de cada indicador para a onda. Definir um peso como 0 desabilita o indicador.
- **Trading Permissions** – habilitar ou desabilitar entradas e saídas compradas/vendidas separadamente.
- **Risk** – stop-loss e take-profit em porcentagem do preço de entrada.

## Como funciona

1. Assinar a série de velas especificada e calcular todos os indicadores.
2. Para cada vela finalizada, avaliar o estado de cada indicador e convertê-lo em um valor binário (+1 / -1).
3. Somar os valores ponderados para obter a onda atual.
4. Gerar sinais de negociação:
   - **Breakdown**: entrar comprado quando a onda cruza acima do zero, entrar vendido quando cruza abaixo do zero.
   - **Twist**: entrar comprado quando a onda muda de direção para cima, entrar vendido quando gira para baixo.
5. O stop-loss e take-profit de proteção opcionais são gerenciados pela proteção de posição integrada.

Esta abordagem permite a combinação flexível de múltiplos indicadores mantendo a lógica de negociação direta.
