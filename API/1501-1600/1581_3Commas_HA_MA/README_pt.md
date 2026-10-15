# Estratégia 3Commas HA & MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Usa velas Heikin Ashi e um par de médias móveis exponenciais. Uma operação comprada ocorre quando uma vela HA de baixa é seguida por uma de alta enquanto a MA rápida está acima da MA lenta. As vendidas seguem a configuração oposta. As posições são fechadas quando o preço cruza a MA lenta ou atinge o stop de swing.

## Detalhes
- **Critérios de entrada**: Reversão de Heikin Ashi com confirmação de MA.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Preço cruza a MA lenta ou stop.
- **Stops**: Máximo/mínimo do swing.
- **Valores padrão**:
  - `MaFast` = 9
  - `MaSlow` = 18
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Heikin Ashi, EMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
