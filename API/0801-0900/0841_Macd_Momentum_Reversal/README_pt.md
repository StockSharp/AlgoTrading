# Reversão de Momentum MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia usa o histograma MACD para detectar reversões de momentum.
Vende a descoberto quando a vela de alta cresce mas o histograma MACD declina.
Compra quando a vela de baixa cresce mas o histograma MACD sobe.

## Detalhes

- **Critérios de entrada**: Maior corpo de vela com momentum MACD decrescente.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
