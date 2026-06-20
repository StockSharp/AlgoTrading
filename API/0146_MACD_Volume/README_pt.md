# Macd Volume Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia que combina o MACD (Convergência/Divergência de Médias Móveis) com confirmação de volume. Entra em posições quando a linha MACD cruza a linha de Sinal e confirma com aumento de volume.

Os testes indicam um retorno anual médio de aproximadamente 175%. Funciona melhor no mercado de ações.

Os cruzamentos do MACD são filtrados por um aumento de volume para confirmar o momentum. Sinais de compra surgem em cruzamentos de alta com volume em expansão; os de venda fazem o oposto.

Traders de momentum que observam picos de volume podem achá-la valiosa. O risco é limitado usando um stop de ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `MACD crosses above Signal && Volume > AvgVolume * VolumeMultiplier`
  - Vendido: `MACD crosses below Signal && Volume > AvgVolume * VolumeMultiplier`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Cruzamento do MACD na direção oposta
- **Stops**: Baseado em percentual em `StopLossPercent`
- **Valores padrão**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: MACD, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

