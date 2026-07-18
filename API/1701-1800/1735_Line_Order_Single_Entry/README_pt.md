# Estratégia de Entrada Única por Linha de Ordem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Line Order é uma tradução do script MQL4 "LineOrder" (10715). A estratégia abre uma posição quando o preço de mercado atinge uma linha de entrada predefinida e em seguida gerencia a posição com stop-loss, take-profit e trailing stop opcional.

## Parâmetros

- `Entry Price` – nível de preço que aciona uma posição.
- `Stop Loss (pips)` – distância da entrada ao stop loss inicial.
- `Take Profit (pips)` – distância da entrada ao take profit.
- `Trailing Stop (pips)` – distância opcional do trailing stop. Quando definido como zero, o trailing é desabilitado.
- `Candle Type` – tipo de velas usadas para processamento.

## Lógica de Negociação

1. A estratégia assina a série de velas selecionada.
2. Quando uma vela concluída fecha acima do preço de entrada, uma posição comprada é aberta. Quando fecha abaixo do preço de entrada, uma posição vendida é aberta.
3. Após a entrada, os níveis de stop-loss e take-profit são calculados usando o passo de preço do instrumento.
4. Se o trailing stop estiver habilitado, o nível de stop se move na direção do trade.
5. A posição é fechada quando o preço atinge o nível de stop-loss ou take-profit.

Esta é uma adaptação simplificada do script MQL original, focada na execução automatizada de ordens em uma linha definida pelo usuário.
