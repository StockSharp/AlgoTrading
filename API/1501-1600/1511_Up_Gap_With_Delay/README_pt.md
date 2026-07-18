# Estratégia de Gap de Alta com Atraso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprado quando a sessão abre com um gap de alta superior a um limite e um número específico de barras passou desde a entrada anterior. A posição é mantida por um número fixo de barras.

## Detalhes

- **Critérios de entrada**: gap de alta maior que o limite e período de atraso satisfeito
- **Comprado/Vendido**: Comprado
- **Critérios de saída**: após o período de manutenção expirar
- **Stops**: Não
- **Valores padrão**:
  - `GapThreshold` = 1
  - `DelayPeriods` = 0
  - `HoldingPeriods` = 7
- **Filtros**:
  - Categoria: Padrão
  - Direção: Comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
