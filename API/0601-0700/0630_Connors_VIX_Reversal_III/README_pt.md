# Connors VIX Reversão III
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia contrária que usa picos do VIX em relação à sua média móvel. Compra quando o VIX salta acima da média por uma porcentagem definida e vende a descoberto quando o VIX cai abaixo dela.

As posições são fechadas quando o VIX cruza a média móvel do dia anterior.

## Detalhes

- **Critérios de entrada**: VIX mínimo acima da MA e fechamento acima da MA pelo limiar para compras; VIX máximo abaixo da MA e fechamento abaixo do limiar para vendas.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: VIX cruzando a MA de ontem.
- **Stops**: Não.
- **Valores padrão**:
  - `LengthMA` = 10
  - `PercentThreshold` = 10m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Contrário
  - Direção: Ambos
  - Indicadores: VIX, SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
