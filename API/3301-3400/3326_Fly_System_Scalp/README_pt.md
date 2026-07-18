# Estratégia Fly System Scalp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Fly System Scalp Strategy é um sistema de rompimento de alta frequência que reproduz o comportamento central do expert advisor *FlySystemEA* do MQL4. A estratégia monitora constantemente as melhores cotações bid/ask e implanta duas ordens stop simétricas ao redor do preço de mercado. O objetivo é capturar microtendências rápidas que surgem após consolidações de curto prazo, mantendo controle rígido sobre spread, comissões e limites de sessão.

A conversão foca nas seguintes mecânicas:

* Colocação automática de ordens buy stop e sell stop a uma distância configurável do mercado.
* Cancelamento automático de ordens pendentes quando o spread (incluindo comissão) excede o limiar admissível ou a negociação está fora da sessão permitida.
* Gestão opcional de take-profit e obrigatória de stop-loss anexada diretamente a novas ordens pendentes.
* Suporte a volume fixo manual e dimensionamento automático baseado em risco usando especificações contratuais da corretora (price step, step value, lot step, volume mínimo/máximo).
* Ciclo de negociação autorreinicializável que aguarda posições serem fechadas antes de armar um novo par de ordens stop.

A implementação StockSharp aproveita a API de alto nível (assinatura level-1 com bind) e segue as convenções exigidas do projeto: parâmetros são expostos por `StrategyParam`, comentários estão em inglês e o namespace usa declaração file-scoped.

## Lógica de negociação
1. **Feed Level 1:** a estratégia assina dados level-1 do ativo atribuído. Cada atualização registra o par bid/ask mais recente.
2. **Camada de validação:** antes de qualquer ação de negociação, o motor verifica:
   * A estratégia está online e permitida a operar.
   * A hora atual está dentro da janela opcional de negociação.
   * O spread mais comissão não excede `MaxSpread` pips.
3. **Colocação de ordens pendentes:** quando as condições acima são verdadeiras, nenhuma posição está aberta e a estratégia está pronta para um novo ciclo, duas ordens são preparadas:
   * Buy Stop em `Ask + PendingDistance * pip` com Stop Loss protetor e Take Profit opcional.
   * Sell Stop em `Bid - PendingDistance * pip` com proteções espelhadas.
   As ordens são registradas novamente quando a diferença entre preço desejado e real atinge `ModifyThreshold` pips.
4. **Gestão de ordens:** se uma posição abre, a ordem pendente oposta é cancelada imediatamente. Quando um ciclo é interrompido por violações de spread/tempo, todas as ordens pendentes são removidas e a estratégia aguarda condições válidas.
5. **Dimensionamento:** quando `AutoLotSize` está habilitado, o volume é derivado de `RiskFactor` por cento do patrimônio dividido pela perda por contrato na distância de stop configurada. O volume é arredondado ao passo de lote da corretora e limitado a mínimos/máximos.
6. **Proteção:** `StartProtection()` é invocado para que o StockSharp monitore a posição para liquidação emergencial se exigido pela infraestrutura.

## Parâmetros
| Nome | Descrição | Padrão |
|------|-------------|---------|
| `PendingDistance` | Distância em pips entre preço de mercado e ambas as ordens stop. | 4 |
| `StopLossDistance` | Distância de stop-loss em pips anexada a novas posições. | 0.4 |
| `TakeProfitDistance` | Distância de take-profit em pips quando habilitado. | 10 |
| `UseTakeProfit` | Habilita colocação de take-profit. | `false` |
| `MaxSpread` | Spread máximo permitido (pips); 0 desativa o filtro. | 1 |
| `CommissionInPips` | Comissão (em pips) adicionada ao filtro de spread. | 0 |
| `AutoLotSize` | Habilita dimensionamento baseado em risco. | `false` |
| `RiskFactor` | Percentual do patrimônio usado para dimensionar posições com auto sizing ativo. | 10 |
| `ManualVolume` | Volume fixo usado quando auto sizing está desabilitado. | 0.1 |
| `UseTimeFilter` | Habilita o filtro de sessão de negociação. | `false` |
| `TradeStartTime` | Hora de início da sessão (inclusiva). | 00:00:00 |
| `TradeStopTime` | Hora de término da sessão (exclusiva). | 00:00:00 |
| `ModifyThreshold` | Delta de preço (pips) exigido antes de registrar novamente uma ordem pendente. | 1 |

## Notas de uso
* Garanta que o instrumento alvo forneça `Step`, `PriceStep`, `StepPrice`, `LotStep`, `MinVolume` e `MaxVolume`, porque o dimensionamento automático depende desses valores. Quando os dados faltam, a estratégia retorna com segurança para `ManualVolume`.
* O valor de pip é estimado pela precisão decimal e passo de preço do ativo, correspondendo à lógica da implementação MQL original (incluindo tratamento especial para cotações Forex de 3/5 dígitos).
* Se `TradeStartTime` for igual a `TradeStopTime` enquanto `UseTimeFilter` está habilitado, a sessão é considerada sempre aberta. Quando o início é maior que o fim, a sessão cruza a meia-noite.
* A validação de spread adiciona `CommissionInPips` ao spread atual, replicando o comportamento em que a versão MQL combinava spread e comissão em um único filtro.
* A estratégia não cria nem gerencia objetos de gráfico. Visualização pode ser adicionada externamente vinculando dados level-1 a gráficos.

## Diferenças em relação ao EA original
* O temporizador de tick de baixo nível e elementos GUI da versão MQL são omitidos intencionalmente. A variante StockSharp depende de eventos level-1 e logging integrado.
* A lógica de modificação de ordens é simplificada: quando o preço alvo difere por mais de `ModifyThreshold` pips, a ordem é registrada novamente, em vez da lógica multirramos presente no EA.
* A detecção automática de comissão pelo histórico de operações é substituída por um parâmetro estático `CommissionInPips`; ainda assim, o filtro de risco adiciona esse valor ao spread antes da negociação.
* A versão StockSharp utiliza `StartProtection()` em vez de loops personalizados de monitoramento de stops.

## Testes históricos
A estratégia exige dados de cotação level-1 para reproduzir a lógica de disparo de ordens stop. Para simulações históricas, forneça séries bid/ask ou construa dados level-1 sintéticos a partir de histórico de ticks. Feeds apenas de candles são insuficientes porque ordens stop pendentes precisam reagir a mudanças de spread.
