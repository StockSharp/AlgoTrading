# Estratégia de Trading Baseada em Sequências
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Rastreia velas vencedoras e perdedoras consecutivas. Após atingir a sequência especificada, a estratégia entra na direção oposta e mantém a posição por um número fixo de velas. Velas Doji são ignoradas com base no tamanho do corpo.

## Detalhes

- **Critérios de entrada**: Lado oposto após atingir a sequência de ganhos/perdas.
- **Comprado/Vendido**: Configurável (`TradeDirection`).
- **Critérios de saída**: Após `HoldDuration` velas.
- **Stops**: Não.
- **Valores padrão**:
  - `TradeDirection` = Long
  - `StreakThreshold` = 8
  - `HoldDuration` = 7
  - `DojiThreshold` = 0.01
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão
  - Direção: Configurável
  - Indicadores: Price Action
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
