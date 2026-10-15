# Captura de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia seguidora de tendência combinando o Parabolic SAR com filtro ADX. Operações compradas ocorrem quando o preço fecha acima do valor do SAR enquanto o ADX permanece abaixo de um limiar, sinalizando uma tendência nascente. Operações vendidas são abertas na condição oposta.

## Detalhes

- **Critérios de entrada**: Preço acima/abaixo do Parabolic SAR com ADX abaixo de `AdxLevel`.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Stop loss, take profit ou sinal oposto.
- **Stops**: Stop loss fixo, take profit e ajuste de break-even.
- **Valores padrão**:
  - `SarStep` = 0.02
  - `SarMax` = 0.2
  - `AdxPeriod` = 14
  - `AdxLevel` = 20
  - `StopLoss` = 1800 pontos
  - `TakeProfit` = 500 pontos
  - `BreakEven` = 50 pontos
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR, ADX
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
