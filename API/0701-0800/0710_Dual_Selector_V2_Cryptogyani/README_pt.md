# Seletor de Estratégia Dual V2 - Estratégia Cryptogyani
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia alterna entre duas abordagens somente compradas baseadas em SMA.

- **Estratégia 1**: opera cruzamento de SMA com take profit trailing opcional ou alvo fixo.
- **Estratégia 2**: opera cruzamento de SMA confirmado por tendência em período superior, usa stop ATR e take profit parcial.

## Detalhes

- **Critérios de entrada**:
  - Estratégia 1: SMA rápida cruza acima da SMA lenta.
  - Estratégia 2: SMA rápida cruza acima da SMA lenta e o preço está acima da SMA do período superior.
- **Critérios de saída**:
  - Estratégia 1: alvo de take profit ou stop trailing.
  - Estratégia 2: take profit parcial e depois stop baseado em ATR.
- **Indicadores**: SMA, ATR.
- **Direção**: Somente comprado.
- **Stops**: Sim.
