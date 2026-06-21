# Estratégia Forex Fraus 4 For M1s
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Conversão da estratégia MQL4 #13643. O consultor especialista original entra em operações quando o indicador Williams %R toca níveis extremos e depois cruza de volta. Esta versão em C# utiliza a API de alto nível do StockSharp.

A estratégia funciona em candles de 1 minuto e reage a dois níveis-chave:
- Um sinal comprado é gerado depois que Williams %R sobe acima de -99.9 tendo estado abaixo.
- Um sinal vendido aparece quando Williams %R cai abaixo de -0.1 tendo estado acima.

As posições são fechadas por stops fixos, alvos ou trailing stop. Um filtro de tempo pode restringir as operações a uma janela intradiária específica.

## Detalhes

- **Critérios de entrada**  
  - Comprado: `WilliamsR` cruza acima de `BuyThreshold` (-99.9) depois de estar abaixo.  
  - Vendido: `WilliamsR` cruza abaixo de `SellThreshold` (-0.1) depois de estar acima.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**  
  - O preço atinge o stop-loss (`StopLoss`) ou o take-profit (`TakeProfit`)  
  - Trailing stop (`TrailingStop`) ativado quando habilitado
- **Stops**: Baseados em passos
- **Valores padrão**  
  - `WprPeriod` = 360  
  - `BuyThreshold` = -99.9  
  - `SellThreshold` = -0.1  
  - `StopLoss` = 0  
  - `TakeProfit` = 0  
  - `UseProfitTrailing` = true  
  - `TrailingStop` = 30  
  - `TrailingStep` = 1  
  - `UseTimeFilter` = false  
  - `StartHour` = 7  
  - `StopHour` = 17  
  - `Volume` = 0.01  
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**  
  - Categoria: Reversão de tendência  
  - Direção: Ambos  
  - Indicadores: Williams %R  
  - Stops: Sim  
  - Complexidade: Básico  
  - Período: Intradiário (M1)  
  - Sazonalidade: Não  
  - Redes neurais: Não  
  - Divergência: Não  
  - Nível de risco: Médio
