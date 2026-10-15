# Estratégia LANZ 3.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia LANZ 3.0 opera rompimentos do range asiático. A direção é escolhida após a janela de decisão de 01:15–02:15 horário de Nova York e uma ordem limitada é colocada no máximo ou mínimo do range com alvos e stops baseados em Fibonacci. Se a ordem não for preenchida até às 02:15, pode inverter a direção. Ordens não preenchidas são canceladas às 08:00 e posições abertas são fechadas às 15:45.

## Detalhes

- **Critérios de entrada**:
  - Rompimento do máximo ou mínimo do range asiático após a janela de decisão.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Take profit ou stop-loss baseado em Fibonacci.
  - Todas as posições fechadas às 15:45 NY.
- **Stops**: Multiplicadores de Fibonacci.
- **Valores padrão**:
  - `UseOptimizedFibo` = true
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Qualquer
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
