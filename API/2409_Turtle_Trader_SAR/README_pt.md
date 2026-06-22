# Estratégia Turtle Trader SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Turtle Trader SAR converte o sistema Turtle original do MQL5 com um trailing Parabolic SAR opcional para StockSharp C#.
A estratégia opera rompimentos de canais Donchian, dimensiona posições por risco baseado em ATR e pode piramidear operações vencedoras.

## Como Funciona

1. **Cálculo de Indicadores**
   - ATR de 20 períodos para volatilidade.
   - Canais Donchian para `ShortPeriod` e `ExitPeriod`.
   - Parabolic SAR opcional para stops de rastreamento.
2. **Dimensionamento de Posição**
   - Cada entrada arrisca `RiskFraction` do capital atual.
   - O tamanho da unidade é limitado por `MaxUnits`.
3. **Critérios de Entrada**
   - Fechamento acima da máxima de `ShortPeriod` -> comprar.
   - Fechamento abaixo da mínima de `ShortPeriod` -> vender.
4. **Pirâmide**
   - Adiciona nova unidade a cada movimento de `AddInterval` ATR a favor até `MaxUnits`.
5. **Critérios de Saída**
   - Rompimento oposto de `ExitPeriod`.
   - Stop ATR usando `StopAtr` e take profit opcional `TakeAtr`.
   - Se `UseSar` for true, o stop Parabolic SAR também se aplica.

## Parâmetros

- `ExitPeriod` = 10
- `ShortPeriod` = 20
- `LongPeriod` = 55
- `RiskFraction` = 0.01
- `MaxUnits` = 4
- `AddInterval` = 1
- `StopAtr` = 1
- `TakeAtr` = 1
- `UseSar` = false
- `SarStep` = 0.02
- `SarMax` = 0.2
- `CandleType` = 1 day

## Tags

- **Categoria**: Seguidor de tendência
- **Direção**: Ambos
- **Indicadores**: ATR, Highest, Lowest, Parabolic SAR
- **Stops**: ATR / SAR
- **Complexidade**: Intermediário
- **Período**: Diário
- **Sazonalidade**: Não
- **Redes neurais**: Não
- **Divergência**: Não
- **Nível de risco**: Médio
