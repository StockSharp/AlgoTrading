# Estratégia BlauTvi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia converte o especialista MQL5 `Exp_BlauTVI` em uma estratégia de alto nível do StockSharp. Ela usa o **Blau True Volume Index (TVI)** para detectar reversões no fluxo de volume de ticks.

## Ideia

O True Volume Index separa os up-ticks e down-ticks e os suaviza com três médias móveis exponenciais. O valor final oscila entre -100 e +100 e representa o domínio de compradores ou vendedores. A estratégia abre uma posição comprada quando o indicador vira para cima após um declínio e abre uma posição vendida quando o indicador vira para baixo após uma subida. As posições existentes na direção oposta são fechadas.

## Parâmetros

- `Length1` – primeiro período de suavização para up-ticks e down-ticks.
- `Length2` – segundo período de suavização.
- `Length3` – período de suavização final aplicado ao TVI.
- `CandleType` – tipo de vela utilizado para os cálculos (padrão: período de 4 horas).
- `Allow Buy Open` – habilitar abertura de posições compradas.
- `Allow Sell Open` – habilitar abertura de posições vendidas.
- `Allow Buy Close` – habilitar fechamento de posições compradas quando aparece sinal de venda.
- `Allow Sell Close` – habilitar fechamento de posições vendidas quando aparece sinal de compra.
- `Enable Stop Loss` – usar proteção de stop-loss em pontos.
- `Stop Loss` – valor do stop-loss em pontos.
- `Enable Take Profit` – usar proteção de take-profit em pontos.
- `Take Profit` – valor do take-profit em pontos.
- `Volume` – volume da ordem em lotes.

## Sinais

1. **Compra** – quando o valor anterior do TVI é menor que o anterior a ele e o valor atual do TVI é maior que o anterior. Se habilitado, as posições vendidas existentes são fechadas.
2. **Venda** – quando o valor anterior do TVI é maior que o anterior a ele e o valor atual do TVI é menor que o anterior. Se habilitado, as posições compradas existentes são fechadas.

Apenas velas finalizadas são processadas e todos os cálculos usam o volume de ticks da vela. Stop-loss e take-profit são opcionais e expressos em pontos de preço.

## Notas

A estratégia usa a API de alto nível: assina velas, calcula o indicador internamente com instâncias de `ExponentialMovingAverage` e gerencia posições com os métodos `BuyMarket` e `SellMarket`. O gráfico mostra o indicador TVI junto com as operações executadas pela estratégia.
