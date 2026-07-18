# Como Usar Alavancagem e Margem — Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um sistema de cruzamento de oscilador Stochastic. A estratégia compra quando a linha %K cruza acima de %D abaixo do nível 80 e vende a descoberto quando %K cruza abaixo de %D acima de 20. As posições são protegidas por um take‑profit medido em ticks.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: %K cruza acima de %D e %K < 80.
  - **Vendido**: %K cruza abaixo de %D e %K > 20.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Take‑profit ou cruzamento oposto
- **Stops**: Sim, take‑profit em ticks
- **Valores padrão**:
  - `Stochastic Period` = 13
  - `%K Period` = 4
  - `%D Period` = 3
  - `Take Profit Ticks` = 100
  - `CandleType` = 1 minuto
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Stochastic
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
