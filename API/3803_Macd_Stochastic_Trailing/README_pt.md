# Estratégia de rastreamento Macd Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

# MACD Stochastic Estratégia de rastreamento

## Visão geral
- Convertido do MetaTrader 4 consultor especialista `MQL/7637/3_lccfpgubwykd__www_forex-instruments_info.mq4`.
- Usa um fluxo de trabalho de **três períodos de tempo**: velas horárias acionam ambos os filtros MACD, velas de 15 minutos fornecem osciladores Stochastic e velas de 1 minuto confirmam quebras de preços e gerenciam saídas finais.
- Implementa uma estratégia StockSharp de alto nível usando `SubscribeCandles(...).Bind(...)` / `BindEx(...)` sem pesquisa manual de dados.
- As posições são abertas com ordens de mercado e gerenciadas inteiramente dentro da estratégia (não foram necessárias alterações externas no equipamento de teste).

## Indicadores e Parâmetros
| Nome | Tipo | Padrão | Descrição |
| ---- | ---- | ------- | ----------- |
| `LongStopLoss` | `decimal` | `17` | Distância inicial de parada para negociações longas, expressa em pontos de instrumento. |
| `ShortStopLoss` | `decimal` | `40` | Distância inicial de parada para negociações curtas (pontos). |
| `LongTrailingStop` | `decimal` | `88` | Distância final para posições longas. |
| `ShortTrailingStop` | `decimal` | `76` | Distância final para posições curtas. |
| `OrderVolume` | `decimal` | `0.1` | Volume base de negociação (lotes) espelhado da entrada MQL. |
| `MacdCandleType` | `DataType` | `H1` | Prazo para os filtros MACD de alta e baixa (`22/27/9` e `19/77/9`). |
| `StochasticCandleType` | `DataType` | `M15` | Período usado para ambos os osciladores Stochastic (`5/3/11` e `9/3/19`). |
| `EntryCandleType` | `DataType` | `M1` | Prazo que fornece confirmação de rompimento e lógica de rastreamento. |

Todas as configurações baseadas em pontos são convertidas em preços absolutos por meio do instrumento `PriceStep`, reproduzindo fielmente o multiplicador MetaTrader `Point`.

## Regras de negociação
### Entrada longa
1. A linha principal horária MACD(22,27,9) cruza acima de seu valor anterior, mas permanece abaixo de zero.
2. M15 Stochastic(%K=5,%D=3,slowing=11) está abaixo de 26 e subindo em relação ao seu valor anterior.
3. O fechamento atual do M1 perfura a máxima anterior do M1.
4. Quando todas as condições se alinham e nenhuma posição está aberta, a estratégia compra `OrderVolume` mais qualquer quantidade necessária para inverter uma posição vendida existente.

### Entrada curta
1. A linha principal horária MACD(19,77,9) fica abaixo do valor anterior, com o valor anterior acima de zero.
2. M15 Stochastic(%K=9,%D=3,slowing=19) está acima de 70.
3. O fechamento atual do M1 quebra abaixo da mínima anterior do M1.
4. Uma posição curta é aberta com a mesma lógica de inversão de posição do EA original.

### Sair e seguir
- As paradas iniciais refletem as distâncias MQL `StopLoss` em pontos.
- Os trailing stops são ativados quando o preço se move mais do que a distância final especificada em favor da posição e são recalculados em cada vela M1 finalizada.
- Se o preço atingir o nível de stop ativo (inicial ou de trilha), a posição será fechada com uma ordem de mercado.

## Notas de implementação
- As assinaturas de velas são divididas por período de tempo para que as atualizações dos indicadores permaneçam independentes, correspondendo exatamente ao comportamento de vários períodos de tempo do EA.
- As comparações finais de MQL `Bid`/`Ask` são aproximadas com os máximos/mínimos da vela M1 finalizados, que é a representação mais próxima dentro do alto nível baseado em vela API.
- O código segue as diretrizes do repositório: recuo de tabulação, namespace `StockSharp.Samples.Strategies`, comentários em inglês e declarações de parâmetros dentro do construtor via `Param(...)`.
