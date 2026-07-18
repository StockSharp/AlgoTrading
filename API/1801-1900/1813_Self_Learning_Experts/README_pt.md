# Estratégia de Especialistas com Autoaprendizagem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aprende com padrões binários históricos de preços e estima a probabilidade de movimento futuro para cima ou para baixo. Quando a probabilidade excede um limite definido pelo usuário, a estratégia abre uma posição a mercado nessa direção. As estatísticas coletadas decaem com o tempo por meio de um fator de esquecimento para dar mais peso ao comportamento recente. O sistema pode opcionalmente mover os níveis de stop quando novos sinais aparecem e suporta um stop trailing baseado em passos de preço.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Probabilidade de movimento para cima ≥ `ProbabilityThreshold`.
  - **Vendido**: Probabilidade de movimento para baixo ≥ `ProbabilityThreshold`.
- **Stops**: Stop trailing opcional com stop-loss e take-profit simétricos.
- **Valores padrão**:
  - `PatternSize` = 10
  - `ProbabilityThreshold` = 0.8
  - `ForgetRate` = 1.05
  - `Trailing` = 0 (desativado)
- **Filtros**:
  - Categoria: Reconhecimento de padrões
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Opcional
  - Complexidade: Alto
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
