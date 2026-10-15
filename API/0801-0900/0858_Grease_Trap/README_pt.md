# Estratégia Grease Trap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Grease Trap usa duas médias móveis de comprimento Fibonacci e opera nos cruzamentos com metas de lucro.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: A média rápida cruza acima da média lenta.
  - **Vendido**: A média rápida cruza abaixo da média lenta.
- **Critérios de saída**: Meta de lucro ou cruzamento oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length1` = 9
  - `Length2` = 14
  - `LongProfit` = 0.02
  - `ShortProfit` = 0.02
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: SMA
  - Complexidade: Baixo
  - Nível de risco: Médio
