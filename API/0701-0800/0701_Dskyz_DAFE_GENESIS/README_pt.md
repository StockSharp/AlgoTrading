# Estratégia Dskyz (DAFE) GENESIS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Versão simplificada da estratégia Dskyz (DAFE) GENESIS. O sistema opera quando o momentum de curto prazo se alinha com um filtro de tendência e o RSI.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `SMA(9) > SMA(30)` e `RSI > 55` e `EMA(8) > EMA(21)`.
  - **Vendido**: `SMA(9) < SMA(30)` e `RSI < 45` e `EMA(8) < EMA(21)`.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - **Comprado**: `EMA(8) < EMA(21)`.
  - **Vendido**: `EMA(8) > EMA(21)`.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `RSI Length` = 9.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: RSI, EMA, SMA
  - Stops: Não
  - Complexidade: Simples
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
