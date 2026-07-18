# Estratégia MAM Crossover Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia construída comparando médias móveis simples dos preços de fechamento e abertura das velas.
Um sinal comprado ocorre quando a SMA do fechamento cruza acima da SMA da abertura e a barra anterior confirmou uma transição de baixo. Um sinal vendido aparece no padrão oposto. Posições opostas são fechadas na inversão do sinal. Stop loss e take profit fixos opcionais protegem as operações.

## Detalhes

- **Critérios de entrada**: Padrão de cruzamentos de SMA(close) e SMA(open) nas últimas duas barras.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento oposto ou stops protetores.
- **Stops**: Sim.
- **Valores padrão**:
  - `MaPeriod` = 20
  - `StopLossTicks` = 40
  - `TakeProfitTicks` = 190
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Fixo
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
