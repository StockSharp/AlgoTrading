# Vela Doji Aprimorada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera velas Doji com regras de confirmação simples e gestão de risco-recompensa fixa. Ela entra quando um Doji aparece e a vela ou seu predecessor confirma a direção fechando além da abertura com sombras pequenas. As ordens de proteção usam um stop-loss em pips e um take-profit definido por uma relação risco-recompensa.

## Detalhes

- **Critérios de entrada**: Vela Doji (corpo <= 30% do intervalo). Se de alta com sombra inferior <=1% ou a vela anterior for de alta, ir comprado. Se de baixa com sombra superior <=1% ou a vela anterior for de baixa, ir vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Take-profit ou stop-loss, ou qualquer novo Doji que encerre a posição.
- **Stops**: Sim.
- **Valores padrão**:
  - `RiskRewardRatio` = 2.0m
  - `StopLossPips` = 5
  - `SmaPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Candlestick
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
