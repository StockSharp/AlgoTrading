# Estratégia Digital CCI Woodies
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera no cruzamento de dois indicadores CCI (Índice de Canal de Commodities). Um CCI rápido reage rapidamente às mudanças de preço, enquanto um CCI lento suaviza o ruído do mercado. Os sinais são gerados quando a linha rápida cruza a lenta.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o CCI rápido cruza acima do CCI lento.
  - Vendido: o CCI rápido cruza abaixo do CCI lento.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - As posições compradas são fechadas quando o CCI rápido cruza abaixo do CCI lento.
  - As posições vendidas são fechadas quando o CCI rápido cruza acima do CCI lento.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = velas de 6 horas
  - `FastLength` = 14
  - `SlowLength` = 6
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: CCI
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
