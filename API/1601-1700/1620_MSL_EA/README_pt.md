# Estratégia MSL EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

MSL EA é uma estratégia de rompimento que constrói linhas dinâmicas de suporte e resistência a partir de extremos locais recentes. A estratégia detecta máximas e mínimas fractais de curto prazo, ajusta-as por uma distância especificada em ticks e abre posições quando o preço fecha além desses níveis. Foi convertida da implementação original em MQL4.

## Como Funciona

1. O algoritmo rastreia máximas e mínimas de velas para determinar extremos locais.
2. A máxima mais alta e a mínima mais baixa entre os últimos *Level* extremos detectados são armazenadas como linhas de resistência e suporte.
3. Cada linha é deslocada por *Distance* ticks para contabilizar o ruído do mercado.
4. Quando o preço de fechamento rompe acima da linha superior, uma posição comprada é aberta; quando rompe abaixo da linha inferior, uma posição vendida é aberta.
5. O número de negociações simultâneas é limitado por *Max Trades*.

## Parâmetros

- **Max Trades** – máximo de posições abertas permitidas.
- **Level** – número de extremos locais usados para construir os níveis.
- **Distance** – deslocamento a partir do extremo em ticks ao colocar as linhas.
- **Candle Type** – período das velas processadas pela estratégia.

## Notas

Esta versão em C# usa a API de alto nível do StockSharp e inclui comentários em inglês. As funções de gestão de risco da biblioteca auxiliar MQL4 original são simplificadas para verificações básicas de posição.
