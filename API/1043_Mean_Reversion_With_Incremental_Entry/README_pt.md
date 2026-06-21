# Estratégia de Reversão à Média com Entrada Incremental
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra em operações quando o preço desvia de uma média móvel simples por uma porcentagem definida. Ordens adicionais são colocadas incrementalmente à medida que o preço se afasta mais da média.

As posições são fechadas assim que o preço retorna à média móvel.

## Detalhes

- **Critérios de entrada:**
  - **Comprado:** `Low < SMA` e a diferença percentual entre `Low` e `SMA` ≥ `Initial Percent`.
  - **Vendido:** `High > SMA` e a diferença percentual entre `High` e `SMA` ≥ `Initial Percent`.
- **Entradas incrementais:** Novas ordens são adicionadas a cada `Percent Step` adicional a partir da entrada anterior.
- **Critérios de saída:**
  - **Comprado:** `Close ≥ SMA`.
  - **Vendido:** `Close ≤ SMA`.
- **Indicadores:** SMA.
- **Valores padrão:**
  - `MA Length` = 30.
  - `Initial Percent` = 5.
  - `Percent Step` = 1.
