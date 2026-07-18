# Estratégia TFM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento com multiplicador de período. Utiliza um período superior formado pela multiplicação do período base. Comprado quando o preço rompe acima da máxima anterior e opcionalmente vendido ou saída quando o preço cai abaixo da mínima anterior.

## Detalhes
- **Critérios de entrada**: O preço cruza níveis do período multiplicado.
- **Comprado/Vendido**: Comprado com vendido opcional.
- **Critérios de saída**: Cruzamento do nível oposto ou reversão opcional.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleTime` = TimeSpan.FromMinutes(1)
  - `Multiplier` = 2
  - `AllowShort` = false
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos (se vendidos habilitados)
  - Indicadores: High/Low
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
