# Parada final adicional
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Porto do MetaTrader especialista **AddOn_TrailingStop**. A estratégia não abre posições por si só e apenas ajusta os trailing stops para uma posição líquida existente.

## Como funciona

- Assina os dados da Level1 para monitorar as melhores ofertas e cotações mais recentes.
- Calcula o tamanho do pip a partir dos decimais de segurança para que as entradas se comportem como em MetaTrader (4/5 dígitos = 0,0001 pip, 2/3 dígitos = 0,01 pip).
- Quando uma posição longa é aberta e o preço de compra avança `TrailingStartPips` pips, a estratégia move o trailing stop interno para `Bid - TrailingStartPips` pips.
- A parada longa só é avançada quando o novo nível for pelo menos `TrailingStepPips` pips maior que a parada anterior.
- Quando uma posição curta é aberta e o preço de venda cai `TrailingStartPips` pips, a estratégia move o trailing stop interno para `Ask + TrailingStartPips` pips.
- O stop curto só é avançado quando o novo nível for pelo menos `TrailingStepPips` pips inferior ao stop anterior.
- Se a cotação atual cruzar o trailing stop, a estratégia fecha toda a posição no mercado e reinicia o seu estado.

## Parâmetros

- `EnableTrailing` (padrão **true**) – ativa ou desativa o gerenciamento de trailing stop.
- `TrailingStartPips` (padrão **15**) – lucro em pips necessário antes da ativação do trailing.
- `TrailingStepPips` (padrão **5**) – lucro extra em pips necessário antes que o stop possa se mover novamente.
- `MagicNumber` (padrão **0**) – identificador mantido para paridade com o especialista MQL. É informativo porque StockSharp opera na posição estratégica atual.

## Notas

- Requer um feed de dados configurado de `Security`, `Portfolio` e nível 1.
- Projetado para complementar outras estratégias que lidam com entradas.
- Usa `StrategyParam<T>` para que cada entrada possa ser otimizada ou exposta na IU.
- Envia ordens `BuyMarket`/`SellMarket` quando o trailing stop é atingido porque StockSharp gerencia automaticamente as ordens de proteção após a saída da posição.
