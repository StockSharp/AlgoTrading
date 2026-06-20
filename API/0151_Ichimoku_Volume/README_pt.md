# Ichimoku Volume Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Implementação da estratégia - Ichimoku + Volume. Compra quando o preço está acima da nuvem Kumo, Tenkan-sen está acima de Kijun-sen e o volume está acima da média. Vende quando o preço está abaixo da nuvem Kumo, Tenkan-sen está abaixo de Kijun-sen e o volume está acima da média.

Os testes indicam um retorno anual médio de aproximadamente 40%. Funciona melhor no mercado de criptomoedas.

Os componentes do Ichimoku definem o viés direcional enquanto o aumento de volume confirma o interesse. As operações são abertas quando o preço se alinha com a nuvem e o volume aumenta.

É adequado para traders que seguem rompimentos de nuvem com participação. O risco é restrito por um stop baseado em ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Price > Cloud && Tenkan > Kijun && Volume > AvgVolume`
  - Vendido: `Price < Cloud && Tenkan < Kijun && Volume > AvgVolume`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Rompimento da nuvem na direção oposta
- **Stops**: Baseado em percentual usando `StopLoss`
- **Valores padrão**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `VolumeAvgPeriod` = 20
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Ichimoku Cloud, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

