# Estratégia MADH de Diferença de Médias Móveis, Hann
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa o indicador MADH descrito por John Ehlers. A estratégia fica comprada quando o indicador está acima de zero e vendida quando está abaixo.

## Detalhes
- **Critérios de entrada**: MADH > 0 para comprados, MADH < 0 para vendidos.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Reverter com sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `ShortLength` = 8
  - `DominantCycle` = 27
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MADH
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
