# USD/CHF CCI Estratégia de interrupção do canal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de interrupção de canal em USD/CHF CCI** é uma StockSharp implementação de alto nível do MetaTrader 4 consultor especialista `UsdChf_new`. A estratégia escuta os rompimentos do Commodity Channel Index (CCI) no período H4 e implanta ordens de stop pendentes acima ou abaixo do preço atual. Depois que uma ordem é preenchida, a posição é protegida pelas mesmas regras de gerenciamento de dinheiro baseadas em pip usadas no robô original: um stop loss fixo, cancelamento opcional de ordens pendentes obsoletas, realocação do ponto de equilíbrio e gerenciamento de trailing stop.

Esta conversão mantém o fluxo de execução original, mas abrange o fluxo de trabalho idiomático StockSharp: assinaturas de velas, vinculações de indicadores e auxiliares de pedidos de alto nível (`BuyStop`, `SellStop`, `BuyMarket`, `SellMarket`). Todas as distâncias de risco ainda são configuradas em pips para permanecerem familiares aos usuários de Forex.

## Lógica de negociação

1. **Indicador e Sinais**
   - Calcule um CCI com o período configurado nas velas H4 finalizadas.
   - Monitore os limites do canal: `+CCI Channel` e `-CCI Channel`.
   - Detecte cruzamentos do valor atual em relação ao valor anterior para gerar sinais.
     - Cruzar **para cima** através de `-CCI Channel` prepara um **stop de compra** acima do preço.
     - Cruzar **para baixo** através de `+CCI Channel` prepara um **stop de venda** abaixo do preço.
2. **Pedidos pendentes**
   - As ordens stop são compensadas do fechamento da vela em `Entry Indent (pips)` e arredondadas para o passo do instrumento.
   - Apenas uma ordem pendente pode estar ativa por vez. Criar um novo cancela o lado oposto.
   - Se o mercado se afastar mais de `Cancel Distance (pips)`, a ordem pendente será cancelada para evitar a perseguição do preço.
3. **Gerenciamento de posição**
   - As posições preenchidas herdam a distância de stop loss original.
   - Quando a negociação ganha pelo menos `Break Even (pips)`, o stop de proteção passa para o preço de entrada.
   - Após o lucro exceder `Trailing Stop (pips)`, o stop segue o preço enquanto mantém o gap configurado.
   - Os cruzamentos opostos CCI forçam uma saída de posição e colocam uma nova ordem de stop na nova direção.

## Parâmetros

| Parâmetro | Descrição | Padrão | Otimizável |
|-----------|-------------|---------|-------------|
| `CandleType` | Série de velas usada para cálculos CCI (padrão H4). | Período de 4 horas | Não |
| `CciPeriod` | CCI período médio. | 73 | Sim |
| `CciChannel` | Nível CCI absoluto formando os limites do canal. | 120 | Sim |
| `EntryIndentPips` | Distância (em pips) entre o preço de mercado e a ordem de stop pendente. | 30 | Sim |
| `StopLossPips` | Distância inicial de stop loss em pips. | 95 | Sim |
| `CancelDistancePips` | Gap máximo antes de cancelar ordens pendentes. | 30 | Sim |
| `TrailingStopPips` | Distância de parada móvel uma vez ativada. | 110 | Sim |
| `BreakEvenPips` | O lucro necessário antes que o stop seja movido para o nível de entrada. | 60 | Sim |

Todas as distâncias pip são convertidas em compensações de preço usando os instrumentos `PriceStep` e `Decimals`. Para símbolos Forex de 3/5 dígitos, o pip é igual a dez etapas de preço, caso contrário, é igual a uma única etapa.

## Notas de uso

1. Anexe a estratégia a um título USD/CHF (ou qualquer instrumento onde a gestão de risco baseada em pip seja relevante).
2. Defina o volume de negociação desejado por meio da propriedade base `Strategy.Volume`.
3. Opcionalmente, ajuste os parâmetros baseados em pip para corresponder às especificações do contrato do corretor.
4. Execute backtests no Designer/Tester para validar o comportamento antes de entrar em operação.

## Notas de conversão

- O especialista MetaTrader iterou por meio de pools de pedidos brutos. Em StockSharp a estratégia armazena referências aos pedidos pendentes ativos e, em vez disso, usa auxiliares de cancelamento de alto nível.
- Stop loss, ponto de equilíbrio e trailing são implementados por meio de saídas de mercado explícitas porque a modificação de ordens do lado da corretora não faz parte do API de alto nível.
- Todos os comentários embutidos foram traduzidos para o inglês e ampliados para maior clareza.
