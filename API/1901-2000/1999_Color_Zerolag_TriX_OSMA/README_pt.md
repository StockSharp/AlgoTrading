# Estratégia Color Zerolag TriX OSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

Esta estratégia utiliza um oscilador TRIX OSMA de zero defasagem construído a partir de cinco períodos TRIX diferentes. Cada componente TRIX é ponderado e suavizado para formar um único oscilador que reage a mudanças de tendência com defasagem mínima. Uma posição comprada é aberta quando o oscilador vira para cima e uma posição vendida quando ele vira para baixo.

## Como Funciona

1. Calcular cinco valores TRIX usando médias móveis exponenciais triplas e a taxa de mudança.
2. Combinar os valores TRIX com seus pesos para formar um valor de tendência rápida.
3. Suavizar a tendência rápida duas vezes para criar um oscilador OSMA de zero defasagem.
4. Detectar reversões de tendência comparando os dois últimos valores do oscilador.
5. Entrar comprado em uma virada para cima e vendido em uma virada para baixo; posições opostas existentes são fechadas antes de abrir uma nova.

## Parâmetros

- `Smoothing1` – fator de suavização para a tendência lenta.
- `Smoothing2` – fator de suavização para a linha OSMA.
- `Factor1..Factor5` – pesos aplicados a cada componente TRIX.
- `Period1..Period5` – períodos para os cinco cálculos TRIX.
- `CandleType` – série de candles usada para cálculos.

## Indicadores

- TripleExponentialMovingAverage
- RateOfChange
- Combinação personalizada de TRIX OSMA de zero defasagem

## Notas

A estratégia requer que todos os cinco indicadores TRIX estejam formados antes de gerar sinais. A proteção para stops e alvos é ativada via `StartProtection`.
