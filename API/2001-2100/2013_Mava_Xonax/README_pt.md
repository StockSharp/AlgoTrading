# Estratégia MAVA Xonax
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa médias móveis exponenciais de preços de abertura e fechamento para detectar mudanças de direção. As distâncias de stop loss e take profit são derivadas das EMAs de máxima e mínima, garantindo que as operações tenham níveis de risco e recompensa predefinidos.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A EMA de abertura cruza acima da EMA de fechamento usando as duas últimas barras concluídas.
  - **Vendido**: A EMA de abertura cruza abaixo da EMA de fechamento usando as duas últimas barras concluídas.
- **Comprado/Vendido**: Ambos
- **Stops**: Stop loss e take profit fixos baseados em intervalos de EMA.
- **Valores padrão**:
  - `EmaPeriod` = 6
  - `CandleType` = TimeSpan.FromMinutes(240).TimeFrame()
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Longo prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
