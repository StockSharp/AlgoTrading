# Estratégia de Momentum Consistente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Consistent Momentum** seleciona instrumentos que exibem momentum forte em duas janelas de tempo e rebalancea o portfólio mensalmente. Mantém cada tranche por um número fixo de meses e aloca capital igualmente nas cestas compradas e vendidas.

## Detalhes
- **Critérios de entrada**: No primeiro dia de negociação de cada mês, comprar ativos no decil superior de ambas as medidas de momentum e vender a descoberto o decil inferior.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: As posições são fechadas após o término do período de manutenção ou quando ocorre o rebalanceamento.
- **Stops**: Sem lógica de stop explícita; o tamanho da posição é baseado na alocação em dólares.
- **Valores padrão**:
  - `LookbackDays = 7 * 21`
  - `HoldingMonths = 6`
  - `MinTradeUsd = 50`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Price momentum
  - Stops: Não
  - Complexidade: Avançado
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
