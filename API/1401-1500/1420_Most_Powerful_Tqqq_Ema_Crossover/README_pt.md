# Estratégia de Cruzamento de EMA TQQQ Mais Poderosa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia entra comprada quando a EMA rápida cruza acima da EMA lenta. O take profit e o stop loss são definidos como multiplicadores do preço de entrada.

## Detalhes

- **Critérios de entrada**: EMA rápida cruzando acima da EMA lenta
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: Preço atingindo o nível de take profit ou stop loss
- **Stops**: Sim (multiplicador fixo)
- **Valores padrão**:
  - `FastLength` = 20
  - `SlowLength` = 50
  - `TakeProfitMultiplier` = 1.3
  - `StopLossMultiplier` = 0.95
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
