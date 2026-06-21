# Machine Learning Supertrend TP SL Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador Supertrend com take profit e stop loss trailing.

Os níveis de stop e lucro seguem a linha Supertrend, buscando capturar movimentos sustentados enquanto protege ganhos quando o impulso se enfraquece.

## Detalhes

- **Critérios de entrada**: Preço cruzando a linha Supertrend.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou atingir o take profit/stop loss trailing.
- **Stops**: Sim, trailing pelo Supertrend.
- **Valores padrão**:
  - `AtrPeriod` = 4
  - `AtrFactor` = 2.94m
  - `StopLossMultiplier` = 0.0025m
  - `TakeProfitMultiplier` = 0.022m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR, Supertrend
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
