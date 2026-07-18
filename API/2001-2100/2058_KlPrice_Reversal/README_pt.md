# Estratégia de Reversão KlPrice
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão em C# do especialista MQL5 original **exp_i-KlPrice.mq5**. Implementa um sistema de reversão baseado em um oscilador de preço normalizado. O oscilador compara o preço atual com uma banda de preço suavizada derivada de uma média móvel e o intervalo verdadeiro médio (ATR). Cruzar os limites predefinidos gera sinais de trading.

## Como Funciona

1. Uma média móvel simples (SMA) suaviza o preço de fechamento.
2. Um Intervalo Verdadeiro Médio (ATR) estima a volatilidade do mercado.
3. O oscilador é calculado como:
   
   `jres = 100 * (Close - (SMA - ATR)) / (2 * ATR) - 50`
4. O valor do oscilador é mapeado para cinco zonas de cor:
   - **4** – acima do nível superior
   - **3** – entre zero e o nível superior
   - **2** – entre os níveis superior e inferior
   - **1** – entre o nível inferior e zero
   - **0** – abaixo do nível inferior
5. Uma posição comprada é aberta quando o oscilador sai da zona 4. Uma posição vendida é aberta quando sai da zona 0. As posições existentes fecham quando o oscilador cruza zero.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `CandleType` | Período para dados de preço. |
| `PriceMaLength` | Período de SMA para suavização de preço. |
| `AtrLength` | Período de ATR para calcular a banda de preço. |
| `UpLevel` | Limiar superior do oscilador. |
| `DownLevel` | Limiar inferior do oscilador. |
| `EnableBuy` | Permitir abertura de posições compradas. |
| `EnableSell` | Permitir abertura de posições vendidas. |

## Uso

1. Criar uma instância de `KlPriceReversalStrategy`.
2. Definir os parâmetros desejados.
3. Anexar a estratégia a um portfólio e ativo.
4. Iniciar a estratégia para receber sinais e colocar ordens.

A estratégia usa ordens a mercado via `BuyMarket` e `SellMarket`. A proteção de posição é ativada através de `StartProtection()`.

## Notas

- A implementação aproxima o indicador MQL original usando indicadores integrados do StockSharp (`SimpleMovingAverage` e `AverageTrueRange`).
- Todos os cálculos são realizados apenas em velas concluídas.
