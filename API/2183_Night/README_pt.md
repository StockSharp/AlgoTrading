# Estratégia Night Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Night Stochastic opera apenas durante a tranquila sessão noturna das **21:00** às **06:00**. Utiliza a linha %K do Stochastic Oscillator para detectar condições de sobrevenda e sobrecompra.

Quando o oscilador cai abaixo do nível de sobrevenda, uma posição comprada é aberta. Quando sobe acima do nível de sobrecompra, uma posição vendida é aberta. Cada operação é protegida por níveis fixos de stop loss e take profit medidos em pontos de preço.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `%K < StochOversold` e o tempo está entre 21:00 e 06:00.
  - **Vendido**: `%K > StochOverbought` e o tempo está entre 21:00 e 06:00.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Posição fechada por stop loss ou take profit predefinidos.
- **Stops**: Sim, utiliza stop loss e take profit fixos.
- **Valores padrão**:
  - `StopLossPoints` = 40
  - `TakeProfitPoints` = 20
  - `StochOversold` = 30
  - `StochOverbought` = 70
  - `CandleType` = período de 15 minutos
- **Filtros**:
  - Categoria: Baseado em indicadores
  - Direção: Ambos
  - Indicadores: Stochastic Oscillator
  - Período: Curto prazo
  - Janela de trading: 21:00-06:00 horário do servidor
