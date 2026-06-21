# IU Canal EMA Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertido do script do TradingView "IU EMA Channel Strategy". A estratégia opera quando o preço cruza os canais EMA construídos a partir de máximas e mínimas. O stop-loss é definido no extremo da vela anterior e o take profit é calculado usando uma relação risco/recompensa.

## Detalhes

- **Critérios de entrada**: O fechamento cruza acima do EMA alto para comprado, abaixo do EMA baixo para vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss no extremo da vela anterior ou take profit pela relação risco/recompensa.
- **Stops**: Sim, stop fixo e alvo.
- **Valores padrão**:
  - `EmaLength` = 100
  - `RiskToReward` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Variável
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
