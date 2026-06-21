# Estratégia Simples de Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este exemplo demonstra como gerenciar uma posição aberta com um trailing stop usando a API de alto nível do StockSharp.

## Visão geral
- Abre uma única posição comprada após receber a primeira vela concluída.
- Ativa a proteção de posição com um trailing stop.
- O preço do stop segue o preço atual a uma distância fixa.

## Parâmetros
- `TrailPoints` – distância em pontos de preço usada para o trailing do stop.
- `CandleType` – tipo de velas processadas pela estratégia.

## Lógica
1. Ao iniciar, a estratégia se inscreve em velas e ativa `StartProtection` com trailing.
2. Após a primeira vela concluída, a estratégia compra a preço de mercado.
3. Quando o preço se move a favor da posição, o nível do stop é movido para manter a distância definida por `TrailPoints`.
4. Se o preço reverter e tocar o trailing stop, a posição é fechada automaticamente.

A estratégia é simplificada e tem como objetivo mostrar o uso básico do trailing stop.
