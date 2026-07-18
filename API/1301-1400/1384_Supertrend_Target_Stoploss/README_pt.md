# Estratégia Supertrend com Alvo e Stop Loss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que compra quando o preço cruza acima da linha Supertrend e vende quando cruza abaixo. Um alvo e stop loss de percentual fixo fecham as posições.

## Detalhes

- **Critérios de entrada**: Preço cruzando o Supertrend.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Percentual de alvo ou stop loss.
- **Stops**: Sim, percentual fixo.
- **Valores padrão**:
  - `Period` = 14
  - `Multiplier` = 3m
  - `TargetPct` = 0.01m
  - `StopPct` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR, Supertrend
  - Stops: Fixo
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
