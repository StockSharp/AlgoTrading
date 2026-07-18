# Estratégia de Ação de Preço Pura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de ação de preço simplificada que detecta Quebra de Estrutura (BOS) e Mudança de Estrutura de Mercado (MSS) a partir de máximas e mínimas recentes.

A estratégia entra comprado no BOS e vendido no MSS com percentuais fixos de stop-loss e take-profit.

## Detalhes

- **Critérios de entrada**: BOS para comprado, MSS para vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss ou take-profit.
- **Stops**: Percentual fixo.
- **Valores padrão**:
  - `Length` = 5
  - `SlPercent` = 1m
  - `TpPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
