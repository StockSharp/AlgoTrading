# Estratégia de Stop Percentual e Tomada de Lucro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza duas médias móveis simples (SMA) para detectar a direção da tendência. Quando a SMA rápida cruza acima da SMA lenta, abre uma posição comprada. Quando a SMA rápida cruza abaixo da SMA lenta, abre uma posição vendida. Após a entrada, a estratégia define níveis de stop-loss e tomada de lucro como percentagens do preço de entrada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A SMA rápida cruza acima da SMA lenta.
  - **Vendido**: A SMA rápida cruza abaixo da SMA lenta.
- **Critérios de saída**:
  - Stop-loss e tomada de lucro baseados em percentagens do preço de entrada.
- **Stops**: Sim, tanto stop-loss quanto tomada de lucro.
- **Indicadores**: SMA.
- **Categoria**: Seguidor de tendência.
- **Período**: Qualquer.
