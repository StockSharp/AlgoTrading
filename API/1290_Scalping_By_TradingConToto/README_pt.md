# Estratégia de Scalping de TradingConToto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Scalping de TradingConToto traça linhas entre máximos ou mínimos de pivô consecutivos dependendo da tendência da EMA. Quando o preço cruza acima de uma linha descendente de máximos de pivô durante uma tendência de alta, a estratégia entra comprada. Quando o preço cai abaixo de uma linha ascendente de mínimos de pivô durante uma tendência de baixa, entra vendida. A operação é permitida apenas durante uma sessão especificada.

## Detalhes

- **Critérios de entrada**: Tendência de alta com o preço rompendo uma linha descendente de máximos de pivô para comprado; tendência de baixa com o preço rompendo uma linha ascendente de mínimos de pivô para vendido.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Take profit e stop loss.
- **Stops**: Sim.
- **Valores padrão**:
  - `Pivot` = 16
  - `Pips` = 64
  - `Spread` = 0
  - `Session` = "0830-0930"
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: EMA, pivot
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
