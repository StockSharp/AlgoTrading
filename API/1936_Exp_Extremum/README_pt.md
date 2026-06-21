# Estratégia Exp Extremum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera reversões detectadas comparando extremos de preço em uma janela de retrospecto. Ela observa se a vela atual empurra o preço além de máximas ou mínimas anteriores e reage quando o sinal desta comparação muda.

## Como funciona

1. Para cada vela concluída, a estratégia encontra:
   - A máxima mais baixa nas últimas *N* barras.
   - A mínima mais alta nas últimas *N* barras.
2. As diferenças entre a máxima/mínima atual e esses níveis são somadas.
3. Uma soma positiva indica pressão de alta, uma soma negativa indica pressão de baixa.
4. Quando o sinal de duas barras atrás se opõe ao sinal de uma barra atrás, aparece um sinal de reversão:
   - Para cima então Para baixo → abrir posição comprada.
   - Para baixo então Para cima → abrir posição vendida.
5. Permissões opcionais permitem desabilitar independentemente a abertura ou fechamento de posições compradas/vendidas.

## Parâmetros

- `Length` – período do indicador para cálculos de extremos.
- `CandleType` – período das velas recebidas.
- `BuyPosOpen` / `SellPosOpen` – permissões para abrir posições compradas ou vendidas.
- `BuyPosClose` / `SellPosClose` – permissões para fechar posições compradas ou vendidas.

## Notas

A estratégia usa a API de alto nível com assinaturas de velas e indicadores integrados `Highest`/`Lowest`. As posições são abertas com ordens a mercado e fechadas via `ClosePosition()` quando o sinal oposto aparece.
