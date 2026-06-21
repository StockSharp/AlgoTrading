# Estratégia NQ Phantom Scalper Pro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de Rompimento de bandas VWAP com filtros opcionais de volume e tendência.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o preço fecha acima da banda VWAP superior com volume confirmatório.
  - **Vendido**: o preço fecha abaixo da banda VWAP inferior com volume confirmatório.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - O preço cruza de volta pelo VWAP ou o stop por ATR é atingido.
- **Stops**: Baseado em ATR
- **Valores padrão**:
  - `Band #1 Mult` = 1.0
  - `Band #2 Mult` = 2.0
  - `ATR Length` = 14
  - `ATR Stop Mult` = 1.0
  - `Volume SMA Period` = 20
  - `Volume Spike Mult` = 1.5
  - `Trend EMA Length` = 50
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: VWAP, ATR, EMA, SMA
  - Stops: Sim
  - Complexidade: Médio
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
