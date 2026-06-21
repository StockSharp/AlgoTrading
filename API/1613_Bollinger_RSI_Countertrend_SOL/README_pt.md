# Estratégia Contrária à Tendência Bollinger RSI SOL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de reversão à média para SOL que compra quando o preço cruza acima da banda inferior de Bollinger com RSI baixo e vende quando o preço cruza abaixo da banda superior com RSI alto. Apenas em dias úteis.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço cruza acima da banda inferior e `RSI` < `Long RSI` em dias úteis.
  - **Vendido**: O preço cruza abaixo da banda superior e `RSI` > `Short RSI` em dias úteis.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Comprado: o preço cruza acima da banda superior ou stop loss abaixo das mínimas recentes.
  - Vendido: o preço cruza acima da banda do meio ou atinge o alvo de lucro.
- **Stops**: Stop comprado abaixo das mínimas recentes.
- **Valores padrão**:
  - `Bollinger Period` = 20
  - `Bollinger Width` = 2
  - `RSI Length` = 14
  - `Long RSI` = 25
  - `Short RSI` = 79
  - `Short Profit %` = 3.5
- **Filtros**:
  - Categoria: Mean Reversion
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Sim (dias úteis)
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
