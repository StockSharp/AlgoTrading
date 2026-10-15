# Estratégia Fracture
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Fracture combina rompimentos de fractais com médias móveis suavizadas e ADX para operar tanto em mercados laterais quanto em tendência.

## Detalhes

- **Critérios de entrada**: Se o ADX estiver abaixo do limiar, entrar comprado acima do último fractal de alta ou vendido abaixo do último fractal de baixa quando o preço também estiver acima/abaixo da SMMA rápida. No regime de tendência (SMMA rápida acima/abaixo das mais lentas), entrar na direção da tendência quando o preço cruzar a SMMA rápida.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Fechar posição quando o lucro superar o ATR multiplicado por `MinProfit`.
- **Stops**: Alvo de lucro baseado em ATR.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `AtrPeriod` = 14
  - `AdxPeriod` = 22
  - `AdxLine` = 40
  - `Ma1Period` = 5
  - `Ma2Period` = 9
  - `Ma3Period` = 22
  - `RangingMultiplier` = 0.5
  - `MinProfit` = 1
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado & Vendido
  - Indicadores: Fractal, SMMA, ATR, ADX
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
