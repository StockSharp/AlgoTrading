# Estratégia de Regressão Múltipla
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera quando o preço cruza uma linha de regressão e gerencia o risco com limites baseados em volatilidade. Níveis opcionais de stop loss e take profit são derivados de uma medida de risco selecionada.

## Detalhes

- **Critérios de entrada**: Preço cruzando acima ou abaixo do valor de regressão.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou quando o preço atinge os limites selecionados.
- **Stops**: Opcional, baseado em `UseStopLoss` e `UseTakeProfit`.
- **Valores padrão**:
  - `Length` = 90
  - `RiskMeasure` = Atr
  - `RiskMultiplier` = 1
  - `UseStopLoss` = true
  - `UseTakeProfit` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: LinearRegression, ATR/StdDev/Bollinger/Keltner
  - Stops: Opcional
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
