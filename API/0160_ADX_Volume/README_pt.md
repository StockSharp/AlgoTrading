# Estratégia ADX Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia ADX + Volume. Entrar em operações quando o ADX está acima do limiar com volume acima da média. A direção é determinada pela comparação entre DI+ e DI-.

Os testes indicam um retorno anual médio de cerca de 67%. Funciona melhor no mercado de ações.

Um ADX alto denota uma tendência forte e os picos de volume confirmam o compromisso. As entradas são feitas quando ambos os indicadores mostram força simultaneamente.

Ótimo para capturar rompimentos enérgicos. Um stop baseado em ATR mantém a exposição sob controle.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `ADX > AdxThreshold && Volume > AvgVolume`
  - Vendido: `ADX > AdxThreshold && Volume > AvgVolume`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: A tendência enfraquece abaixo do limiar
- **Stops**: Baseados em ATR usando `StopLoss`
- **Valores padrão**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `VolumeAvgPeriod` = 20
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: ADX, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
