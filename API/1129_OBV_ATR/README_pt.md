# Estratégia OBV ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia acompanha o On-Balance Volume (OBV) e entra em negociações quando o OBV rompe sua máxima ou mínima recente. Mantém um canal dinâmico similar a um Rompimento de ATR, alternando entre modos de alta e baixa.

## Detalhes

- **Critérios de entrada**: OBV cruza acima da máxima anterior para comprado; cruza abaixo da mínima anterior para vendido.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou ordens de proteção.
- **Stops**: Sim.
- **Valores padrão**:
  - `LookbackLength` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: OBV, Highest, Lowest
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
