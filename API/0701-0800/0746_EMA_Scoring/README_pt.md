# Estratégia de Pontuação EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia avalia a direção do mercado usando três linhas EMA e opera quando um limiar de pontuação é cruzado.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: A pontuação cruza acima do limiar.
  - **Vendido**: A pontuação cruza abaixo do limiar negativo.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinal inverso.
- **Stops**: Não.
- **Valores padrão**:
  - `Short EMA Period` = 21
  - `Medium EMA Period` = 50
  - `Long EMA Period` = 100
  - `Score Threshold` = 4
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Médio prazo
