# Pausar a negociação na estratégia de perdas consecutivas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Pause Trading On Consecutive Loss** reproduz a lógica de controle de risco do consultor especialista MetaTrader 4 *"Pause Trading On Consecutive Loss"*. O script original monitorava as negociações fechadas mais recentes, contava quantas delas terminavam com lucro negativo e suspendia novos pedidos quando a seqüência de perdas ultrapassava um limite definido pelo usuário em um curto espaço de tempo. A porta StockSharp mantém esse comportamento enquanto o envolve em um modelo de entrada de impulso mínimo para que o mecanismo de pausa possa ser avaliado dentro da estratégia independente.

## Como funciona

1. A estratégia assina velas de prazo especificadas por `CandleType`. Sempre que chega uma vela finalizada, o preço de fechamento é comparado com o fechamento anterior. Se aumentou, a estratégia tenta uma entrada longa; se diminuiu, uma entrada curta é considerada. As posições saem sempre que uma posição de alta enfrenta uma vela de baixa (fechamento abaixo da abertura) ou uma posição de baixa enfrenta uma vela de alta (fechamento acima da abertura).
2. Após cada posição fechada, o lucro realizado da estratégia é inspecionado. Os resultados perdedores enfileiram seu carimbo de data/hora de fechamento em uma lista FIFO interna que armazena apenas perdas consecutivas. As saídas lucrativas ou de equilíbrio apagam a lista, assim como o loop MQL foi abortado ao encontrar um negócio sem perdas.
3. Quando a lista atinge `ConsecutiveLosses` itens, a estratégia verifica se a diferença de tempo entre a perda mais antiga e a mais recente está dentro de `WithinMinutes`. Se for, a negociação será pausada até `PauseMinutes` decorrer do último horário de fechamento. Durante a pausa, nenhuma nova ordem de mercado é enviada, mas o gerenciamento de posição existente continua operando para que a carteira possa se estabilizar naturalmente.
4. Assim que a pausa expirar, a lista de perdas será apagada e a negociação será retomada automaticamente. O comportamento imita as funções originais `CheckLastNLossDifference` e `lastOrderCloseTime` sem depender de uma verificação persistente do histórico de pedidos.

A implementação usa assinaturas de vela de alto nível de StockSharp (`SubscribeCandles`) e o gerenciador de PnL integrado para monitorar os lucros realizados. Uma fila simples (`Queue<DateTimeOffset>`) captura os carimbos de data e hora da seqüência de perdas, respeitando a proibição de travessia manual redundante do histórico.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | Período de 5 minutos | Agregação de velas usada para entradas de impulso simples. |
| `OrderVolume` | `0.1` | Volume (em lotes/contratos) enviado com cada ordem de entrada e saída. |
| `ConsecutiveLosses` | `3` | Número de posições perdedoras consecutivas necessárias antes que novas negociações sejam pausadas. |
| `WithinMinutes` | `20` | Número máximo de minutos permitidos entre a primeira e a última derrota da seqüência. Um valor zero desativa a verificação da janela. |
| `PauseMinutes` | `20` | Duração da suspensão da negociação após a detecção da sequência de perdas. |

## Notas

- A fila de carimbos de data/hora de perda só é preenchida quando a estratégia está plana e acaba de perceber uma perda. Fechamentos parciais ou negociações lucrativas não prolongam a sequência, evitando falsos positivos.
- O temporizador de pausa é avaliado em relação a cada vela finalizada. Se `PauseMinutes` decorrer enquanto a estratégia estiver ociosa, a próxima vela desbloqueia imediatamente a negociação.
- Como a versão StockSharp opera em uma posição de compensação, a diferença de PnL realizada é derivada de `PnLManager.RealizedPnL`, espelhando fielmente a pesquisa do histórico de MetaTrader sem reprocessar todo o registro do pedido.
