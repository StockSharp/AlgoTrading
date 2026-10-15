# Estratégia de Exp Cronex MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia replica o consultor especialista **Exp_CronexMFI**. Ela suaviza o Índice de Fluxo de Dinheiro (MFI) duas vezes e opera **contra** o cruzamento das linhas resultantes. O porto mantém a filosofia contrária original enquanto expõe cada configuração como um parâmetro de estratégia do StockSharp.

## Como funciona
1. Subscrever a série de velas selecionada (o padrão é o período de 4 horas).
2. Calcular o Índice de Fluxo de Dinheiro com o período configurado.
3. Aplicar o método de suavização escolhido duas vezes: o primeiro passo produz a linha Cronex rápida, o segundo passo suaviza a linha rápida novamente para construir a linha lenta.
4. Armazenar pares históricos de valores rápidos e lentos com um atraso ajustável (`SignalShift`).
5. Quando a linha rápida cruza **para baixo** através da linha lenta, fechar posições vendidas (se permitido) e abrir/ampliar uma posição comprada. Quando a linha rápida cruza **para cima**, fechar posições compradas e abrir/ampliar uma posição vendida.
6. Ordens são enviadas com o `Volume` da estratégia e podem ser desabilitadas independentemente para os lados comprado e vendido.

A estratégia apenas avalia velas finalizadas para corresponder ao timing da implementação do MetaTrader.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `MfiPeriod` | `int` | `25` | Comprimento do Índice de Fluxo de Dinheiro. |
| `FastPeriod` | `int` | `14` | Período do primeiro estágio de suavização (linha Cronex rápida). |
| `SlowPeriod` | `int` | `25` | Período do segundo estágio de suavização (linha Cronex lenta). |
| `SignalShift` | `int` | `1` | Número de velas concluídas para atrasar o processamento de sinais, reproduzindo o comportamento `SignalBar` do MQL. |
| `Smoothing` | `SmoothingMethod` | `Simple` | Algoritmo de média móvel usado para ambos os estágios de suavização. |
| `EnableLongEntries` | `bool` | `true` | Habilita ordens a mercado que abrem ou adicionam a posições compradas. |
| `EnableShortEntries` | `bool` | `true` | Habilita ordens a mercado que abrem ou adicionam a posições vendidas. |
| `EnableLongExits` | `bool` | `true` | Permite que sinais fechem a exposição comprada existente. |
| `EnableShortExits` | `bool` | `true` | Permite que sinais fechem a exposição vendida existente. |
| `CandleType` | `DataType` | `TimeFrame(4h)` | Série de velas usada para cálculos de indicadores. |
| `Volume` | `decimal` | `1` | Tamanho de ordem usado ao abrir novas posições. |

## Opções de suavização
O indicador MQL original oferece vários modos de suavização proprietários. O porto StockSharp os mapeia para médias móveis integradas:

| Conceito MQL | Valor `SmoothingMethod` | Notas |
| --- | --- | --- |
| SMA | `Simple` | Média móvel simples. |
| EMA | `Exponential` | Média móvel exponencial. |
| SMMA | `Smoothed` | Média móvel suavizada (Wilder). |
| LWMA | `Weighted` | Média móvel ponderada linear. |
| JJMA / JurX / ParMA / T3 / VIDYA / AMA | `DoubleExponential`, `TripleExponential`, `Hull`, `ZeroLagExponential`, `ArnaudLegoux`, `KaufmanAdaptive` | Selecionar a aproximação mais próxima para suavização adaptativa. |

## Diferenças vs versão MQL
- Seleção de volume de tick/real do MQL não está disponível; velas do StockSharp fornecem dados de volume agregado.
- Gestão de operações depende exclusivamente de ordens a mercado. O auxiliar de gestão monetária original que atrasava a execução até a próxima barra é emulado através de `SignalShift`.
- O posicionamento de stop-loss e take-profit deve ser configurado externamente (por exemplo, via regras de risco ou módulos de proteção).

## Notas de uso
- Escolher uma série de velas que corresponda à liquidez do instrumento; o intervalo padrão de 4 horas reflete o EA fonte.
- Ajustar `SignalShift` se quiser confirmar um cruzamento com velas adicionais.
- Combinar a estratégia com regras de gestão de risco (ex.: `StartProtection`) para limitar perdas.
