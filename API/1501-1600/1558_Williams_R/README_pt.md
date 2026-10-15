# Estratégia Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia compra quando Williams %R cai em território de sobrevenda profunda e sai em rompimento de alta ou em níveis de sobrecompra.

## Detalhes

- **Critérios de entrada**: %R < -90
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: fechamento > máxima anterior ou %R > -30
- **Stops**: Não
- **Valores padrão**:
  - `LookbackPeriod` = 2
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Comprado
  - Indicadores: WilliamsR
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
