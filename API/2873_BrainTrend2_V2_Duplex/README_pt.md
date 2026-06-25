# Estratégia BrainTrend2 V2 Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia BrainTrend2 V2 Duplex é um port de alto nível para StockSharp do especialista MetaTrader 5 original `Exp_BrainTrend2_V2_Duplex`. Ela executa duas instâncias independentes do indicador BrainTrend2 V2: uma dedicada a oportunidades compradas e outra ajustada para oportunidades vendidas. Cada lado pode operar em sua própria série de velas, comprimento ATR e deslocamento de sinal, permitindo que a estratégia misture confirmações multi-período ou configurações de risco assimétricas.

BrainTrend2 V2 é um motor de seguimento de tendência que constrói um canal dinâmico de "rio" comparando o range verdadeiro mais recente com uma média ATR ponderada. O indicador pinta as velas com cinco cores distintas:

- **0** – Vela altista dentro de um rio de tendência de alta.
- **1** – Vela baixista dentro de um rio de tendência de alta.
- **2** – Marcador neutro enquanto o rio muda de direção.
- **3** – Vela altista dentro de um rio de tendência de baixa.
- **4** – Vela baixista dentro de um rio de tendência de baixa.

A estratégia duplex interpreta essas transições de cor para acionar entradas e saídas, espelhando de perto as regras codificadas no especialista MQL5.

## Lógica de operação
### Lado comprado
- Avaliar o indicador na vela localizada `Long Signal Bar` passos atrás (padrão 1 = a barra terminada anterior).
- Abrir uma posição comprada quando:
  - A cor na barra `SignalBar + 1` (duas barras atrás) era **menor que 2** (tons verdes de um rio de tendência de alta), **e**
  - A cor na barra `SignalBar` é **maior que 1** (transição para fora do estado puramente altista).
- Fechar uma posição comprada existente quando a cor na barra `SignalBar + 1` é **maior que 2** (tons magenta produzidos pelo rio de tendência de baixa).

### Lado vendido
- Avaliar o indicador na vela localizada `Short Signal Bar` passos atrás (padrão 1).
- Abrir uma posição vendida quando:
  - A cor na barra `SignalBar + 1` era **maior que 2** (tons magenta), **e**
  - A cor na barra `SignalBar` é **maior que 0** (qualquer coisa exceto uma vela puramente altista).
- Fechar uma posição vendida existente quando a cor na barra `SignalBar + 1` é **menor que 2** (retorno ao rio de tendência de alta).

Quando uma nova ordem é emitida, a estratégia automaticamente compensa qualquer exposição oposta. Por exemplo, uma solicitação de entrada vendida primeiro recomprará a posição comprada atual (se houver) e então enviará a ordem de venda pelo volume vendido configurado.

## Gestão de risco
- Ambos os lados podem especificar distâncias independentes de stop-loss e take-profit em pontos. Um valor de `0` desabilita o bracket respectivo.
- Stops e metas são calculados em preços absolutos usando o passo de preço do instrumento. Comprados monitoram a mínima/máxima da vela, vendidos monitoram a máxima/mínima para emular a execução intrabarra.
- O tamanho da posição é expresso diretamente em unidades de negociação e pode diferir entre os fluxos comprado e vendido.
- A estratégia também habilita `StartProtection()` para integração com quaisquer salvaguardas em nível de portfólio configuradas dentro do StockSharp.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `Long Candle Type` | Tipo de dado de vela usado para o indicador comprado (período). | Período H4 |
| `Long ATR Period` | Lookback ATR usado no cálculo do BrainTrend2 V2 para o fluxo comprado. | 7 |
| `Long Signal Bar` | Deslocamento histórico (em barras) avaliado para decisões compradas. | 1 |
| `Enable Long Entries` | Permite ou bloqueia novas ordens compradas. | true |
| `Enable Long Exits` | Permite ou bloqueia saídas compradas geradas pelo indicador. | true |
| `Long Volume` | Tamanho de ordem base para entradas compradas. | 1 |
| `Long Stop Loss` | Distância de stop-loss em pontos para operações compradas (0 = desabilitado). | 1000 |
| `Long Take Profit` | Distância de take-profit em pontos para operações compradas (0 = desabilitado). | 2000 |
| `Short Candle Type` | Tipo de dado de vela usado para o indicador vendido. | Período H4 |
| `Short ATR Period` | Lookback ATR usado no cálculo do BrainTrend2 V2 para o fluxo vendido. | 7 |
| `Short Signal Bar` | Deslocamento histórico (em barras) avaliado para decisões vendidas. | 1 |
| `Enable Short Entries` | Permite ou bloqueia novas ordens vendidas. | true |
| `Enable Short Exits` | Permite ou bloqueia saídas vendidas geradas pelo indicador. | true |
| `Short Volume` | Tamanho de ordem base para entradas vendidas. | 1 |
| `Short Stop Loss` | Distância de stop-loss em pontos para operações vendidas (0 = desabilitado). | 1000 |
| `Short Take Profit` | Distância de take-profit em pontos para operações vendidas (0 = desabilitado). | 2000 |

## Notas de uso
- Use deslocamentos de sinal maiores para aguardar confirmação adicional de velas ou combine diferentes períodos atribuindo tipos de velas distintos aos fluxos comprado e vendido.
- Como a estratégia usa uma implementação personalizada do BrainTrend2, ela não depende de nenhum arquivo de indicador externo; tudo está contido na classe C#.
- Stops e metas são gerenciados em cada vela terminada. Ao executar com dados ao vivo, considere usar um intervalo de vela suficientemente pequeno se precisar de controle de risco mais rigoroso.
- Definir tanto as distâncias de stop quanto de take-profit como zero mantém as posições abertas até que um gatilho de cor oposta apareça.
- O buffer do indicador é inicializado assim que suficientes velas iguais ao período ATR foram processadas. Até esse momento, nenhuma decisão de operação é tomada.
