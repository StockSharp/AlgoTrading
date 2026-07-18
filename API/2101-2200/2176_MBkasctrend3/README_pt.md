# Estratégia MBKAsctrend3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia MBKAsctrend3 utiliza três osciladores Williams %R com diferentes períodos. Sua combinação ponderada define a tendência do mercado. Uma posição comprada é aberta quando o valor ponderado cruza acima de um limiar superior e o oscilador de longo prazo também está alto. Uma posição vendida é aberta quando os valores caem abaixo de seus limiares inferiores. As posições são protegidas por níveis configuráveis de stop-loss e take-profit expressos em pontos.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Weighted WPR > 67+Swing e long WPR > 50-AverageSwing.
  - **Vendido**: Weighted WPR < 33-Swing e long WPR < 50+AverageSwing.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou níveis de proteção.
- **Stops**: Stop loss e take profit absolutos.
- **Filtros**: Nenhum.

## Parâmetros
- `WprLength1`, `WprLength2`, `WprLength3` – períodos dos três indicadores Williams %R.
- `Swing` – deslocamento dos limiares superior/inferior.
- `AverageSwing` – deslocamento adicional baseado no oscilador de longo prazo.
- `Weight1`, `Weight2`, `Weight3` – pesos para cada indicador.
- `StopLoss`, `TakeProfit` – níveis de proteção em pontos.
- `CandleType` – período dos candles, padrão 4 horas.
