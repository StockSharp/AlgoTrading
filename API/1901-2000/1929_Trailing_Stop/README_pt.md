# Estratégia de Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia implementa a lógica de trailing stop do script MQL original `TRAILING.mq4`. Ela gerencia uma posição aberta existente e a fecha quando o mercado atinge um alvo de lucro especificado ou atinge um stop loss. Quando o parâmetro de trailing está habilitado, o nível de stop acompanha o preço para travar lucros.

## Parâmetros
- **TakeProfit** – distância de lucro do preço de entrada em unidades absolutas de preço.
- **StopLoss** – distância adversa máxima permitida do preço de entrada.
- **Trailing** – distância usada para o trailing dinâmico do nível de stop.
- **CandleType** – série de candles usada para obter atualizações de preço.

## Como Funciona
1. A estratégia assina a série de candles escolhida.
2. Após cada candle concluído, a posição atual é avaliada.
3. Para posições compradas, a estratégia fecha a posição quando o lucro excede *TakeProfit* ou a perda excede *StopLoss*.
4. Se *Trailing* for maior que zero, o nível de stop sobe com o preço. Quando o preço cai abaixo do trailing stop, a posição é fechada.
5. Posições vendidas seguem a mesma lógica na direção oposta.
6. O preço de entrada é registrado a partir do primeiro trade executado e reiniciado quando a posição é fechada.

## Notas
- A estratégia usa a API de alto nível com `Bind` para processar candles.
- Ela não abre novas posições por conta própria; apenas gerencia uma posição já aberta.
- Os parâmetros são expostos via `StrategyParam` e podem ser otimizados.
