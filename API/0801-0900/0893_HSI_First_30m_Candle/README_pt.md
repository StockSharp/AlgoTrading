# Estratégia da Primeira Vela de 30m do HSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia captura a máxima e mínima dos primeiros 30 minutos após a abertura da sessão de Hong Kong e opera rompimentos em um gráfico de 5 minutos. Apenas uma operação é permitida por dia.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o preço rompe acima da máxima dos primeiros 30 minutos durante a sessão.
  - **Vendido**: o preço cai abaixo da mínima dos primeiros 30 minutos durante a sessão.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Stop loss no lado oposto do intervalo.
  - Take profit a uma distância do tamanho do intervalo multiplicado por `RiskReward` desde a entrada.
- **Stops**: Sim.
- **Valores padrão**:
  - `RiskReward` = 1.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Price action
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
