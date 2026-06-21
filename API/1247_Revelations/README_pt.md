# Estratégia Revelations
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia de rompimento de volatilidade que entra em fortes picos de ATR confirmados por extremos locais e um índice de regime. O tamanho da posição se adapta à intensidade do pico.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Pico de ATR para cima em mínimo local com confirmação de regime.
  - **Vendido**: Pico de ATR para baixo em máximo local com confirmação de regime.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Atingimento de take profit ou stop loss.
- **Stops**: Stops de percentual fixo.
- **Valores padrão**:
  - `ATR Fast` = 14
  - `ATR Slow` = 21
  - `ATR StdDev` = 12
  - `Spike Threshold` = 0.5
  - `Super Spike Mult` = 1.5
  - `Regime Window` = 8
  - `Regime Events` = 3
  - `Local Window` = 3
  - `Max Quantity` = 2
  - `Min Quantity` = 1
  - `Stop %` = 0.9
  - `Take Profit %` = 1.8
- **Filtros**:
  - Categoria: Rompimento de volatilidade
  - Direção: Comprado/Vendido
  - Indicadores: ATR, SMA, Highest/Lowest
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
