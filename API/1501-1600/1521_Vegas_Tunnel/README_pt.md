# Estratégia Vegas Tunnel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza quatro EMAs para definir um túnel e stops opcionais baseados em ATR.
Abre comprado quando o preço e a EMA rápida estão acima das EMAs lentas e do túnel, e vendido quando estão abaixo.

## Detalhes

- **Critérios de entrada**: alinhamento das EMAs com o preço relativo ao túnel
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop loss ou take profit
- **Stops**: baseados em ATR ou EMA
- **Valores padrão**:
  - `RiskRewardRatio` = 2
  - `UseAtr` = true
  - `AtrLength` = 14
  - `AtrMult` = 1.5
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
