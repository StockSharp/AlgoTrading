# Estratégia TP SL Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão direta do consultor especialista MetaTrader 5 "TP SL Trailing". A estratégia não gera entradas por si mesma. Em vez disso, gerencia posições existentes instalando um stop-loss e take-profit de proteção e arrastando o stop assim que a operação se torna lucrativa. A configuração baseada em pips corresponde aos parâmetros do script original e permite que a lógica seja anexada a qualquer símbolo suportado pelo StockSharp.

## Lógica de negociação
- Quando uma nova posição aparece, a estratégia pode opcionalmente definir um stop-loss e take-profit inicial usando as distâncias em pips configuradas. Este comportamento é controlado pelo flag **Only Zero Values**, assim como no consultor especialista original.
- Para posições compradas, a estratégia move o stop-loss para cima assim que o lucro não realizado excede a soma do trailing stop e o trailing step. O stop é movido para `preço atual - trailing stop`, garantindo que uma porção mínima do lucro seja assegurada.
- Para posições vendidas, a estratégia reflete a mesma ideia e move o stop para baixo assim que o lucro excede os limiares de trailing.
- Se tanto o trailing stop quanto o trailing step forem zero, a estratégia deixa o stop-loss intocado.
- O nível de take-profit nunca é arrastado. É definido apenas durante a fase de colocação inicial quando **Only Zero Values** está habilitado, replicando completamente o comportamento MQL.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período das velas usadas para rastrear movimentos de preço. Um período mais rápido melhora a precisão do trailing. |
| `StopLossPips` | Distância em pips entre o preço de entrada e o stop-loss inicial. Aplicado apenas quando **Only Zero Values** está habilitado. |
| `TakeProfitPips` | Distância em pips entre o preço de entrada e o take-profit inicial. Aplicado apenas quando **Only Zero Values** está habilitado. |
| `TrailingStopPips` | Distância de trailing core em pips. Define o quão longe atrás do preço atual o stop deve permanecer após a ativação. |
| `TrailingStepPips` | Buffer adicional de pips que deve ser excedido antes que o stop se mova novamente. Previne atualizações de stop muito frequentes. |
| `OnlyZeroValues` | Corresponde ao flag EA original. Quando habilitado, as ordens de proteção iniciais são criadas apenas para posições que atualmente não têm stop-loss ou take-profit atribuídos. |

## Notas de conversão
- As distâncias em pips são convertidas para unidades de preço usando o `PriceStep` do instrumento. Isso mantém a lógica agnóstica ao instrumento e reflete o ajuste de 3/5 dígitos na versão MQL.
- As ordens de proteção são re-registradas sempre que a lógica de trailing move o stop-loss. As ordens ativas de uma posição anterior são canceladas automaticamente quando o tamanho da posição retorna a zero.
- Todos os comentários de código estão escritos em inglês, enquanto esta documentação é intencionalmente detalhada para ajudar a reproduzir cada decisão tomada durante o processo de portabilidade.
