# Estratégia de Cruzamento EMA com Stop Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Cruzamento EMA com Stop Trailing** abre uma posição comprada quando a EMA curta cruza acima da EMA longa e abre uma posição vendida quando cruza abaixo. Um stop trailing baseado no preço mais alto ou mais baixo após a entrada fecha a posição quando o preço reverte em um percentual definido.

## Detalhes
- **Critérios de entrada**: cruzamento da EMA curta sobre a EMA longa.
- **Comprado/Vendido**: ambas as direções.
- **Critérios de saída**: cruzamento oposto ou stop trailing.
- **Stops**: stop trailing usando o preço máximo/mínimo desde a entrada.
- **Valores padrão**:
  - `ShortLength = 9`
  - `LongLength = 21`
  - `TrailStopPercent = 1`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Stop trailing
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
