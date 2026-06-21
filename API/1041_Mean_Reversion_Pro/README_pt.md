# Estratégia de Reversão à Média Pro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Mean Reversion Pro é um sistema de reversão à média construído para os principais índices. Utiliza duas médias móveis e níveis de amplitude intrabar para detectar pullbacks. Operações compradas são preferidas, pois os índices tendem a se mover para cima.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: fechamento abaixo da SMA rápida, fechamento abaixo do nível de 20% do intervalo, fechamento acima da SMA lenta, sem posição.
  - **Vendido**: fechamento acima da SMA rápida, fechamento acima do nível de 80% do intervalo, fechamento abaixo da SMA lenta, sem posição.
- **Comprado/Vendido**: Ambos (comprado recomendado).
- **Critérios de saída**:
  - **Comprado**: fechamento cruza acima da SMA rápida.
  - **Vendido**: fechamento cruza abaixo da SMA rápida.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Fast SMA` = 5
  - `Slow SMA` = 100
  - `Direction` = Somente comprado
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Configurável
  - Indicadores: SMA
  - Stops: Nenhum
  - Complexidade: Simples
  - Período: Diário
