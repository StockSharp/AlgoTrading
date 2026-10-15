# Estratégia Spread By
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Spread By usa uma média móvel com bandas de desvio padrão para operar em extremos de preço.
Compra quando o preço cai abaixo da banda inferior e vende quando o preço sobe acima da banda superior.

## Detalhes

- **Critérios de entrada**: o preço se move além de ±1 desvio padrão da média móvel
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: o preço retorna à média móvel
- **Stops**: Não
- **Valores padrão**:
  - `Length` = 100
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: SMA, StandardDeviation
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
