# Estratégia VIDYA de Auto-Trading (Lógica de Reversão)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aplica uma Média Dinâmica de Índice Variável (VIDYA) com bandas ATR amplas.
Uma operação comprada é aberta quando o preço rompe acima da banda superior, e uma operação vendida quando o preço rompe abaixo da banda inferior.

## Detalhes

- **Critérios de entrada**: o preço cruza a banda ATR ao redor do VIDYA
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: rompimento de banda oposta
- **Stops**: Não
- **Valores padrão**:
  - `VidyaLength` = 10
  - `VidyaMomentum` = 20
  - `BandDistance` = 2
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: VIDYA, ATR
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
