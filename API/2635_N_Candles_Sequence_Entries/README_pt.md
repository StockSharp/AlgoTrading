# Estratégia N Candles com Entradas em Sequência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Conceito
A estratégia N Candles escaneia o mercado em busca de velas consecutivas que fecham todas na mesma direção. Uma vez que um número configurável de velas altistas ou baixistas aparecer, a estratégia entra na direção da sequência. A implementação é uma conversão direta do assessor especializado do MetaTrader "N Candles v4" e preserva seus controles de risco, configuração baseada em pips e comportamento opcional de trailing stop dentro da API de alto nível do StockSharp.

## Condições de entrada
- Cada vela concluída é avaliada uma vez.
- Velas que fecham para cima são contadas como altistas, velas que fecham para baixo como baixistas, e velas doji reiniciam a sequência.
- Quando `ConsecutiveCandles` velas altistas (ou baixistas) aparecem em uma linha, a estratégia envia uma ordem de mercado na direção do movimento.
- Limites de empilhamento estilo hedge ou limites de exposição estilo neteado são aplicados dependendo do `AccountingMode` selecionado.

## Gestão de saídas
- `StopLossPips` e `TakeProfitPips` definem níveis de saída estáticos medidos em pips a partir do preço médio de entrada da posição ativa.
- Se `TrailingStopPips` for maior que zero, o nível do stop segue o preço mais favorável:
  - Quando não existe um stop fixo (por exemplo quando `StopLossPips` é zero), a estratégia aguarda até que o preço se mova `TrailingStopPips` em favor da operação antes de colocar um stop de equilíbrio.
  - Uma vez que um stop foi definido, ele se move em direção ao mercado quando a distância entre o preço e o stop supera `TrailingStopPips + TrailingStepPips`.
- Os níveis de proteção são recalculados sempre que o tamanho da posição muda e são verificados contra cada vela concluída, garantindo que qualquer evento de stop-loss ou take-profit feche a operação imediatamente.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `ConsecutiveCandles` | Número de velas idênticas necessárias para acionar uma entrada. | 3 |
| `TakeProfitPips` | Distância do take-profit em pips. Usar zero para desabilitar o alvo. | 50 |
| `StopLossPips` | Distância do stop-loss em pips. Usar zero para desabilitar o stop. | 50 |
| `TrailingStopPips` | Distância do trailing stop em pips. Zero desabilita o trailing. | 10 |
| `TrailingStepPips` | Movimento adicional necessário antes de o trailing stop avançar. | 4 |
| `MaxPositionsPerDirection` | Número máximo de entradas empilhadas por direção no hedging. | 2 |
| `MaxNetVolume` | Tamanho máximo da posição líquida absoluta ao operar em modo neteado. | 2 |
| `AccountingMode` | Alternar entre `Netting` (limite de volume) e `Hedging` (limite de contagem de entradas). | Netting |
| `CandleType` | Agregação de velas usada para detecção de padrões. | Velas de 1 minuto |

Todos os parâmetros baseados em pips são convertidos para offsets de preço usando o tamanho do tick do instrumento. Se o instrumento tiver 3 ou 5 casas decimais, o tamanho do pip é escalado por um fator de dez para refletir a definição do MetaTrader.

## Notas de implementação
- A estratégia depende da subscrição de velas de alto nível do StockSharp (`SubscribeCandles`) e evita buffers de histórico manuais.
- A lógica de proteção rastreia o preço mais alto (para posições compradas) ou mais baixo (para posições vendidas) visto após a entrada para emular o comportamento de trailing original.
- Os limites de posição se adaptam automaticamente ao `Volume` base da estratégia. Aumentar o `Volume` expande os tamanhos de ordens de stop e take-profit proporcionalmente.
- Mensagens de log são emitidas sempre que uma saída de proteção (stop ou take-profit) fecha uma posição, fornecendo clareza durante os backtests.

## Dicas de uso
- Escolher o modo `Hedging` ao simular plataformas que permitem múltiplos tickets por direção, ou ficar com `Netting` para refletir contas de posição única.
- Definir `TrailingStepPips` como zero para um trailing stop clássico que se move sempre que o mercado avança `TrailingStopPips`.
- Como as saídas são avaliadas em velas concluídas, considerar um intervalo de velas mais curto se a precisão intrabar for crítica.
