# Estratégia Cronex AC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Cronex AC recria o clássico consultor especialista Cronex Acceleration/Deceleration (AC) usando a API de alto nível do StockSharp. Ela suaviza o Oscilador Acelerador com duas médias móveis consecutivas e reage quando a linha rápida cruza a linha lenta. Cruzamentos altistas abrem posições compradas e fecham vendidas, enquanto cruzamentos baixistas abrem vendidas e fecham compradas.

## Lógica de trading

1. Construir valores do Oscilador Acelerador (AO-AC) a partir da série de velas selecionada.
2. Suavizar o AC com o tipo de média móvel escolhido duas vezes: a primeira suavização produz a linha "rápida" e a segunda suavização produz a linha "sinal".
3. Avaliar as duas linhas na barra definida pelo parâmetro `SignalBar`. A estratégia também olha uma barra mais atrás para confirmar um cruzamento.
4. Quando a linha rápida cruza acima da linha de sinal, a estratégia fecha posições vendidas existentes (se habilitado) e abre uma nova posição comprada (se habilitado).
5. Quando a linha rápida cruza abaixo da linha de sinal, a estratégia fecha posições compradas existentes (se habilitado) e abre uma nova posição vendida (se habilitado).
6. O tamanho da posição é igual ao `Volume` configurado mais o valor absoluto da posição atual, permitindo reversões em uma única ordem a mercado.

A lógica espelha o especialista MQL5 agindo apenas em velas completamente finalizadas e separando as permissões para entradas e saídas em ambas as direções.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `SmoothingType` | `CronexMovingAverageType` | `Simple` | Algoritmo de média móvel aplicado ao Oscilador Acelerador. Opções: Simple, Exponential, Smoothed, Weighted. |
| `FastPeriod` | `int` | `14` | Retrocesso da primeira suavização (linha rápida). |
| `SlowPeriod` | `int` | `25` | Retrocesso da segunda suavização (linha de sinal). |
| `SignalBar` | `int` | `1` | Número de barras finalizadas a olhar para trás ao ler o sinal. Um valor de 1 replica o comportamento padrão do Cronex. |
| `CandleType` | `DataType` | `TimeFrame(8h)` | Série de velas usada para cálculos. |
| `EnableLongEntry` | `bool` | `true` | Permitir abrir posições compradas após um cruzamento altista. |
| `EnableShortEntry` | `bool` | `true` | Permitir abrir posições vendidas após um cruzamento baixista. |
| `EnableLongExit` | `bool` | `true` | Permitir fechar posições compradas quando a linha rápida cai abaixo da linha lenta. |
| `EnableShortExit` | `bool` | `true` | Permitir fechar posições vendidas quando a linha rápida sobe acima da linha lenta. |
| `Volume` | `decimal` | padrão da estratégia | Tamanho de ordem usado para entradas. A estratégia adiciona automaticamente o valor absoluto da posição aberta para reverter em uma única operação. |

## Gráficos

Quando uma área de gráfico está disponível, a estratégia plota:

- velas fonte para o período selecionado,
- valores do Oscilador Acelerador,
- médias móveis rápida e de sinal,
- as próprias operações da estratégia para validação visual.

## Notas

- Todos os cálculos dependem de velas concluídas (`CandleStates.Finished`) para evitar repintagem.
- Os buffers de suavização mantêm exatamente valores históricos suficientes para avaliar o deslocamento `SignalBar` solicitado, correspondendo ao especialista MQL original.
- Recursos de gestão monetária da versão MQL (stop-loss, take-profit, desvio) são intencionalmente omitidos para que a gestão de posições possa ser tratada externamente através dos controles de risco do StockSharp.
