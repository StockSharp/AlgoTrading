# Estratégia de Oscilações de Liquidez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Oscilações de Liquidez rastreia máximas e mínimas de pivô recentes para definir resistência e suporte. Uma operação comprada ocorre quando a mínima cruza acima do suporte enquanto o fechamento permanece abaixo da resistência. Uma operação vendida é acionada quando a máxima cruza abaixo da resistência enquanto o fechamento permanece acima do suporte. O gerenciamento de risco usa um stop loss abaixo/acima do nível com um buffer e um take profit ao dobro dessa distância, gerando uma relação risco-recompensa de 1:2.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Mínima cruza acima do suporte e fechamento < resistência.
  - **Vendido**: Máxima cruza abaixo da resistência e fechamento > suporte.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Stop loss no nível ou com buffer.
  - Take profit a 2× a distância de risco.
- **Stops**: Stop loss e take profit.
- **Valores padrão**:
  - `Lookback` = 5
  - `StopLossBuffer` = 0.5
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Pivot highs/lows
  - Stops: Sim
  - Complexidade: Baixo
  - Período: 1h (padrão)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
