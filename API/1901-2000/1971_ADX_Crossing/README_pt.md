# Estratégia de Cruzamento ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Cruzamento ADX** é construída em torno do indicador Average Directional Index (ADX). Ela analisa o cruzamento do índice direcional positivo (+DI) e do índice direcional negativo (-DI) para identificar possíveis mudanças de tendência.

Quando +DI cruza acima de -DI, a estratégia considera um sinal de alta e pode abrir posições compradas enquanto opcionalmente fecha posições vendidas existentes. De forma inversa, quando +DI cruza abaixo de -DI, é tratado como um sinal de baixa, provocando entradas vendidas e o fechamento opcional de posições compradas. Níveis opcionais de stop-loss e take-profit são suportados por gestão de risco integrada.

## Indicador

A estratégia usa o indicador `AverageDirectionalIndex` do StockSharp. Apenas as linhas direcionais são necessárias; o valor principal do ADX não é usado na tomada de decisões.

## Parâmetros

- `ADX Period` – comprimento do cálculo do ADX. O padrão é `50`.
- `Candle Type` – período usado para a assinatura de candles. O padrão é `1 hora`.
- `Allow Buy Open` – habilitar a abertura de posições compradas. O padrão é `true`.
- `Allow Sell Open` – habilitar a abertura de posições vendidas. O padrão é `true`.
- `Allow Buy Close` – permitir fechar posições compradas no sinal de venda. O padrão é `true`.
- `Allow Sell Close` – permitir fechar posições vendidas no sinal de compra. O padrão é `true`.
- `Stop Loss` – distância do stop-loss em unidades de preço absolutas. O padrão é `1000`.
- `Take Profit` – distância do take-profit em unidades de preço absolutas. O padrão é `2000`.

## Lógica de trading

1. Assinar candles do período selecionado e calcular o indicador ADX.
2. Rastrear os valores anteriores de +DI e -DI para detectar cruzamentos.
3. Em um cruzamento de alta (+DI cruza acima de -DI):
   - Fechar posição vendida se `Allow Sell Close` estiver habilitado.
   - Abrir posição comprada se `Allow Buy Open` estiver habilitado.
4. Em um cruzamento de baixa (+DI cruza abaixo de -DI):
   - Fechar posição comprada se `Allow Buy Close` estiver habilitado.
   - Abrir posição vendida se `Allow Sell Open` estiver habilitado.
5. Níveis protetores de stop-loss e take-profit são aplicados usando `StartProtection`.

## Notas

- Apenas candles concluídos (`CandleStates.Finished`) são processados.
- A estratégia depende do gerenciamento de risco integrado do StockSharp para níveis de stop.
- As posições são fechadas enviando uma ordem de mercado oposta com o volume atual.

Esta estratégia destina-se a fins educacionais e pode requerer ajuste adicional antes de ser usada em mercados reais.
