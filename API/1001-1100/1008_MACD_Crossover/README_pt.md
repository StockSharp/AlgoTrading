# Estratégia de Cruzamento MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no cruzamento do MACD dentro de uma zona especificada.

A estratégia de cruzamento MACD aguarda a linha MACD cruzar a linha de sinal enquanto o valor do MACD permanece entre os limiares inferior e superior. O cruzamento oposto fecha a posição existente. Nenhum stop-loss é aplicado.

## Detalhes

- **Critérios de entrada**: Cruzamento MACD dentro da zona.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `LowerThreshold` = -0.5m
  - `UpperThreshold` = 0.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
