# Estratégia JBrainTrend ReOpen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma implementação em C# inspirada no exemplo MQL5 "JBrainTrend1Stop_ReOpen".  
Ela usa o oscilador Estocástico para determinar condições de sobrecompra e sobrevenda e suporta piramidagem reabrindo posições quando o preço avança um passo especificado.

## Lógica
- Subscrever candles do período selecionado.
- Calcular o oscilador Estocástico (%K e %D).
- Entrar comprado quando %K cai abaixo de 20 e vendido quando %K sobe acima de 80.
- As posições são fechadas quando o extremo oposto é atingido.
- Após uma entrada, posições adicionais são adicionadas se o preço mover `PriceStep` na direção da operação, até `MaxPositions`.
- Stop-loss e take-profit de proteção são aplicados em unidades absolutas de preço.

## Parâmetros
- `StochPeriod` – período principal do oscilador Estocástico.
- `KPeriod` / `DPeriod` – períodos de suavização para as linhas %K e %D.
- `CandleType` – período usado para análise.
- `StopLoss` – distância do stop-loss em unidades de preço.
- `TakeProfit` – distância do take-profit em unidades de preço.
- `PriceStep` – movimento de preço necessário para reabrir uma posição.
- `MaxPositions` – número máximo de entradas em uma direção.
- `BuyEnabled` / `SellEnabled` – habilitar ou desabilitar operações compradas/vendidas.

## Notas
O script MQL5 original usava um indicador personalizado chamado *JBrainTrend1Stop*.  
Este port em C# aproxima o conceito de negociação com indicadores embutidos do StockSharp para facilitar a integração.
