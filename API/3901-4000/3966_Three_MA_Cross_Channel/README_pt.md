# Estratégia de três canais cruzados MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de três canais cruzados de MA** converte o MetaTrader Expert Advisor `3MaCross_EA` no StockSharp API de alto nível. Ele monitora três médias móveis configuráveis ​​e abre negociações quando as médias mais rápidas cruzam as mais lentas. Um canal de preço Donchian é usado opcionalmente para gerenciar saídas, imitando de perto o EA original que referenciava o indicador "Canal de preço".

## Lógica de negociação
- **Entrada longa**: gerada quando as médias móveis rápida e média fecham acima da média móvel lenta e qualquer uma das duas médias mais rápidas cruza acima da lenta na barra atual.
- **Entrada curta**: acionada quando as médias móveis rápida e média fecham abaixo da média móvel lenta e qualquer uma das duas médias mais rápidas cruza abaixo da média lenta.
- **Posição de saída**:
  - Sinal de cruzamento oposto.
  - Parada opcional do canal Donchian: as posições longas fecham se o preço cair abaixo da banda inferior; as posições curtas fecham se o preço subir acima da banda superior.
  - Distâncias fixas opcionais de take-profit ou stop-loss medidas em unidades de preço absoluto.

A estratégia sempre espera pelas velas concluídas, correspondendo ao comportamento `TradeAtCloseBar` do script original. Apenas uma posição direcional é mantida por vez; quando aparece um sinal contra uma posição existente, a negociação atual é fechada antes que uma nova seja aberta.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
|------|------|---------|-------------|
| `FastLength` | `int` | `2` | Lookback para a média móvel rápida. |
| `MediumLength` | `int` | `4` | Lookback para a média móvel média. |
| `SlowLength` | `int` | `30` | Lookback para a média móvel lenta. |
| `ChannelLength` | `int` | `15` | Janela de canal Donchian usada para saídas baseadas em canal. |
| `FastType` | `MovingAverageTypeEnum` | `EMA` | Algoritmo de média móvel aplicado à média rápida (SMA, EMA, SMMA, WMA). |
| `MediumType` | `MovingAverageTypeEnum` | `EMA` | Algoritmo de média móvel aplicado à média média. |
| `SlowType` | `MovingAverageTypeEnum` | `EMA` | Algoritmo de média móvel aplicado à média lenta. |
| `TakeProfit` | `decimal` | `0` | Meta de lucro em unidades de preço absoluto. Defina como `0` para desativar. |
| `StopLoss` | `decimal` | `0` | Limite de perda em unidades de preço absoluto. Defina como `0` para desativar. |
| `UseChannelStop` | `bool` | `true` | Ativa Donchian saídas de canal. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Tipo de vela usado para cálculos. |

## Notas
- Todas as médias móveis usam preços de fechamento e podem ser configuradas individualmente para corresponder às opções `FasterMode`, `MediumMode` e `SlowerMode` originais do EA.
- `TakeProfit` e `StopLoss` usam distâncias de preço absolutas (por exemplo, `0.0010` corresponde a 10 pips em um símbolo Forex de 5 dígitos). Eles são avaliados no fechamento de velas, replicando o gerenciamento de fechamento de barra do EA.
- Quando `UseChannelStop` está ativado, a estratégia reproduz o comportamento automático de stop-loss que dependia do indicador personalizado `Price Channel`.
- A estratégia desenha as três médias móveis, o canal Donchian e os marcadores comerciais no gráfico para confirmação visual.
