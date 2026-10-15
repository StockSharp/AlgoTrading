# Estratégia de Pulso de Liquidez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Detecta picos de volume elevado confirmados por MACD e ADX. O ATR define o stop e o take profit com limite diário de operações.

## Detalhes

- **Critérios de entrada**:
  - Comprado: pico de volume, MACD cruza acima do sinal, +DI > -DI, ADX >= limiar
  - Vendido: pico de volume, MACD cruza abaixo do sinal, -DI > +DI, ADX >= limiar
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop ou take profit baseado em ATR
- **Stops**: Múltiplos de ATR
- **Valores padrão**:
  - `VolumeSensitivity` = Medium
  - `MacdSpeed` = Medium
  - `DailyTradeLimit` = 20
  - `AtrPeriod` = 9
  - `AdxTrendThreshold` = 41
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MACD, ADX, ATR, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
