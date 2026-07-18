# Estratégia de Negociação por Temporizador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Negociação por Temporizador alterna entre posições compradas e vendidas em intervalos de tempo fixos. Um temporizador aciona ordens a mercado e cada posição é automaticamente protegida com stop-loss e take-profit.

## Detalhes

- **Critérios de entrada**: Evento do temporizador.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop-loss ou take-profit.
- **Stops**: Sim, via StartProtection.
- **Valores padrão**:
  - `TimerInterval` = TimeSpan.FromSeconds(30)
  - `Volume` = 1
  - `StopLossLevel` = 10 pontos
  - `TakeProfitLevel` = 50 pontos
- **Filtros**:
  - Categoria: Temporizador
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
