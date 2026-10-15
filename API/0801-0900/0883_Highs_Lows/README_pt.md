# Estratégia de Máximos e Mínimos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera nos pontos médios das velas em relação ao intervalo de máximos e mínimos.

Compra quando o ponto médio da vela atual está abaixo da média dos valores mais altos e mais baixos e a distância normalizada está abaixo de LowThreshold. Fecha a posição comprada quando o ponto médio sobe acima da média e a distância normalizada está acima de HighThreshold.

## Detalhes

- **Critérios de entrada**: Ponto médio abaixo da média e distância normalizada abaixo de LowThreshold.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Ponto médio acima da média e distância normalizada acima de HighThreshold.
- **Stops**: Não.
- **Valores padrão**:
  - `Range` = 100
  - `LowThreshold` = 15m
  - `HighThreshold` = 85m
  - `CandleType` = TimeSpan.FromMinutes(240)
- **Filtros**:
  - Categoria: Range
  - Direção: Comprado
  - Indicadores: Highest, Lowest
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (240m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
