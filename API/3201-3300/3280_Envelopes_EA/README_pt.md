# Estratégia Envelopes EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia replica o expert advisor do MetaTrader 4 "EnvelopesEA". Ela aplica um envelope de média móvel exponencial ao fluxo principal de candles e negocia reversões à média. Quando o mercado se afasta muito para fora do envelope, uma ordem a mercado contrária é enviada. As posições são fechadas assim que o preço volta a entrar no envelope na direção oposta. O expert original foi testado em EUR/USD em 2019; a versão StockSharp mantém a mesma lógica e expõe todas as entradas principais como parâmetros otimizáveis.

## Lógica de negociação
1. Calcule uma média móvel exponencial (EMA) de comprimento `EnvelopePeriod` nos candles selecionados.
2. Construa um envelope superior e inferior expandindo a EMA com `UpperDeviationPercent` e `LowerDeviationPercent`, respectivamente.
3. Aplique um buffer adicional de entrada definido por `EntryOffsetPoints` (multiplicado pelo passo de preço do instrumento) para evitar operações prematuras.
4. Quando não há posição aberta:
   - Entre comprado se o preço de fechamento cair abaixo do envelope inferior menos o buffer de entrada.
   - Entre vendido se o preço de fechamento subir acima do envelope superior mais o buffer de entrada.
5. Quando existe uma posição:
   - Feche posições compradas quando o preço de fechamento cruzar de volta acima do envelope superior.
   - Feche posições vendidas quando o preço de fechamento cruzar de volta abaixo do envelope inferior.

A estratégia sempre mantém no máximo uma posição aberta e usa ordens a mercado para entradas e saídas.

## Gestão de dinheiro
O volume da ordem é especificado diretamente pelo parâmetro `Volume` (lotes). Não há regras automáticas de martingale ou pirâmide, mantendo o comportamento idêntico à implementação MQ4 mais recente, em que recursos de escalonamento estavam desabilitados por padrão.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `Volume` | Volume da ordem em lotes. | 0.2 |
| `EnvelopePeriod` | Comprimento da EMA que forma a base do envelope. | 50 |
| `UpperDeviationPercent` | Desvio percentual aplicado à banda superior. | 0.5 |
| `LowerDeviationPercent` | Desvio percentual aplicado à banda inferior. | 0.5 |
| `EntryOffsetPoints` | Distância extra, em passos de preço, que o preço deve percorrer além da banda antes da entrada. | 100 |
| `CandleType` | Período usado para candles e cálculos de indicadores. | Candles de 30 minutos |

Todos os parâmetros numéricos (exceto `CandleType`) são marcados como otimizáveis para ajudar a reproduzir os fluxos de otimização originais.

## Observações
- O envelope usa uma EMA em vez da SMA de versões anteriores porque o script MQ4 evoluiu para uma base exponencial na iteração mais recente. Isso oferece reação mais rápida às oscilações de preço e melhora o timing de reversão à média.
- O buffer de entrada é multiplicado pelo `PriceStep` do instrumento. Garanta que os metadados do ativo contenham um tamanho de passo válido; caso contrário, a estratégia usa o padrão conservador `0.0001`.
- A visualização do gráfico inclui candles de preço, o envelope EMA e as operações da estratégia, facilitando validar o comportamento dos sinais contra o Expert Advisor original.
