# Estratégia Color Coppock
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Color Coppock Strategy** implementa um sistema de negociação baseado em um oscilador Coppock modificado. O oscilador soma dois valores de Rate of Change (ROC) e suaviza o resultado com uma média móvel. O momentum crescente gera sinais de compra, enquanto o momentum decrescente gera sinais de venda.

## Como Funciona

1. Calcular dois valores ROC com diferentes períodos.
2. Somar ambos os valores ROC e aplicar uma Média Móvel Simples para suavização.
3. Comparar o valor atual do oscilador com os dois valores anteriores:
   - Se o oscilador vira para cima após declinar, a estratégia entra em uma posição comprada ou fecha a posição vendida existente.
   - Se o oscilador vira para baixo após subir, a estratégia entra em uma posição vendida ou fecha a posição comprada existente.
4. O volume da posição é obtido da propriedade `Volume` da estratégia.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `Roc1Period` | Período para o primeiro cálculo ROC. |
| `Roc2Period` | Período para o segundo cálculo ROC. |
| `SmoothingPeriod` | Período SMA aplicado à soma de ambos os valores ROC. |
| `CandleType` | Tipo de vela utilizado para os cálculos do indicador. |

## Uso

1. Anexar a estratégia a um ativo e definir os parâmetros desejados.
2. A estratégia subscreve as velas especificadas e processa apenas velas finalizadas.
3. As operações são executadas com ordens a mercado usando o volume padrão.

## Notas

- A estratégia utiliza apenas chamadas de API de alto nível como `SubscribeCandles` e auxiliares de ordens a mercado.
- Todos os comentários dentro do código estão escritos em inglês.
