# Estratégia Supertrade RVI Somente Comprado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza o Índice de Volatilidade Relativa (RVI) cruzando acima de 20 para abrir operações compradas. O stop loss e o take profit são definidos a partir do percentual de risco e da relação de recompensa.

## Detalhes

- **Critérios de entrada**: RVI cruza acima do limiar
- **Comprado/Vendido**: Comprado
- **Critérios de saída**: Stop loss ou take profit
- **Stops**: Sim
- **Valores padrão**:
  - `RviLength` = 10
  - `EmaLength` = 14
  - `RviThreshold` = 20
  - `RiskPercent` = 1
  - `RewardRatio` = 3
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado
  - Indicadores: StdDev, EMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

