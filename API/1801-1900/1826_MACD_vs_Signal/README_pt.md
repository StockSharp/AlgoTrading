# Estratégia MACD vs Signal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no cruzamento da linha MACD com a linha de sinal.

Entra comprado quando a linha MACD cruza acima da linha de sinal.
Entra vendido quando a linha MACD cruza abaixo da linha de sinal.
Opcionalmente aplica stop-loss, take-profit e stop de rastreamento.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `MACD cruza acima de Signal`
  - Vendido: `MACD cruza abaixo de Signal`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Cruzamento de MACD oposto
  - Regras de gestão de risco (stop-loss, stop de rastreamento, take-profit)
- **Stops**: Stop-loss, take-profit, stop de rastreamento (opcional)
- **Valores padrão**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLoss` = 50 pontos
  - `TakeProfit` = 999 pontos
  - `TrailingStop` = 0 pontos (desativado)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Stop-loss / Take-profit / Trailing
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
