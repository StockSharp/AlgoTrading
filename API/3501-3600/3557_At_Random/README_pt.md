# Na Estratégia Aleatória
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do consultor especialista MetaTrader 5 "Aleatoriamente" (MQL ID 39835). O bot original demonstra como um processo de decisão puramente aleatório se comporta quando é forçado a estar sempre no mercado. Cada barra completada desencadeia um lançamento de moeda que determina se a próxima ação é comprar ou vender. A versão StockSharp mantém a mesma ideia, mas a expressa com primitivas API de alto nível (`SubscribeCandles`, `BuyMarket`, `SellMarket`) e integra-se perfeitamente com Designer ou Runner.

A implementação evita intencionalmente take-profit, stop-loss ou trailing stops, espelhando o script de referência MQL. Serve, portanto, mais como um equipamento de teste ou um exemplo pedagógico do que como uma estratégia lucrativa.

## Lógica de negociação
1. Assine a série de velas configurada (`CandleType`). O intervalo padrão é de 15 minutos para imitar o comportamento do MetaTrader "período de tempo atual".
2. Assim que uma vela terminar, verifique se uma negociação anterior deve ser fechada. Quando `CloseBeforeReversal` está ativado, a estratégia nivela a posição e aguarda a confirmação de que não resta nenhuma exposição antes de emitir a próxima ordem.
3. Gere uma direção aleatória usando um gerador de números pseudoaleatórios. O parâmetro opcional `RandomSeed` permite sequências determinísticas para backtests reproduzíveis.
4. Envie uma ordem de mercado usando o `TradeVolume` fixo. As negociações longas e curtas são simétricas e não há ordens de proteção. O registro pode ser ativado via `LogSignals` para rastrear cada decisão aleatória.

Como cada vela desencadeia apenas uma decisão aleatória, a estratégia é plana ou carrega uma única posição a qualquer momento. As posições só são invertidas ou fechadas quando a próxima barra aparece.

## Gerenciamento de pedidos e riscos
- Todas as entradas e saídas são realizadas com `BuyMarket` / `SellMarket` usando o volume configurado. Não há limite ou ordens de parada.
- Se `CloseBeforeReversal` estiver desabilitado, a estratégia pode manter posições consecutivas: um novo sinal aleatório pode abrir imediatamente o lado oposto sem fechar explicitamente a negociação anterior primeiro.
- Nenhuma gestão de dinheiro ou proteção de conta é implementada. O objetivo da porta é reproduzir o comportamento do consultor especialista de referência para cenários de testes educacionais e de infraestrutura.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TradeVolume` | Tamanho base do pedido usado para cada entrada aleatória. Deve permanecer positivo. |
| `CloseBeforeReversal` | Força a estratégia a fechar a posição atual antes de realizar a próxima negociação aleatória. |
| `LogSignals` | Grava mensagens `AddInfoLog` sempre que uma direção aleatória é gerada. |
| `CandleType` | Período de tempo que produz a série de velas que impulsiona o lançamento aleatório da moeda. |
| `RandomSeed` | Valor inicial para o gerador de números pseudoaleatórios. Use `0` para confiar no relógio do sistema. |

## Notas de uso
- A porta mantém a ausência de níveis de take-profit e stop-loss, assim como a referência MQL. Qualquer controle de risco deve ser adicionado manualmente se a estratégia for utilizada para experimentos com capital real.
- As sementes determinísticas são úteis para criar conjuntos de dados reproduzíveis ao otimizar ou avaliar comportamento aleatório.
- A ativação do registro é recomendada durante os testes porque uma estratégia puramente aleatória oferece pouco feedback visual no gráfico.
