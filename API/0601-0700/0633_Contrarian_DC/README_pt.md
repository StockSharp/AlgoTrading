# Estratégia Contrarian DC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Contrarian DC opera contra os rompimentos do Canal Donchian. Compra quando o preço perfura a banda inferior e vende quando o preço toca a banda superior. Após um stop-loss, as entradas na mesma direção são pausadas por um número de velas. O gerenciamento de risco utiliza stop-loss e take-profit simétricos com base em uma relação risco/recompensa.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Mínimo do preço <= Donchian Low && pausa satisfeita
  - **Vendido**: Máximo do preço >= Donchian High && pausa satisfeita
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Stop**: Stop-loss percentual
  - **Alvo**: Take-profit baseado em risco/recompensa
  - **Banda**: Fechar ao atingir a banda oposta
- **Stops**: Sim, baseados em percentual
- **Valores padrão**:
  - `DonchianPeriod` = 20
  - `RiskRewardRatio` = 1.7m
  - `StopLossPercent` = 0.3m
  - `PauseCandles` = 3
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Donchian Channel
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
