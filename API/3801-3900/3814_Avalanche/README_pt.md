# Estratégia de Avalanche
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Avalanche é um sistema de reversão à média em estilo de grade inspirado no consultor especialista original MetaTrader Avalanche v1.2. A ideia é monitorar a relação entre o preço e um preço de referência de equilíbrio (ERP) de prazo superior calculado como uma média móvel simples. Quando o preço é negociado abaixo do ERP, a estratégia espera uma recuperação em direção à média e acumula posições longas. Quando o preço é negociado acima do ERP, a estratégia busca uma queda e acumula posições vendidas. Cada posição adicional é espaçada por limites de distância configuráveis, enquanto cada entrada recebe níveis individuais de stop-loss e take-profit.

Esta porta StockSharp concentra-se na perna "em direção" do algoritmo original. As ordens de hedge fora do ERP da versão MQL não são replicadas porque as estratégias StockSharp operam em uma única posição líquida, mas a lógica de empilhamento, buffer e realização de lucros da grade permanece fiel à abordagem original.

## Como funciona

1. Assine duas séries de velas: o período de negociação e um período de ERP que alimenta a média móvel.
2. Calcule uma média móvel simples do ERP e determine se o preço está posicionado acima ou abaixo dela. Um buffer configurável evita inversões frequentes.
3. Quando uma nova tendência de ERP aparecer, feche qualquer grade aberta e aguarde novos sinais.
4. Abra uma posição inicial na direção que deve trazer o preço de volta ao ERP (comprado abaixo, vendido acima) se a sinalização `OpenStartingOrders` estiver habilitada.
5. Continue adicionando posições na mesma direção quando o preço avançar na distância `IntervalToward` (empilhamento de impulso).
6. Adicione entradas de proteção adicionais quando o preço se mover contra a grade em `IntervalToward + StackBufferToward` (empilhamento martingale).
7. Cada entrada tem sua própria meta de stop-loss e take-profit medida em pontos, garantindo que as pernas lucrativas possam ser fechadas individualmente enquanto a grade continua a gerenciar a exposição restante.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `BaseVolume` | Volume base do pedido usado antes de aplicar multiplicadores. |
| `TowardMultiplier` | Multiplicador de lote para entradas padrão para ERP. |
| `TowardInterestMultiplier` | Multiplicador usado quando o instrumento paga swap positivo na direção de negociação. |
| `IntervalToward` | Distância em pontos necessária para adicionar uma pilha de acompanhamento de tendências. |
| `StackBufferToward` | Buffer adicional adicionado ao intervalo ao acumular contra movimentos adversos de preços. |
| `TakeProfitToward` | Distância de lucro em pontos para cada entrada. Defina como `0` para desativar. |
| `StopLossToward` | Distância de stop-loss em pontos para cada entrada. Defina como `0` para desativar. |
| `ErpPeriod` | Número de períodos da média móvel simples do ERP. |
| `ErpChangeBuffer` | Buffer (em pontos) aplicado ao redor do ERP antes de mudar o viés. |
| `CandleType` | Prazo de negociação usado para acionar entradas e saídas. |
| `ErpCandleType` | Prazo usado para calcular a média móvel do ERP. |
| `OpenStartingOrders` | Se ativado, abre imediatamente a primeira ordem de grade quando as condições são satisfeitas. |

## Diferenças em relação ao original EA

- Somente a perna voltada para ERP é implementada porque a estratégia StockSharp mantém uma única posição líquida. As ordens de proteção são omitidas.
- A execução de ordens depende de ordens de mercado em vez de ordens de stop pendentes usadas pela versão MQL.
- A detecção da direção de troca é preservada para escolher entre os multiplicadores padrão e de juros.

## Dicas de uso

- Ajuste `IntervalToward` e `StackBufferToward` para controlar a agressividade com que a grade adiciona novas negociações.
- Garantir que o instrumento e os prazos selecionados forneçam liquidez suficiente; os sistemas de rede podem acumular uma exposição considerável.
- Combine a estratégia com controles de risco externos (paradas de ações, filtros de sessão) ao executar em produção.
