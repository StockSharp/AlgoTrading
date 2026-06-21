# Estratégia G-Channel com EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina a lógica do canal G-Channel com um filtro de tendência EMA.

Compra quando o último cruzamento é descendente e o preço está abaixo da EMA. Vende quando o último cruzamento é ascendente e o preço está acima da EMA.

## Detalhes

- **Critérios de entrada**: Estado do G-Channel com filtro EMA.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `ChannelLength` = 100
  - `EmaLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: G-Channel, EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
