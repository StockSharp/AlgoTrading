# Estratégia de Exp Cronex AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o consultor especialista do MetaTrader **Exp_CronexAO** para a API de alto nível do StockSharp. O robô original opera cruzamentos entre as duas linhas do Cronex Awesome Oscillator (AO). A versão StockSharp subscreve uma série de velas configurável, calcula o AO, suaviza-o duas vezes com médias móveis para reproduzir as linhas Cronex, e abre ou fecha posições quando a linha rápida cruza a linha lenta.

## Lógica de trading

1. Construir o Awesome Oscillator a partir das velas selecionadas.
2. Suavizar o oscilador duas vezes com médias móveis simples. A primeira suavização cria a linha Cronex "rápida", a segunda suavização produz a linha "sinal".
3. Olhar `SignalBar` velas concluídas para trás e comparar as linhas Cronex nessa barra e na anterior.
4. Um sinal de **compra** aparece quando a linha rápida está acima da linha lenta e fez um cruzamento ascendente na barra de retrocesso. A estratégia opcionalmente fecha qualquer posição vendida e, se permitido, abre uma ordem de compra a mercado.
5. Um sinal de **venda** espelha a regra anterior: a linha rápida deve estar abaixo da linha lenta e deve ter cruzado para baixo na barra de retrocesso. A estratégia opcionalmente fecha qualquer posição comprada e, se permitido, abre uma ordem de venda a mercado.
6. Os níveis de stop-loss e take-profit, expressos em pontos do instrumento, são anexados à posição resultante sempre que uma nova operação é aberta.

Apenas uma posição líquida é mantida. Quando a direção muda, a estratégia combina o volume necessário para fechar a posição oposta com o novo volume de trade para emular o modo de netting do MetaTrader.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Tipo de dados das velas usadas para os cálculos de Cronex AO. O padrão é um período de 8 horas. |
| `FastPeriod` | Comprimento da primeira suavização aplicada ao Awesome Oscillator. |
| `SlowPeriod` | Comprimento da segunda suavização aplicada à linha rápida. |
| `SignalBar` | Número de barras concluídas para trás que devem conter o sinal de cruzamento. A estratégia também inspeciona a barra seguinte para confirmar a direção. |
| `BuyOpenEnabled` / `SellOpenEnabled` | Habilitar ou desabilitar a abertura de posições compradas ou vendidas. |
| `BuyCloseEnabled` / `SellCloseEnabled` | Controlar se posições opostas podem ser fechadas quando um sinal inverso aparece. |
| `TakeProfit` | Alvo de lucro em pontos, aplicado após cada nova entrada se maior que zero. |
| `StopLoss` | Stop protetor em pontos, também aplicado após cada nova entrada se maior que zero. |

## Gestão de risco

As distâncias de stop-loss e take-profit imitam as entradas baseadas em pontos da versão MetaTrader. Elas são recalculadas cada vez que uma nova operação é enviada para que as ordens protetoras sempre correspondam ao tamanho atual da posição líquida.

## Diferenças da versão MetaTrader

- A implementação StockSharp usa médias móveis simples para ambos os estágios de suavização Cronex. A implementação XMA original permite vários métodos de suavização, mas a configuração padrão corresponde à média simples que é reproduzida aqui.
- Rotinas de deslizamento e gestão monetária da biblioteca `TradeAlgorithms` não são replicadas. O dimensionamento de posição é controlado via a propriedade padrão `Volume`.
- A execução de operações depende do comportamento de netting do StockSharp. Quando a direção é revertida, uma única ordem a mercado é emitida com volume suficiente para achatar e inverter a posição em uma etapa, refletindo a lógica de conta de netting do MT5.
