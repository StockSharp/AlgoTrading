# Estratégia Keltner Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia que combina os Canais Keltner e o Oscilador Stochastic.
Entra em posições quando o preço atinge os limites do Canal Keltner e o Stochastic confirma condições de sobrevenda/sobrecompra.

Os testes indicam um retorno anual médio de aproximadamente 163%. Funciona melhor no mercado de ações.

Esta configuração busca capturar reversões próximas às bandas Keltner enquanto o oscilador confirma mudanças de momentum. Os sinais podem ser acionados em ambas as direções quando o preço pressiona contra um envelope.

Traders de curto prazo que buscam reversões rápidas podem achá-la útil. O risco é contido por uma distância de stop baseada em ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close < LowerBand && StochK < StochOversold`
  - Vendido: `Close > UpperBand && StochK > StochOverbought`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: `Close > EMA`
  - Vendido: `Close < EMA`
- **Stops**: `StopLossAtr` ATR a partir da entrada
- **Valores padrão**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `KeltnerMultiplier` = 2.0m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `StopLossAtr` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Keltner Channel, Stochastic Oscillator
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

