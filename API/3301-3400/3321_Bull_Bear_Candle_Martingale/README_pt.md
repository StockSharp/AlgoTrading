# Estratégia Bull & Bear Candle Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia reage a candles altistas e baixistas fortes e abre posições a mercado na mesma direção. Ela usa uma sequência martingale independente para cada lado: posições compradas escalam o volume com o *Bull Multiplier*, enquanto posições vendidas usam o *Bear Multiplier*. Distâncias protetoras de stop-loss e take-profit também são configuradas separadamente para cada direção, permitindo controle preciso sobre o comportamento assimétrico exposto pelo expert advisor MQL original.

## Lógica de negociação
1. Assinar o tipo de candle configurado (padrão: 1 minuto) e aguardar apenas candles concluídos.
2. Quando não há posição aberta:
   - **Setup altista:** se `Close > Open` e o tamanho do corpo do candle excede o filtro de corpo altista, comprar a mercado.
   - **Setup baixista:** se `Close < Open` e o tamanho do corpo excede o filtro de corpo baixista, vender a mercado.
3. Cada entrada define ordens de stop-loss e take-profit convertidas de distâncias em pips para o passo de preço do instrumento.
4. Quando uma posição fecha, o PnL realizado é comparado com a linha de base anterior:
   - Um resultado negativo multiplica o volume martingale respectivo.
   - Um resultado positivo ou break-even redefine esse lado para o volume inicial.
5. Novos sinais são ignorados enquanto uma posição está aberta, reproduzindo o comportamento de operação única do EA de origem.

## Gestão monetária
- Ciclos martingale longos e curtos são acompanhados independentemente, então uma sequência perdedora comprada não afeta a próxima venda, e vice-versa.
- Volumes são alinhados ao `VolumeStep` do ativo para evitar rejeições de ordens.
- `StartProtection(useMarketOrders: true)` habilita o tratamento de ordens protetoras do StockSharp para os níveis stop e take anexados.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| **Initial Volume** | Volume base que inicia cada ciclo martingale para ambas as direções. |
| **Bull Multiplier** | Multiplicador aplicado à próxima operação altista após uma posição comprada perdedora. |
| **Bear Multiplier** | Multiplicador aplicado à próxima operação baixista após uma posição vendida perdedora. |
| **Bull Stop Loss** | Distância do stop-loss em pips para operações altistas. Convertida para preço usando o passo do instrumento. |
| **Bull Take Profit** | Distância do take-profit em pips para operações altistas. |
| **Bear Stop Loss** | Distância do stop-loss em pips para operações baixistas. |
| **Bear Take Profit** | Distância do take-profit em pips para operações baixistas. |
| **Bull Body Filter** | Corpo mínimo de candle altista em pips exigido para disparar uma compra. |
| **Bear Body Filter** | Corpo mínimo de candle baixista em pips exigido para disparar uma venda. |
| **Candle Type** | Timeframe usado para geração de sinais (padrão: timeframe de 1 minuto). |

## Notas de uso
- Garanta que o ativo conectado exponha valores válidos de `PriceStep` e `VolumeStep`. A estratégia usa 0.0001 por padrão quando `PriceStep` não é fornecido.
- A lógica martingale depende de PnL realizado, então o fechamento manual de posição ainda atualizará a sequência corretamente.
- A otimização pode focar em filtros de corpo e combinações de multiplicadores para equilibrar responsividade contra drawdown.
