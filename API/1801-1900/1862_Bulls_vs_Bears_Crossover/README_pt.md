# Estratégia de Cruzamento Bulls vs Bears
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

Esta estratégia implementa um sistema de cruzamento baseado no indicador **Bulls vs Bears (BvsB)**. O indicador mede a distância entre os preços máximo e mínimo de um candle e uma média móvel. Quando a distância de alta cai abaixo da distância de baixa, indica pressão de alta se enfraquecendo, e a estratégia abre uma posição comprada. Por outro lado, quando a distância de alta sobe acima da distância de baixa, uma posição vendida é aberta. As posições existentes são fechadas no sinal oposto ou quando as metas de lucro ou perda são atingidas.

O tipo e o comprimento da média móvel são configuráveis, permitindo que a estratégia se adapte a diferentes mercados e períodos. O gerenciamento de risco é controlado por níveis fixos de stop-loss e take-profit expressos em passos de preço.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `MaType` | Método de cálculo da média móvel (SMA, EMA, SMMA, WMA). |
| `MaLength` | Período da média móvel. |
| `StopLoss` | Distância de stop-loss em passos de preço. |
| `TakeProfit` | Distância de take-profit em passos de preço. |
| `OpenLong` | Permitir abertura de posições compradas em cruzamento de alta. |
| `OpenShort` | Permitir abertura de posições vendidas em cruzamento de baixa. |
| `CloseLong` | Permitir fechamento de posições compradas em cruzamento de baixa. |
| `CloseShort` | Permitir fechamento de posições vendidas em cruzamento de alta. |
| `CandleType` | Período dos candles processados. |

## Como Funciona

1. Subscrever a série de candles especificada e calcular uma média móvel.
2. Para cada candle finalizado, calcular as distâncias de alta e baixa:
   - **Bull** = `(HighPrice - MA) / PriceStep`
   - **Bear** = `(MA - LowPrice) / PriceStep`
3. Detectar cruzamentos entre os valores Bull e Bear.
4. Abrir ou fechar posições de acordo com a direção do cruzamento e as opções habilitadas.
5. Gerenciar risco usando os níveis de stop-loss e take-profit configurados.

Esta abordagem simples, porém flexível, pode ser aplicada a muitos instrumentos para medir o equilíbrio entre forças de alta e de baixa.
