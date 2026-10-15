# Estratégia de Envelope Nadaraya-Watson
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Constrói envelopes de regressão kernel Nadaraya-Watson em escala logarítmica. Vai comprado quando o preço cruza acima do envelope inferior e opcionalmente vai vendido quando o preço cruza abaixo do envelope superior.

## Detalhes

- **Critérios de entrada**:
  - Comprado quando o fechamento cruza acima do envelope inferior.
  - Vendido quando o fechamento cruza abaixo do envelope superior (no modo Comprado/Vendido).
- **Comprado/Vendido**: Configurável.
- **Critérios de saída**: Cruzamento inverso do envelope.
- **Stops**: Não.
- **Valores padrão**:
  - `LookbackWindow` = 8
  - `RelativeWeighting` = 8
  - `StartRegressionBar` = 25
  - `StrategyType` = Long Only
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Envelope
  - Direção: Configurável
  - Indicadores: Nadaraya-Watson
  - Stops: Não
  - Complexidade: Avançado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
