# Estratégia de Cruzamento Multi-EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia abre posições compradas separadas para quatro pares de EMA quando a EMA mais rápida cruza acima da mais lenta. Cada posição é fechada quando sua EMA rápida cai abaixo da EMA lenta.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A EMA rápida cruza acima da EMA lenta em qualquer um dos pares (1/5, 3/10, 5/20, 10/40).
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - A EMA rápida cai abaixo da EMA lenta para o par respectivo.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `EMA1` = 1
  - `EMA3` = 3
  - `EMA5` = 5
  - `EMA10` = 10
  - `EMA20` = 20
  - `EMA40` = 40
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
