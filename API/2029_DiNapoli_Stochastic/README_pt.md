# Estratégia DiNapoli Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um sistema de trading baseado no oscilador **DiNapoli Stochastic**. Reage aos cruzamentos entre as linhas %K e %D do indicador estocástico.

## Lógica da estratégia

1. Subscrever velas do período selecionado.
2. Calcular os valores do DiNapoli Stochastic usando o oscilador Stochastic padrão com períodos de suavização.
3. Fechar posições vendidas quando o %K anterior estava acima do %D.
4. Fechar posições compradas quando o %K anterior estava abaixo do %D.
5. Abrir uma posição comprada quando %K cruza abaixo de %D e operações compradas são permitidas.
6. Abrir uma posição vendida quando %K cruza acima de %D e operações vendidas são permitidas.

## Parâmetros

- `FastK` – período base para %K.
- `SlowK` – período de suavização para %K.
- `SlowD` – período de suavização para %D.
- `BuyOpen` – habilitar ou desabilitar entradas compradas.
- `SellOpen` – habilitar ou desabilitar entradas vendidas.
- `BuyClose` – habilitar ou desabilitar o fechamento de posições compradas.
- `SellClose` – habilitar ou desabilitar o fechamento de posições vendidas.
- `CandleType` – período de velas utilizado para os cálculos.

## Notas

A estratégia utiliza a API de alto nível do StockSharp e processa apenas velas finalizadas. Os valores do indicador são obtidos através de `BindEx` sem utilizar requisições de valores históricos.
