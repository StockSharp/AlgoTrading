# Estratégia de Canal Karpenko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Canal Karpenko constrói um canal de preço dinâmico usando duas médias móveis. A linha base é uma média dos preços de fechamento, enquanto os limites superior e inferior são derivados do intervalo médio máximo-mínimo escalado pela razão áurea 1.618. O canal se expande até envolver a barra atual.

Um sinal para comprado aparece quando o limite superior, anteriormente acima da linha base, cruza abaixo dela. Um sinal vendido surge quando o limite superior cruza acima da linha base após ficar abaixo. As posições existentes na direção oposta são fechadas quando o regime muda.

Apenas velas completadas são processadas. Níveis fixos de stop-loss e take-profit protegem cada operação.

## Detalhes

- **Critérios de entrada:**
  - **Comprado:** Limite superior anterior acima da linha base e valor atual abaixo ou igual a ela.
  - **Vendido:** Limite superior anterior abaixo da linha base e valor atual acima ou igual a ela.
- **Critérios de saída:**
  - Fechar comprado quando o limite superior anterior estava abaixo da linha base.
  - Fechar vendido quando o limite superior anterior estava acima da linha base.
- **Stops:** Distâncias fixas de stop-loss e take-profit em unidades de preço.
- **Valores padrão:**
  - `Base MA` = 144
  - `History` = 500
  - `Stop Loss` = 1000
  - `Take Profit` = 2000
  - `Candle Type` = 4 hour
- **Filtros:**
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Custom
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
