# Estratégia Linear sobre MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina sinais MACD sobre preço e volume com regressão linear.

## Detalhes

- **Critérios de entrada**: comprado quando ambos os MACDs estão acima dos sinais e o preço de regressão fica entre a abertura e o fechamento; vendido na condição inversa.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Lookback` = 21
  - `RiskHigh` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MACD, Linear Regression
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Variável
