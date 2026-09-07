# Monitor de drawdown do patrimônio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama constrói uma curva de patrimônio a partir do P&L da estratégia, mantém um pico persistente, registra cada novo rompimento do limite de drawdown apenas uma vez e executa um ciclo comprado protegido deliberadamente espaçado para que os valores monitorados da conta mudem durante o backtest.

![schema](schema.svg)

## Visão geral da estratégia

- Candles BTCUSDT concluídos de cinco minutos fornecem o único relógio de amostragem. Uma assinatura Level 1 paralela do melhor preço de compra mantém ativa a avaliação do P&L não realizado entre as amostras de candles.
- P&L change atualiza armazenamentos silenciosos de P&L realizado e não realizado em seu próprio ritmo de eventos. Cada candle concluído libera uma vez os dois valores mais recentes e Start Balance para `Equity = Start Balance + Realized P&L + Unrealized P&L`.
- `max(pico armazenado, patrimônio)` mantém o pico de patrimônio de toda a execução. Highest(2), configurado para emitir apenas valores formados, confirma esse fluxo de pico monotônico e introduz uma amostra de aquecimento sem encurtar o histórico do pico.
- O drawdown é `(Peak - Equity) / Peak * 100`. Comparison verifica `Drawdown >= Drawdown Alert`, enquanto Crossing e um armazenamento booleano encaminham ao log apenas um novo cruzamento ascendente do limite.
- Modify position abre uma posição comprada a mercado quando a posição está zerada. A proteção com distâncias absolutas a fecha, e um temporizador de 1.440 candles permite a próxima entrada somente após cinco dias de candles subsequentes de cinco minutos.

## Regras de entrada e saída

- **Entrada comprada**: depois que Highest(2) estiver formado, um Flag de entrada disponível envia Volume 1 a um bloco Modify position com Buy, OpenPosition e MarketOrder. Assim, a primeira tentativa de entrada ocorre no segundo candle concluído.
- **Entrada vendida**: o diagrama não abre posições vendidas. As execuções Sell são saídas protetoras da posição comprada.
- **Saída**: Position protection envia uma saída a mercado após um movimento favorável de 0.04 ou desfavorável de 0.03 em unidades absolutas de preço. A execução da entrada inicia o intervalo de 1.440 candles subsequentes; o temporizador, e não a execução da saída, redefine a disponibilidade de entrada.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| BTC Data Security | BTCUSDT@BNBFT | Instrumento usado pelas assinaturas Candles, Level 1 e Strategy trades. Defina Strategy Security para o mesmo instrumento nas transações e no P&L. |
| Candle Series | 00:05:00 | Intervalo de candles concluídos e relógio das amostras de patrimônio e etapas do intervalo. |
| Start Balance | 1000 | Valor-base adicionado ao P&L realizado e não realizado ao calcular o patrimônio. |
| Peak Confirmation Length | 2 | Comprimento de Highest sobre o fluxo de pico persistente monotônico; adia a negociação até a segunda amostra. |
| Drawdown Alert, % | 1 | Um registro é gravado quando o drawdown alcança ou ultrapassa esse nível vindo de baixo. |
| Volume | 1 | Quantidade de cada entrada comprada. |
| Entry Cooldown N | 1440 | Número de candles concluídos subsequentes de cinco minutos entre entradas permitidas, equivalente a cinco dias. |
| Take Distance | 0.04 | Distância absoluta favorável do preço que fecha a posição comprada a mercado. |
| Stop Distance | 0.03 | Distância absoluta desfavorável do preço que fecha a posição comprada a mercado. |

## Detalhes do diagrama

- A [Variable](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) BTC alimenta [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) concluídos, [Level 1](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html) do melhor preço de compra e [Strategy trades](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html). Os blocos de negociação usam Strategy Security e Strategy Portfolio.
- Armazenamentos Variable silenciosos separam o fluxo orientado a eventos de [P&L change](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) do relógio de candles. A ordem fixa de liberação fornece a cada [Formula](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/formula.html) um conjunto completo de entradas do mesmo candle.
- O pico persistente começa em zero, portanto alterações em Start Balance continuam válidas. A Formula max atualiza esse estado antes que sua saída monotônica entre no [Indicator](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) Highest(2) formado.
- [Comparison](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) e [Crossing](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) recebem o limite e o drawdown em uma ordem fixa. Um cruzamento false na recuperação é ignorado; um cruzamento ascendente true libera a porcentagem armazenada por [String Formatter](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) para uma [Notification](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) Log.
- O bloco de intervalo [Delay](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) consome cada candle antes de qualquer decisão do mesmo candle. Uma execução Buy ativa N = 1440, e sua saída redefine o Flag de entrada antes que o candle elegível alcance Highest.
- A execução Buy de [Modify position](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) inicializa a [Position protection](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) local. O gráfico mostra candles, patrimônio amostrado, pico, drawdown, ambos os componentes de P&L, execuções de entrada, execuções protetoras e todas as execuções da estratégia.

## Uso

Importe o arquivo `.json` no Designer, defina Strategy Security como BTCUSDT@BNBFT e execute-o sobre o histórico de março incluído. Antes de usar o diagrama em negociação real, verifique para seu instrumento a escala do patrimônio, as distâncias absolutas de proteção, a porcentagem do alerta e o intervalo de cinco dias.
