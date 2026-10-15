# Estratégia TMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia TMA usa múltiplas médias móveis suavizadas e padrões de velas para operar na direção da tendência de 200 períodos. Combina sinais de golpe de 3 linhas e engolfamento com um filtro de sessão.

## Detalhes

- **Critérios de entrada**: engolfamento de alta ou golpe de 3 linhas em tendência de alta / engolfamento de baixa ou golpe de 3 linhas em tendência de baixa com EMA(2) acima/abaixo de SMA(200) e filtro de sessão opcional
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: EMA(2) cruzando SMA(200)
- **Stops**: Não
- **Valores padrão**:
  - `CandleType` = velas de 5 minutos
  - `FastLength` = 21
  - `MidLength` = 50
  - `Mid2Length` = 100
  - `SlowLength` = 200
  - `UseSession` = false
  - `SessionStart` = 08:30
  - `SessionEnd` = 12:00
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA, EMA
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
